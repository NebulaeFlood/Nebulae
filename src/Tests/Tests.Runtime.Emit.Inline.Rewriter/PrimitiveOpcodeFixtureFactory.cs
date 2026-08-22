using Mono.Cecil;
using Mono.Cecil.Cil;
using Nebulae.Runtime.Emit.Inline;
using Tests.Runtime.Emit.Inline.Infrastructure;

namespace Tests.Runtime.Emit.Inline.Rewriter;

internal sealed record PrimitiveOpcodeFixture(
    string AssemblyPath,
    IReadOnlyDictionary<string, string> MethodNames);

internal static class PrimitiveOpcodeFixtureFactory
{
    public static PrimitiveOpcodeFixture Create(string outputDirectory)
    {
        string assemblyPath = System.IO.Path.Combine(outputDirectory, "PrimitiveOpcodeFixture.dll");
        using AssemblyDefinition placeholders = AssemblyDefinition.ReadAssembly(typeof(IL).Assembly.Location);
        using AssemblyDefinition assembly = AssemblyDefinition.CreateAssembly(
            new AssemblyNameDefinition("PrimitiveOpcodeFixture", new Version(1, 0, 0, 0)),
            "PrimitiveOpcodeFixture",
            ModuleKind.Dll);
        ModuleDefinition module = assembly.MainModule;

        var scenarioType = new TypeDefinition(
            "Generated",
            "OpcodeScenarios",
            TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed,
            module.TypeSystem.Object);
        module.Types.Add(scenarioType);

        TypeDefinition targetType = CreateTargetType(module);
        TypeDefinition opaqueValueType = CreateOpaqueValueType(module);
        ReferenceMethods referenceMethods = GetReferenceMethods(placeholders, module);
        MethodReference getTypeFromHandle = module.ImportReference(
            typeof(Type).GetMethod(
                nameof(Type.GetTypeFromHandle),
                [typeof(RuntimeTypeHandle)])!);
        FieldReference emptyTypes = module.ImportReference(
            typeof(Type).GetField(nameof(Type.EmptyTypes))!);
        MethodDefinition targetMethod = targetType.Methods.Single(
            static method => method.Name == "Target");
        FieldDefinition targetField = targetType.Fields.Single();

        PlaceholderMethod[] primitiveMethods = [.. GetPlaceholderMethods(placeholders)
            .Where(static method => method.IsPrimitive)
            .GroupBy(static method => method.Code, StringComparer.Ordinal)
            .Select(static group => group
                .OrderBy(static method => method.Definition.Parameters.Any(
                    static parameter => parameter.ParameterType is ByReferenceType))
                .ThenBy(static method => method.Definition.FullName, StringComparer.Ordinal)
                .First())
            .OrderBy(static method => method.Code, StringComparer.Ordinal)];
        var methodNames = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (PlaceholderMethod placeholder in primitiveMethods)
        {
            string methodName = $"Opcode_{placeholder.Code}";
            var method = new MethodDefinition(
                methodName,
                MethodAttributes.Public | MethodAttributes.Static,
                module.TypeSystem.Void);
            scenarioType.Methods.Add(method);
            method.Body.InitLocals = true;
            ILProcessor il = method.Body.GetILProcessor();

            EmitOperand(
                il,
                method,
                placeholder,
                module,
                targetType,
                opaqueValueType,
                targetMethod,
                targetField,
                referenceMethods,
                getTypeFromHandle,
                emptyTypes);

            MethodReference placeholderCall = module.ImportReference(placeholder.Definition);

            if (placeholderCall.HasGenericParameters)
            {
                var genericCall = new GenericInstanceMethod(placeholderCall);

                foreach (GenericParameter _ in placeholderCall.GenericParameters)
                {
                    genericCall.GenericArguments.Add(module.TypeSystem.Int32);
                }

                placeholderCall = genericCall;
            }

            il.Append(Instruction.Create(OpCodes.Call, placeholderCall));

            if (placeholder.Operand is "Branch" or "Branches")
            {
                for (int i = 0; i < 200; i++)
                {
                    il.Append(Instruction.Create(OpCodes.Nop));
                }

                il.Append(Instruction.Create(OpCodes.Ldstr, "target"));
                il.Append(Instruction.Create(OpCodes.Call, referenceMethods.Label));
            }

            il.Append(Instruction.Create(OpCodes.Ret));
            methodNames.Add(placeholder.Code, methodName);
        }

        assembly.Write(assemblyPath);
        return new PrimitiveOpcodeFixture(assemblyPath, methodNames);
    }

    private static TypeDefinition CreateTargetType(ModuleDefinition module)
    {
        var type = new TypeDefinition(
            "Generated",
            "ReferenceTarget",
            TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.BeforeFieldInit,
            module.TypeSystem.Object);
        module.Types.Add(type);

        type.Fields.Add(new FieldDefinition(
            "Value",
            FieldAttributes.Public | FieldAttributes.Static,
            module.TypeSystem.Int32));

        var constructor = new MethodDefinition(
            ".ctor",
            MethodAttributes.Public |
            MethodAttributes.HideBySig |
            MethodAttributes.SpecialName |
            MethodAttributes.RTSpecialName,
            module.TypeSystem.Void)
        {
            HasThis = true
        };
        constructor.Body.GetILProcessor().Append(Instruction.Create(OpCodes.Ret));
        type.Methods.Add(constructor);

        var method = new MethodDefinition(
            "Target",
            MethodAttributes.Public | MethodAttributes.Static,
            module.TypeSystem.Void);
        method.Body.GetILProcessor().Append(Instruction.Create(OpCodes.Ret));
        type.Methods.Add(method);
        return type;
    }

    private static TypeDefinition CreateOpaqueValueType(ModuleDefinition module)
    {
        var type = new TypeDefinition(
            "Generated",
            "OpaqueValue",
            TypeAttributes.Public |
            TypeAttributes.Sealed |
            TypeAttributes.SequentialLayout |
            TypeAttributes.BeforeFieldInit,
            module.ImportReference(typeof(ValueType)));
        module.Types.Add(type);
        return type;
    }

    private static void EmitOperand(
        ILProcessor il,
        MethodDefinition method,
        PlaceholderMethod placeholder,
        ModuleDefinition module,
        TypeDefinition targetType,
        TypeDefinition opaqueValueType,
        MethodDefinition targetMethod,
        FieldDefinition targetField,
        ReferenceMethods referenceMethods,
        MethodReference getTypeFromHandle,
        FieldReference emptyTypes)
    {
        switch (placeholder.Operand)
        {
            case "None":
                return;
            case "Argument":
                method.Parameters.Add(new ParameterDefinition(
                    "value",
                    ParameterAttributes.None,
                    module.TypeSystem.Int32));
                il.Append(Instruction.Create(OpCodes.Ldarg_0));
                return;
            case "Variable":
                method.Body.Variables.Add(new VariableDefinition(module.TypeSystem.Int32));
                il.Append(Instruction.Create(OpCodes.Ldloc_0));
                return;
            case "Byte":
                il.Append(Instruction.Create(OpCodes.Ldc_I4_1));
                return;
            case "Int32":
                il.Append(Instruction.Create(OpCodes.Ldc_I4, 1000));
                return;
            case "Int64":
                il.Append(Instruction.Create(OpCodes.Ldc_I8, 1000L));
                return;
            case "Single":
                il.Append(Instruction.Create(OpCodes.Ldc_R4, 1.25f));
                return;
            case "Double":
                il.Append(Instruction.Create(OpCodes.Ldc_R8, 2.5d));
                return;
            case "String":
            case "Branch":
                il.Append(Instruction.Create(OpCodes.Ldstr, "target"));
                return;
            case "Branches":
                il.Append(Instruction.Create(OpCodes.Ldc_I4_1));
                il.Append(Instruction.Create(OpCodes.Newarr, module.TypeSystem.String));
                il.Append(Instruction.Create(OpCodes.Dup));
                il.Append(Instruction.Create(OpCodes.Ldc_I4_0));
                il.Append(Instruction.Create(OpCodes.Ldstr, "target"));
                il.Append(Instruction.Create(OpCodes.Stelem_Ref));
                return;
            case "TypeRef":
                EmitSystemType(
                    il,
                    placeholder.Code is "Ldelem" or "Stelem"
                        ? opaqueValueType
                        : module.TypeSystem.Int32,
                    getTypeFromHandle);
                return;
            case "FieldRef":
                EmitReferenceType(il, targetType, referenceMethods.Type, getTypeFromHandle);
                il.Append(Instruction.Create(OpCodes.Ldstr, targetField.Name));
                il.Append(Instruction.Create(OpCodes.Callvirt, referenceMethods.Field));
                return;
            case "MethodRef":
            case "Signature":
                EmitReferenceType(il, targetType, referenceMethods.Type, getTypeFromHandle);

                if (placeholder.Code == "Newobj")
                {
                    il.Append(Instruction.Create(OpCodes.Ldsfld, emptyTypes));
                    il.Append(Instruction.Create(OpCodes.Callvirt, referenceMethods.Constructor));
                }
                else
                {
                    il.Append(Instruction.Create(OpCodes.Ldstr, targetMethod.Name));
                    il.Append(Instruction.Create(OpCodes.Ldsfld, emptyTypes));
                    il.Append(Instruction.Create(OpCodes.Callvirt, referenceMethods.Method));
                }

                return;
            default:
                throw new InvalidOperationException(
                    $"Unsupported test operand '{placeholder.Operand}' for '{placeholder.Code}'.");
        }
    }

    private static void EmitReferenceType(
        ILProcessor il,
        TypeReference type,
        MethodReference referenceType,
        MethodReference getTypeFromHandle)
    {
        EmitSystemType(il, type, getTypeFromHandle);
        il.Append(Instruction.Create(OpCodes.Call, referenceType));
    }

    private static void EmitSystemType(
        ILProcessor il,
        TypeReference type,
        MethodReference getTypeFromHandle)
    {
        il.Append(Instruction.Create(OpCodes.Ldtoken, type));
        il.Append(Instruction.Create(OpCodes.Call, getTypeFromHandle));
    }

    private static PlaceholderMethod[] GetPlaceholderMethods(AssemblyDefinition assembly)
    {
        return [.. GetAllTypes(assembly.MainModule.Types)
            .SelectMany(static type => type.Methods)
            .Select(static method => (Method: method, Attribute: method.CustomAttributes.SingleOrDefault(
                static attribute => attribute.AttributeType.FullName ==
                    "Nebulae.Runtime.Emit.Inline.PlaceholderAttribute")))
            .Where(static item => item.Attribute is not null)
            .Select(static item => new PlaceholderMethod(
                item.Method,
                GetEnumName(item.Attribute!.ConstructorArguments[0]),
                GetEnumName(item.Attribute.ConstructorArguments[1]),
                (bool)item.Attribute.ConstructorArguments[3].Value))];
    }

    private static ReferenceMethods GetReferenceMethods(
        AssemblyDefinition assembly,
        ModuleDefinition targetModule)
    {
        MethodDefinition[] methods = [.. GetAllTypes(assembly.MainModule.Types)
            .SelectMany(static type => type.Methods)
            .Where(static method => method.CustomAttributes.Any(
                static attribute => attribute.AttributeType.FullName ==
                    "Nebulae.Runtime.Emit.Inline.ReferenceAttribute"))];

        MethodDefinition Get(string kind, Func<MethodDefinition, bool>? predicate = null)
        {
            return methods.Single(method =>
                GetEnumName(method.CustomAttributes.Single(
                    static attribute => attribute.AttributeType.FullName ==
                        "Nebulae.Runtime.Emit.Inline.ReferenceAttribute")
                    .ConstructorArguments[0]) == kind
                && (predicate?.Invoke(method) ?? true));
        }

        return new ReferenceMethods(
            targetModule.ImportReference(Get("Type")),
            targetModule.ImportReference(Get("Constructor")),
            targetModule.ImportReference(Get("Field")),
            targetModule.ImportReference(Get(
                "Method",
                static method => method.Parameters.Count == 2)),
            targetModule.ImportReference(GetPlaceholderMethods(assembly).Single(
                static method => method.Code == "Label").Definition));
    }

    private static IEnumerable<TypeDefinition> GetAllTypes(
        IEnumerable<TypeDefinition> source)
    {
        foreach (TypeDefinition type in source)
        {
            yield return type;

            foreach (TypeDefinition nested in GetAllTypes(type.NestedTypes))
            {
                yield return nested;
            }
        }
    }

    private static string GetEnumName(CustomAttributeArgument argument)
    {
        long value = Convert.ToInt64(argument.Value, System.Globalization.CultureInfo.InvariantCulture);
        FieldDefinition field = argument.Type.Resolve().Fields.Single(candidate =>
            candidate.HasConstant
            && Convert.ToInt64(candidate.Constant, System.Globalization.CultureInfo.InvariantCulture) == value);
        return field.Name;
    }

    private sealed record PlaceholderMethod(
        MethodDefinition Definition,
        string Code,
        string Operand,
        bool IsPrimitive);

    private sealed record ReferenceMethods(
        MethodReference Type,
        MethodReference Constructor,
        MethodReference Field,
        MethodReference Method,
        MethodReference Label);
}
