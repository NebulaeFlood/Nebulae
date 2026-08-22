using Mono.Cecil;
using Mono.Cecil.Cil;
using Nebulae.Runtime.Emit.Inline;
using Tests.Runtime.Emit.Inline.Infrastructure;

namespace Tests.Runtime.Emit.Inline.Rewriter;

[TestClass]
public sealed class ControlFlowRewriteContractTests
{
    [TestMethod]
    public void BranchTarget_WhenOperandProducerIsRemoved_RetargetsToRewrittenInstruction()
    {
        using var directory = new TemporaryDirectory("branch-retarget");
        string assemblyPath = CreateBranchTargetFixture(directory.DirectoryPath);

        InlineILTaskResult rewrite = InlineILTaskHarness.Execute(
            assemblyPath,
            CompilationHarness.DefaultReferencePaths);

        Assert.IsTrue(
            rewrite.Success,
            string.Join(Environment.NewLine, rewrite.Errors.Select(static error => error.Message)));
        using AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(assemblyPath);
        MethodDefinition method = assembly.MainModule
            .GetType("Generated.ControlFlowScenarios")!
            .Methods.Single(static candidate => candidate.Name == "Retarget");
        Instruction branch = method.Body.Instructions[0];
        Instruction rewrittenConstant = method.Body.Instructions[1];

        Assert.IsTrue(branch.OpCode.Code is Code.Br or Code.Br_S);
        Assert.AreSame(rewrittenConstant, branch.Operand);
        Assert.AreEqual(Code.Ldc_I4_1, rewrittenConstant.OpCode.Code);
    }

    [TestMethod]
    public void ExceptionRegion_WhenRemovingProducerWouldCollapseTryRegion_RejectsAndPreservesInput()
    {
        using var directory = new TemporaryDirectory("collapsed-region");
        string assemblyPath = CreateCollapsedExceptionRegionFixture(directory.DirectoryPath);
        string hash = AssemblyInspector.ComputeSha256(assemblyPath);

        InlineILTaskResult rewrite = InlineILTaskHarness.Execute(
            assemblyPath,
            CompilationHarness.DefaultReferencePaths);

        Assert.IsFalse(rewrite.Success);
        Assert.HasCount(1, rewrite.Errors);
        Assert.Contains("only instruction", rewrite.Errors[0].Message ?? string.Empty);
        Assert.Contains("try region", rewrite.Errors[0].Message ?? string.Empty);
        Assert.AreEqual(hash, AssemblyInspector.ComputeSha256(assemblyPath));
    }

    [TestMethod]
    public void LabelAtEndOfMethod_WhenReferencedInstructionHasNoSuccessor_RejectsAndPreservesInput()
    {
        using var directory = new TemporaryDirectory("terminal-label");
        string assemblyPath = CreateTerminalLabelFixture(directory.DirectoryPath);
        string hash = AssemblyInspector.ComputeSha256(assemblyPath);

        InlineILTaskResult rewrite = InlineILTaskHarness.Execute(
            assemblyPath,
            CompilationHarness.DefaultReferencePaths);

        Assert.IsFalse(rewrite.Success);
        Assert.HasCount(1, rewrite.Errors);
        Assert.Contains("end of method", rewrite.Errors[0].Message ?? string.Empty);
        Assert.AreEqual(hash, AssemblyInspector.ComputeSha256(assemblyPath));
    }

    private static string CreateBranchTargetFixture(string outputDirectory)
    {
        string assemblyPath = System.IO.Path.Combine(outputDirectory, "BranchTargetFixture.dll");
        using AssemblyDefinition assembly = CreateAssembly("BranchTargetFixture");
        ModuleDefinition module = assembly.MainModule;
        TypeDefinition type = AddScenarioType(module);
        var method = new MethodDefinition(
            "Retarget",
            MethodAttributes.Public | MethodAttributes.Static,
            module.TypeSystem.Int32);
        type.Methods.Add(method);
        ILProcessor il = method.Body.GetILProcessor();
        Instruction producer = Instruction.Create(OpCodes.Ldc_I4_1);
        Instruction placeholder = Instruction.Create(OpCodes.Call, ImportLdcI4(module));

        il.Append(Instruction.Create(OpCodes.Br, producer));
        il.Append(producer);
        il.Append(placeholder);
        il.Append(Instruction.Create(OpCodes.Ret));
        assembly.Write(assemblyPath);
        return assemblyPath;
    }

    private static string CreateCollapsedExceptionRegionFixture(string outputDirectory)
    {
        string assemblyPath = System.IO.Path.Combine(outputDirectory, "CollapsedRegionFixture.dll");
        using AssemblyDefinition assembly = CreateAssembly("CollapsedRegionFixture");
        ModuleDefinition module = assembly.MainModule;
        TypeDefinition type = AddScenarioType(module);
        var method = new MethodDefinition(
            "CollapsedTry",
            MethodAttributes.Public | MethodAttributes.Static,
            module.TypeSystem.Void);
        type.Methods.Add(method);
        ILProcessor il = method.Body.GetILProcessor();
        Instruction producer = Instruction.Create(OpCodes.Ldc_I4_1);
        Instruction placeholder = Instruction.Create(OpCodes.Call, ImportLdcI4(module));
        Instruction handlerEnd = Instruction.Create(OpCodes.Ret);

        il.Append(producer);
        il.Append(placeholder);
        il.Append(handlerEnd);
        method.Body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Finally)
        {
            TryStart = producer,
            TryEnd = placeholder,
            HandlerStart = placeholder,
            HandlerEnd = handlerEnd
        });
        assembly.Write(assemblyPath);
        return assemblyPath;
    }

    private static string CreateTerminalLabelFixture(string outputDirectory)
    {
        string assemblyPath = System.IO.Path.Combine(outputDirectory, "TerminalLabelFixture.dll");
        using AssemblyDefinition assembly = CreateAssembly("TerminalLabelFixture");
        ModuleDefinition module = assembly.MainModule;
        TypeDefinition type = AddScenarioType(module);
        var method = new MethodDefinition(
            "TerminalLabel",
            MethodAttributes.Public | MethodAttributes.Static,
            module.TypeSystem.Void);
        type.Methods.Add(method);
        ILProcessor il = method.Body.GetILProcessor();
        Instruction label = Instruction.Create(OpCodes.Call, ImportLabel(module));

        il.Append(Instruction.Create(OpCodes.Br, label));
        il.Append(Instruction.Create(OpCodes.Ldstr, "end"));
        il.Append(label);
        assembly.Write(assemblyPath);
        return assemblyPath;
    }

    private static AssemblyDefinition CreateAssembly(string name)
    {
        return AssemblyDefinition.CreateAssembly(
            new AssemblyNameDefinition(name, new Version(1, 0, 0, 0)),
            name,
            ModuleKind.Dll);
    }

    private static TypeDefinition AddScenarioType(ModuleDefinition module)
    {
        var type = new TypeDefinition(
            "Generated",
            "ControlFlowScenarios",
            TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed,
            module.TypeSystem.Object);
        module.Types.Add(type);
        return type;
    }

    private static MethodReference ImportLdcI4(ModuleDefinition module)
    {
        return module.ImportReference(typeof(IL.Emit).GetMethod(
            nameof(IL.Emit.Ldc_I4),
            [typeof(int)])!);
    }

    private static MethodReference ImportLabel(ModuleDefinition module)
    {
        return module.ImportReference(typeof(IL).GetMethod(
            nameof(IL.Label),
            [typeof(string)])!);
    }
}
