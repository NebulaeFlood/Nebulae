using Mono.Cecil;
using Mono.Cecil.Cil;
using Tests.Runtime.Emit.Inline.Helpers;

namespace Tests.Runtime.Emit.Inline.Replacement;

[TestClass]
[DoNotParallelize]
public sealed class PrimitiveReplacementTests
{
    [TestMethod]
    public async Task PrimitivePlaceholders_AreReplacedWithCorrespondingOpCodes()
    {
        PrimitiveReplacementProbeSet probeSet = PrimitiveReplacementProbeGenerator.Create();
        RewrittenProbeResult result = await BuildScenarioTestHelpers.RewriteGeneratedProbeAsync(probeSet.Source);

        Assert.AreEqual(0, result.ExitCode, $"Generated primitive replacement probes failed to build.{Environment.NewLine}{result.Output}");
        Assert.IsNotNull(result.AssemblyBytes, "The successful probe build did not return rewritten assembly bytes.");

        using var stream = new MemoryStream(result.AssemblyBytes, writable: false);
        using AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(stream);
        TypeDefinition? probeType = assembly.MainModule.Types.SingleOrDefault(static type => type.Name == "ReplacementProbes");
        Assert.IsNotNull(probeType, "Generated probe type 'ReplacementProbes' was not found.");

        var failures = new List<string>();
        foreach (PrimitivePlaceholderProbe probe in probeSet.Probes)
        {
            MethodDefinition? method = probeType.Methods.SingleOrDefault(candidate => candidate.Name == probe.MethodName);
            if (method is null)
            {
                failures.Add($"{probe.CodeName}: generated method '{probe.MethodName}' was not found.");
                continue;
            }

            if (!method.Body.Instructions.Any(instruction => instruction.OpCode.Code == probe.ExpectedCode))
            {
                failures.Add(
                    $"{probe.CodeName}: expected {probe.ExpectedCode}; actual sequence: "
                    + string.Join(", ", method.Body.Instructions.Select(static instruction => instruction.OpCode.Code)));
            }
        }

        foreach (MethodDefinition method in assembly.MainModule.Types.SelectMany(GetMethods))
        {
            if (!method.HasBody)
            {
                continue;
            }

            foreach (Instruction instruction in method.Body.Instructions)
            {
                if (instruction.Operand is MethodReference reference
                    && reference.DeclaringType.Scope is AssemblyNameReference scope
                    && scope.Name == "Nebulae.Runtime.Emit.Inline")
                {
                    failures.Add($"{method.FullName}: unreplaced placeholder call '{reference.FullName}'.");
                }
            }
        }

        if (assembly.MainModule.AssemblyReferences.Any(static reference => reference.Name == "Nebulae.Runtime.Emit.Inline"))
        {
            failures.Add("The rewritten probe assembly still references Nebulae.Runtime.Emit.Inline.");
        }

        Assert.HasCount(
            0,
            failures,
            $"Primitive placeholder replacement mismatches:{Environment.NewLine}{string.Join(Environment.NewLine, failures)}");
    }

    private static IEnumerable<MethodDefinition> GetMethods(TypeDefinition type)
    {
        foreach (MethodDefinition method in type.Methods)
        {
            yield return method;
        }

        foreach (TypeDefinition nestedType in type.NestedTypes)
        {
            foreach (MethodDefinition method in GetMethods(nestedType))
            {
                yield return method;
            }
        }
    }
}
