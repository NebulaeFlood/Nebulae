using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Tests.Runtime.Emit.Inline.Replacement;

internal static class PrimitiveReplacementProbeGenerator
{
    private const string PlaceholderAttributeName = "Nebulae.Runtime.Emit.Inline.PlaceholderAttribute";
    private const string PlaceholderCodeName = "Nebulae.Runtime.Emit.Inline.PlaceholderCode";
    private const string EmitTypeName = "Nebulae.Runtime.Emit.Inline.IL/Emit";

    private static readonly Dictionary<string, Code> FinalizedCodes = new(StringComparer.Ordinal)
    {
        ["Ldarg"] = Code.Ldarg_0,
        ["Ldarga"] = Code.Ldarga_S,
        ["Starg"] = Code.Starg_S,
        ["Ldloc"] = Code.Ldloc_0,
        ["Ldloca"] = Code.Ldloca_S,
        ["Stloc"] = Code.Stloc_0,
        ["Ldelem"] = Code.Ldelem_Any,
        ["Stelem"] = Code.Stelem_Any,
        ["Br"] = Code.Br_S,
        ["Brfalse"] = Code.Brfalse_S,
        ["Brtrue"] = Code.Brtrue_S,
        ["Beq"] = Code.Beq_S,
        ["Bne_Un"] = Code.Bne_Un_S,
        ["Bge"] = Code.Bge_S,
        ["Bge_Un"] = Code.Bge_Un_S,
        ["Bgt"] = Code.Bgt_S,
        ["Bgt_Un"] = Code.Bgt_Un_S,
        ["Ble"] = Code.Ble_S,
        ["Ble_Un"] = Code.Ble_Un_S,
        ["Blt"] = Code.Blt_S,
        ["Blt_Un"] = Code.Blt_Un_S,
        ["Leave"] = Code.Leave_S,
    };

    public static PrimitiveReplacementProbeSet Create()
    {
        string inlineAssemblyPath = Path.Combine(AppContext.BaseDirectory, "Nebulae.Runtime.Emit.Inline.dll");
        Assert.IsTrue(File.Exists(inlineAssemblyPath), $"Inline IL assembly was not found at '{inlineAssemblyPath}'.");

        using AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(inlineAssemblyPath);
        TypeDefinition placeholderCode = FindType(assembly.MainModule.Types, PlaceholderCodeName);
        TypeDefinition emitType = FindType(assembly.MainModule.Types, EmitTypeName);
        IReadOnlyDictionary<int, string> codeNames = placeholderCode.Fields
            .Where(static field => field.HasConstant)
            .ToDictionary(static field => Convert.ToInt32(field.Constant), static field => field.Name);

        List<PlaceholderMetadata> placeholders = [.. emitType.Methods
            .Select(method => (Method: method, Attribute: method.CustomAttributes.SingleOrDefault(
                static attribute => attribute.AttributeType.FullName == PlaceholderAttributeName)))
            .Where(static item => item.Attribute is not null)
            .Select(item => ReadMetadata(item.Method, item.Attribute!, codeNames))];

        Assert.IsNotEmpty(placeholders, $"No primitive placeholders were discovered on '{EmitTypeName}'.");

        PlaceholderMetadata[] nonPrimitive = [.. placeholders.Where(static placeholder => !placeholder.IsPrimitive)];
        Assert.HasCount(0, nonPrimitive, $"IL.Emit unexpectedly contains non-primitive placeholders: {string.Join(", ", nonPrimitive.Select(static item => item.CodeName))}.");

        List<PrimitivePlaceholderProbe> probes = [.. placeholders
            .GroupBy(static placeholder => placeholder.CodeName, StringComparer.Ordinal)
            .Select(CreateProbe)
            .OrderBy(static probe => probe.CodeName, StringComparer.Ordinal)];

        Assert.HasCount(
            placeholders.Select(static placeholder => placeholder.CodeName).Distinct(StringComparer.Ordinal).Count(),
            probes,
            "Each distinct primitive PlaceholderCode must have exactly one generated probe.");

        return new PrimitiveReplacementProbeSet(CreateSource(probes), probes);
    }

    private static PrimitivePlaceholderProbe CreateProbe(IGrouping<string, PlaceholderMetadata> group)
    {
        PlaceholderMetadata selected = group
            .OrderBy(static placeholder => placeholder.OperandName == "TypeRef" ? 0 : 1)
            .ThenBy(static placeholder => placeholder.MethodName, StringComparer.Ordinal)
            .First();

        bool[] prefixValues = [.. group.Select(static placeholder => placeholder.IsPrefix).Distinct()];
        Assert.HasCount(1, prefixValues, $"PlaceholderCode '{group.Key}' has inconsistent prefix metadata.");

        if (!FinalizedCodes.TryGetValue(group.Key, out Code expectedCode)
            && !Enum.TryParse(group.Key, ignoreCase: false, out expectedCode))
        {
            throw new AssertFailedException($"No independent Cecil opcode expectation exists for PlaceholderCode '{group.Key}'.");
        }

        return new PrimitivePlaceholderProbe(
            group.Key,
            selected.OperandName,
            selected.IsPrefix,
            expectedCode,
            "Probe_" + group.Key);
    }

    private static PlaceholderMetadata ReadMetadata(
        MethodDefinition method,
        CustomAttribute attribute,
        IReadOnlyDictionary<int, string> codeNames)
    {
        Assert.HasCount(4, attribute.ConstructorArguments, $"Unexpected PlaceholderAttribute shape on '{method.FullName}'.");

        int codeValue = Convert.ToInt32(attribute.ConstructorArguments[0].Value);
        int operandValue = Convert.ToInt32(attribute.ConstructorArguments[1].Value);

        Assert.IsTrue(codeNames.TryGetValue(codeValue, out string? codeName), $"Unknown PlaceholderCode value '{codeValue}' on '{method.FullName}'.");

        return new PlaceholderMetadata(
            codeName!,
            GetEnumArgumentName(attribute.ConstructorArguments[1], operandValue),
            Convert.ToBoolean(attribute.ConstructorArguments[2].Value),
            Convert.ToBoolean(attribute.ConstructorArguments[3].Value),
            method.Name);
    }

    private static string GetEnumArgumentName(CustomAttributeArgument argument, int value)
    {
        TypeDefinition operandType = argument.Type.Resolve();
        FieldDefinition? field = operandType.Fields.SingleOrDefault(
            candidate => candidate.HasConstant && Convert.ToInt32(candidate.Constant) == value);

        return field?.Name
            ?? throw new AssertFailedException($"Unknown {operandType.FullName} value '{value}'.");
    }

    private static TypeDefinition FindType(IEnumerable<TypeDefinition> types, string fullName)
    {
        foreach (TypeDefinition type in types)
        {
            if (type.FullName == fullName)
            {
                return type;
            }

            TypeDefinition? nested = FindTypeOrDefault(type.NestedTypes, fullName);
            if (nested is not null)
            {
                return nested;
            }
        }

        throw new AssertFailedException($"Metadata type '{fullName}' was not found.");
    }

    private static TypeDefinition? FindTypeOrDefault(IEnumerable<TypeDefinition> types, string fullName)
    {
        foreach (TypeDefinition type in types)
        {
            if (type.FullName == fullName)
            {
                return type;
            }

            TypeDefinition? nested = FindTypeOrDefault(type.NestedTypes, fullName);
            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
    }

    private static string CreateSource(IReadOnlyList<PrimitivePlaceholderProbe> probes)
    {
        var source = new StringBuilder(
            """
            using System;
            using Nebulae.Runtime.Emit.Inline;

            internal struct ProbeValue
            {
            }

            internal class ProbeTarget
            {
                public int InstanceField = 1;
                public static int StaticField = 2;

                public ProbeTarget()
                {
                }

                public static void StaticMethod()
                {
                }

                public virtual void VirtualMethod()
                {
                }
            }

            internal static class ReplacementProbes
            {
            """);

        foreach (PrimitivePlaceholderProbe probe in probes)
        {
            AppendProbeMethod(source, probe);
        }

        return source.AppendLine("}").ToString();
    }

    private static void AppendProbeMethod(StringBuilder source, PrimitivePlaceholderProbe probe)
    {
        string signature = probe.CodeName switch
        {
            "Arglist" => "(__arglist)",
            "Ldarg" or "Ldarga" or "Starg" => "(int value)",
            _ => "()",
        };

        source.AppendLine()
            .Append("    public static void ").Append(probe.MethodName).Append(signature).AppendLine()
            .AppendLine("    {");

        foreach (string statement in GetStatements(probe))
        {
            source.Append("        ").AppendLine(statement);
        }

        source.AppendLine("    }");
    }

    private static IReadOnlyList<string> GetStatements(PrimitivePlaceholderProbe probe)
    {
        if (probe.CodeName == "Nop")
        {
            return ["IL.Emit.Nop();", "return;"];
        }

        if (probe.IsPrefix)
        {
            return GetPrefixStatements(probe.CodeName);
        }

        string invocation = probe.OperandName switch
        {
            "None" => $"IL.Emit.{probe.CodeName}();",
            "Argument" => probe.CodeName == "Starg"
                ? "IL.Emit.Ldc_I4(0);\n        IL.Emit.Starg(value);"
                : $"IL.Emit.{probe.CodeName}(value);",
            "Variable" => probe.CodeName switch
            {
                "Stloc" => "IL.Emit.Ldc_I4(0);\n        IL.Emit.Stloc(out int value);",
                _ => $"IL.Emit.Ldc_I4(0);\n        IL.Emit.Stloc(out int value);\n        IL.Emit.{probe.CodeName}(value);",
            },
            "Byte" => $"IL.Emit.{probe.CodeName}(1);",
            "Int32" => $"IL.Emit.{probe.CodeName}(1000);",
            "Int64" => $"IL.Emit.{probe.CodeName}(1234567890123L);",
            "Single" => $"IL.Emit.{probe.CodeName}(1.25F);",
            "Double" => $"IL.Emit.{probe.CodeName}(2.5D);",
            "String" => $"IL.Emit.{probe.CodeName}(\"probe\");",
            "Branch" => $"IL.Emit.{probe.CodeName}(\"target\");\n        IL.Label(\"target\");\n        IL.Emit.Ret();",
            "Branches" => $"IL.Emit.{probe.CodeName}(\"target\");\n        IL.Label(\"target\");\n        IL.Emit.Ret();",
            "TypeRef" => $"IL.Emit.{probe.CodeName}(typeof(ProbeValue));",
            "FieldRef" => CreateFieldInvocation(probe.CodeName),
            "MethodRef" => CreateMethodInvocation(probe.CodeName),
            "Signature" => "IL.Emit.Calli(IL.Ref(typeof(ProbeTarget)).Method(nameof(ProbeTarget.StaticMethod), typeof(void)));",
            _ => throw new AssertFailedException($"No probe source generator exists for operand '{probe.OperandName}'."),
        };

        return [invocation, "throw IL.Fail();"];
    }

    private static IReadOnlyList<string> GetPrefixStatements(string codeName)
    {
        string statements = codeName switch
        {
            "Unaligned" => "IL.Emit.Unaligned(1);\n        IL.Emit.Ldind_I4();",
            "Volatile" => $"IL.Emit.Volatile();\n        {CreateFieldInvocation("Ldsfld")}",
            "Tail" => $"IL.Emit.Tail();\n        {CreateMethodInvocation("Call")}\n        IL.Emit.Ret();",
            "Constrained" => $"IL.Emit.Constrained(typeof(ProbeValue));\n        {CreateMethodInvocation("Callvirt")}",
            "Readonly" => "IL.Emit.Readonly();\n        IL.Emit.Ldelema(typeof(ProbeValue));",
            "No" => $"IL.Emit.No(1);\n        {CreateFieldInvocation("Ldfld")}",
            _ => throw new AssertFailedException($"No valid companion instruction exists for prefix '{codeName}'."),
        };

        return [statements, "throw IL.Fail();"];
    }

    private static string CreateFieldInvocation(string codeName)
    {
        string fieldName = codeName is "Ldsfld" or "Ldsflda" or "Stsfld"
            ? "StaticField"
            : "InstanceField";

        return $"IL.Emit.{codeName}(IL.Ref(typeof(ProbeTarget)).Field(nameof(ProbeTarget.{fieldName})));";
    }

    private static string CreateMethodInvocation(string codeName)
    {
        return codeName switch
        {
            "Newobj" => "IL.Emit.Newobj(IL.Ref(typeof(ProbeTarget)).Constructor(Type.EmptyTypes));",
            "Callvirt" or "Ldvirtftn" => $"IL.Emit.{codeName}(IL.Ref(typeof(ProbeTarget)).Method(nameof(ProbeTarget.VirtualMethod), typeof(void)));",
            _ => $"IL.Emit.{codeName}(IL.Ref(typeof(ProbeTarget)).Method(nameof(ProbeTarget.StaticMethod), typeof(void)));",
        };
    }

    private sealed record PlaceholderMetadata(
        string CodeName,
        string OperandName,
        bool IsPrefix,
        bool IsPrimitive,
        string MethodName);
}

internal sealed record PrimitiveReplacementProbeSet(
    string Source,
    IReadOnlyList<PrimitivePlaceholderProbe> Probes);

internal sealed record PrimitivePlaceholderProbe(
    string CodeName,
    string OperandName,
    bool IsPrefix,
    Code ExpectedCode,
    string MethodName);
