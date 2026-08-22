using Mono.Cecil;
using Mono.Cecil.Cil;
using Tests.Runtime.Emit.Inline.Infrastructure;

namespace Tests.Runtime.Emit.Inline.Rewriter;

[TestClass]
public sealed class PrimitiveOpcodeConformanceTests
{
    [TestMethod]
    public void PrimitivePlaceholderCatalog_AfterRewrite_MapsEveryDeclaredCecilOpcode()
    {
        using var directory = new TemporaryDirectory("primitive-opcodes");
        PrimitiveOpcodeFixture fixture = PrimitiveOpcodeFixtureFactory.Create(
            directory.DirectoryPath);

        InlineILTaskResult rewrite = InlineILTaskHarness.Execute(
            fixture.AssemblyPath,
            CompilationHarness.DefaultReferencePaths);

        Assert.IsTrue(
            rewrite.Success,
            string.Join(Environment.NewLine, rewrite.Errors.Select(static error => error.Message)));
        Assert.IsEmpty(rewrite.Errors);
        Assert.HasCount(176, fixture.MethodNames);
        Assert.IsFalse(
            AssemblyInspector.ReferencesAssembly(
                fixture.AssemblyPath,
                "Nebulae.Runtime.Emit.Inline"));

        using AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(fixture.AssemblyPath);
        TypeDefinition scenarios = assembly.MainModule.GetType("Generated.OpcodeScenarios")!;

        foreach ((string placeholderCode, string methodName) in fixture.MethodNames)
        {
            MethodDefinition method = scenarios.Methods.Single(candidate => candidate.Name == methodName);
            Code actual = method.Body.Instructions[0].OpCode.Code;
            Code expected = GetExpectedOutputCode(placeholderCode);

            Assert.AreEqual(
                expected,
                actual,
                $"Placeholder '{placeholderCode}' produced '{actual}' in '{method.FullName}'.");
        }
    }

    private static Code GetExpectedOutputCode(string placeholderCode)
    {
        return placeholderCode switch
        {
            "Ldarg" => Code.Ldarg_0,
            "Ldarga" => Code.Ldarga_S,
            "Ldloc" => Code.Ldloc_0,
            "Ldloca" => Code.Ldloca_S,
            "Starg" => Code.Starg_S,
            "Stloc" => Code.Stloc_0,
            "Ldelem" => Code.Ldelem_Any,
            "Stelem" => Code.Stelem_Any,
            _ => Enum.Parse<Code>(placeholderCode, ignoreCase: false)
        };
    }
}
