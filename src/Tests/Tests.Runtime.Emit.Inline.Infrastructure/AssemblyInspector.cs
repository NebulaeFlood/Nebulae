using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Globalization;
using System.Security.Cryptography;

namespace Tests.Runtime.Emit.Inline.Infrastructure;

public static class AssemblyInspector
{
    public static string ComputeSha256(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    public static bool ReferencesAssembly(string assemblyPath, string simpleName)
    {
        using AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(assemblyPath);
        return assembly.Modules.Any(
            module => module.AssemblyReferences.Any(
                reference => string.Equals(
                    reference.Name,
                    simpleName,
                    StringComparison.OrdinalIgnoreCase)));
    }

    public static IReadOnlyList<string> GetMethodInstructions(
        string assemblyPath,
        string typeFullName,
        string methodName)
    {
        using AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(assemblyPath);
        TypeDefinition type = assembly.MainModule.GetType(typeFullName)
            ?? throw new InvalidOperationException($"Cannot find type '{typeFullName}'.");
        MethodDefinition method = type.Methods.Single(candidate => candidate.Name == methodName);
        var instructions = method.Body.Instructions;
        var indexes = instructions
            .Select(static (instruction, index) => (instruction, index))
            .ToDictionary(static item => item.instruction, static item => item.index);

        return [.. instructions.Select(instruction => Normalize(instruction, indexes))];
    }

    private static string Normalize(
        Instruction instruction,
        Dictionary<Instruction, int> indexes)
    {
        string opCode = instruction.OpCode.Name;
        object? operand = instruction.Operand;

        return operand switch
        {
            null => opCode,
            Instruction target => $"{opCode} -> {indexes[target]}",
            Instruction[] targets =>
                $"{opCode} -> [{string.Join(", ", targets.Select(target => indexes[target]))}]",
            ParameterDefinition parameter => $"{opCode} arg:{parameter.Index}",
            VariableDefinition variable => $"{opCode} local:{variable.Index}",
            MethodReference method => $"{opCode} method:{method.FullName}",
            FieldReference field => $"{opCode} field:{field.FullName}",
            TypeReference type => $"{opCode} type:{type.FullName}",
            CallSite callSite => $"{opCode} signature:{callSite.FullName}",
            string value => $"{opCode} string:{value}",
            IFormattable value => $"{opCode} {value.ToString(null, CultureInfo.InvariantCulture)}",
            _ => $"{opCode} {operand}"
        };
    }
}
