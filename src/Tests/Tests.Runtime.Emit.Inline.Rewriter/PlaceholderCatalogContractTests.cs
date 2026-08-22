using Nebulae.Runtime.Emit.Inline;
using System.Reflection;

namespace Tests.Runtime.Emit.Inline.Rewriter;

[TestClass]
public sealed class PlaceholderCatalogContractTests
{
    private static readonly string[] ExpectedExtensionCodes =
    [
        "Fail",
        "Label",
        "Pop",
        "Pop",
        "Pop",
        "Pop",
        "Push",
        "Push",
        "Push",
        "Push",
        "Ret"
    ];

    private static readonly string[] ExpectedPrefixCodes =
        ["Constrained", "No", "Readonly", "Tail", "Unaligned", "Volatile"];

    private static readonly string[] ExpectedReferenceKinds =
    [
        "Constructor",
        "Event",
        "EventAdd",
        "EventRaise",
        "EventRemove",
        "Field",
        "Generic",
        "Indexer",
        "IndexerGet",
        "IndexerSet",
        "Method",
        "MethodMakeGeneric",
        "Placeholder",
        "Property",
        "PropertyGet",
        "PropertySet",
        "Type"
    ];

    [TestMethod]
    public void PlaceholderApi_CompiledMetadata_MatchesIndependentOperationCatalog()
    {
        PlaceholderContract[] contracts = GetPlaceholderContracts();

        Assert.HasCount(193, contracts);
        CollectionAssert.AreEqual(
            ExpectedCodes,
            contracts
                .Select(static contract => contract.Code)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToArray());

        var expectedMultiplicity = ExpectedCodes.ToDictionary(
            static code => code,
            static _ => 1,
            StringComparer.Ordinal);
        expectedMultiplicity["Ldarg"] = 2;
        expectedMultiplicity["Ldarga"] = 2;
        expectedMultiplicity["Starg"] = 2;
        expectedMultiplicity["Stloc"] = 2;
        expectedMultiplicity["Ldtoken"] = 3;
        expectedMultiplicity["Pop"] = 5;
        expectedMultiplicity["Push"] = 4;
        expectedMultiplicity["Ret"] = 2;

        foreach ((string code, int count) in expectedMultiplicity)
        {
            Assert.AreEqual(
                count,
                contracts.Count(contract => contract.Code == code),
                $"Unexpected placeholder overload count for '{code}'.");
        }

        var expectedOperandCounts = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["None"] = 123,
            ["Value"] = 4,
            ["Argument"] = 6,
            ["Variable"] = 4,
            ["Byte"] = 2,
            ["Int32"] = 1,
            ["Int64"] = 1,
            ["Single"] = 1,
            ["Double"] = 1,
            ["String"] = 2,
            ["Branch"] = 14,
            ["Branches"] = 1,
            ["TypeRef"] = 18,
            ["FieldRef"] = 7,
            ["MethodRef"] = 7,
            ["Signature"] = 1
        };
        Dictionary<string, int> actualOperandCounts = contracts
            .GroupBy(static contract => contract.Operand, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => group.Count(),
                StringComparer.Ordinal);

        CollectionAssert.AreEquivalent(
            expectedOperandCounts.Keys.ToArray(),
            actualOperandCounts.Keys.ToArray());

        foreach ((string operand, int count) in expectedOperandCounts)
        {
            Assert.AreEqual(count, actualOperandCounts[operand], operand);
        }
    }

    [TestMethod]
    public void PlaceholderApi_PrefixAndExtensionMetadata_MatchesDeclaredContract()
    {
        PlaceholderContract[] contracts = GetPlaceholderContracts();

        CollectionAssert.AreEquivalent(
            ExpectedPrefixCodes,
            contracts
                .Where(static contract => contract.IsPrefix)
                .Select(static contract => contract.Code)
                .ToArray());
        CollectionAssert.AreEquivalent(
            ExpectedExtensionCodes,
            contracts
                .Where(static contract => !contract.IsPrimitive)
                .Select(static contract => contract.Code)
                .ToArray());
    }

    [TestMethod]
    public void ReferenceApi_CompiledMetadata_ExposesEveryReferenceKind()
    {
        Assembly assembly = typeof(IL).Assembly;
        Type[] publicTypes =
        [
            typeof(IL),
            typeof(IL.Emit),
            typeof(TypeRef),
            typeof(MethodRef),
            typeof(PropertyRef),
            typeof(IndexerRef),
            typeof(EventRef),
            typeof(FieldRef),
            typeof(GenericRef)
        ];
        var referenceKinds = new List<string>();

        foreach (Type type in publicTypes)
        {
            AddReferenceKinds(type.CustomAttributes, referenceKinds);

            foreach (MethodInfo method in type.GetMethods(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                AddReferenceKinds(method.CustomAttributes, referenceKinds);
            }
        }

        Assert.HasCount(25, referenceKinds);
        CollectionAssert.AreEqual(
            ExpectedReferenceKinds,
            referenceKinds
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToArray());

        Assert.AreSame(assembly, typeof(TypeRef).Assembly);
    }

    private static PlaceholderContract[] GetPlaceholderContracts()
    {
        return [.. typeof(IL)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Concat(typeof(IL.Emit).GetMethods(
                BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Select(static method => (Method: method, Attribute: method.CustomAttributes.SingleOrDefault(
                static attribute => attribute.AttributeType.FullName ==
                    "Nebulae.Runtime.Emit.Inline.PlaceholderAttribute")))
            .Where(static item => item.Attribute is not null)
            .Select(static item => new PlaceholderContract(
                item.Method,
                GetEnumName(item.Attribute!.ConstructorArguments[0]),
                GetEnumName(item.Attribute.ConstructorArguments[1]),
                (bool)item.Attribute.ConstructorArguments[2].Value!,
                (bool)item.Attribute.ConstructorArguments[3].Value!))];
    }

    private static void AddReferenceKinds(
        IEnumerable<CustomAttributeData> attributes,
        List<string> collector)
    {
        foreach (CustomAttributeData attribute in attributes)
        {
            if (attribute.AttributeType.FullName ==
                "Nebulae.Runtime.Emit.Inline.ReferenceAttribute")
            {
                collector.Add(GetEnumName(attribute.ConstructorArguments[0]));
            }
        }
    }

    private static string GetEnumName(CustomAttributeTypedArgument argument)
    {
        return Enum.GetName(argument.ArgumentType, argument.Value!)
            ?? throw new InvalidOperationException(
                $"Cannot resolve enum value '{argument.Value}' for '{argument.ArgumentType}'.");
    }

    private sealed record PlaceholderContract(
        MethodInfo Method,
        string Code,
        string Operand,
        bool IsPrefix,
        bool IsPrimitive);

    private static string[] ExpectedCodes { get; } = [.. """
        Add
        Add_Ovf
        Add_Ovf_Un
        And
        Arglist
        Beq
        Bge
        Bge_Un
        Bgt
        Bgt_Un
        Ble
        Ble_Un
        Blt
        Blt_Un
        Bne_Un
        Box
        Br
        Break
        Brfalse
        Brtrue
        Call
        Calli
        Callvirt
        Castclass
        Ceq
        Cgt
        Cgt_Un
        Ckfinite
        Clt
        Clt_Un
        Constrained
        Conv_I
        Conv_I1
        Conv_I2
        Conv_I4
        Conv_I8
        Conv_Ovf_I
        Conv_Ovf_I1
        Conv_Ovf_I1_Un
        Conv_Ovf_I2
        Conv_Ovf_I2_Un
        Conv_Ovf_I4
        Conv_Ovf_I4_Un
        Conv_Ovf_I8
        Conv_Ovf_I8_Un
        Conv_Ovf_I_Un
        Conv_Ovf_U
        Conv_Ovf_U1
        Conv_Ovf_U1_Un
        Conv_Ovf_U2
        Conv_Ovf_U2_Un
        Conv_Ovf_U4
        Conv_Ovf_U4_Un
        Conv_Ovf_U8
        Conv_Ovf_U8_Un
        Conv_Ovf_U_Un
        Conv_R4
        Conv_R8
        Conv_R_Un
        Conv_U
        Conv_U1
        Conv_U2
        Conv_U4
        Conv_U8
        Cpblk
        Cpobj
        Div
        Div_Un
        Dup
        Endfilter
        Endfinally
        Fail
        Initblk
        Initobj
        Isinst
        Jmp
        Label
        Ldarg
        Ldarga
        Ldc_I4
        Ldc_I8
        Ldc_R4
        Ldc_R8
        Ldelem
        Ldelem_I
        Ldelem_I1
        Ldelem_I2
        Ldelem_I4
        Ldelem_I8
        Ldelem_R4
        Ldelem_R8
        Ldelem_Ref
        Ldelem_U1
        Ldelem_U2
        Ldelem_U4
        Ldelema
        Ldfld
        Ldflda
        Ldftn
        Ldind_I
        Ldind_I1
        Ldind_I2
        Ldind_I4
        Ldind_I8
        Ldind_R4
        Ldind_R8
        Ldind_Ref
        Ldind_U1
        Ldind_U2
        Ldind_U4
        Ldlen
        Ldloc
        Ldloca
        Ldnull
        Ldobj
        Ldsfld
        Ldsflda
        Ldstr
        Ldtoken
        Ldvirtftn
        Leave
        Localloc
        Mkrefany
        Mul
        Mul_Ovf
        Mul_Ovf_Un
        Neg
        Newarr
        Newobj
        No
        Nop
        Not
        Or
        Pop
        Push
        Readonly
        Refanytype
        Refanyval
        Rem
        Rem_Un
        Ret
        Rethrow
        Shl
        Shr
        Shr_Un
        Sizeof
        Starg
        Stelem
        Stelem_I
        Stelem_I1
        Stelem_I2
        Stelem_I4
        Stelem_I8
        Stelem_R4
        Stelem_R8
        Stelem_Ref
        Stfld
        Stind_I
        Stind_I1
        Stind_I2
        Stind_I4
        Stind_I8
        Stind_R4
        Stind_R8
        Stind_Ref
        Stloc
        Stobj
        Stsfld
        Sub
        Sub_Ovf
        Sub_Ovf_Un
        Switch
        Tail
        Throw
        Unaligned
        Unbox
        Unbox_Any
        Volatile
        Xor
        """
        .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Order(StringComparer.Ordinal)];
}
