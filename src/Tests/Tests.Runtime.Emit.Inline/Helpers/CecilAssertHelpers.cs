using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Tests.Runtime.Emit.Inline.Helpers;

internal static class CecilAssertHelpers
{
    private const string PlaceholderAssemblyName = "Nebulae.Runtime.Emit.Inline";

    public static void AssertNoPlaceholderCalls()
    {
        using AssemblyDefinition assembly = AssemblyHelpers.ReadCurrentTestAssembly();

        string[] calls = GetTypes(assembly.MainModule)
            .SelectMany(static type => type.Methods)
            .Where(static method => method.HasBody)
            .SelectMany(static method => method.Body.Instructions.Select(instruction => (method, instruction)))
            .Where(static item => item.instruction.Operand is MethodReference method && IsPlaceholderAssemblyReference(method.DeclaringType.Scope))
            .Select(static item => $"{item.method.FullName}: {item.instruction}")
            .ToArray();

        if (calls.Length != 0)
        {
            Assert.Fail($"Placeholder calls remain in the test assembly:{Environment.NewLine}{string.Join(Environment.NewLine, calls)}");
        }
    }

    public static void AssertNoPlaceholderAssemblyReference()
    {
        using AssemblyDefinition assembly = AssemblyHelpers.ReadCurrentTestAssembly();

        AssemblyNameReference? reference = assembly.MainModule.AssemblyReferences
            .FirstOrDefault(static reference => reference.Name == PlaceholderAssemblyName);

        if (reference is not null)
        {
            Assert.Fail($"The test assembly still references '{reference.FullName}'.");
        }
    }

    public static TypeDefinition GetTypeDefinition(string fullName)
    {
        AssemblyDefinition assembly = AssemblyHelpers.ReadCurrentTestAssembly();

        TypeDefinition? type = GetTypes(assembly.MainModule)
            .FirstOrDefault(type => type.FullName == fullName);

        return type ?? throw new AssertFailedException($"Type '{fullName}' was not found in the test assembly.");
    }

    public static MethodDefinition GetMethodDefinition(string typeFullName, string methodName)
    {
        TypeDefinition type = GetTypeDefinition(typeFullName);
        MethodDefinition[] methods = type.Methods
            .Where(method => method.Name == methodName)
            .ToArray();

        return methods.Length switch
        {
            1 => methods[0],
            0 => throw new AssertFailedException($"Method '{typeFullName}.{methodName}' was not found in the test assembly."),
            _ => throw new AssertFailedException($"Method name '{typeFullName}.{methodName}' is ambiguous in the test assembly."),
        };
    }

    public static IReadOnlyList<Instruction> GetInstructions(string typeFullName, string methodName)
    {
        MethodDefinition method = GetMethodDefinition(typeFullName, methodName);

        if (!method.HasBody)
        {
            throw new AssertFailedException($"Method '{typeFullName}.{methodName}' does not have a method body.");
        }

        return method.Body.Instructions.ToArray();
    }

    private static IEnumerable<TypeDefinition> GetTypes(ModuleDefinition module)
    {
        foreach (TypeDefinition type in module.Types)
        {
            yield return type;

            foreach (TypeDefinition nestedType in GetNestedTypes(type))
            {
                yield return nestedType;
            }
        }
    }

    private static IEnumerable<TypeDefinition> GetNestedTypes(TypeDefinition type)
    {
        foreach (TypeDefinition nestedType in type.NestedTypes)
        {
            yield return nestedType;

            foreach (TypeDefinition descendant in GetNestedTypes(nestedType))
            {
                yield return descendant;
            }
        }
    }

    private static bool IsPlaceholderAssemblyReference(IMetadataScope scope)
    {
        return scope is AssemblyNameReference reference && reference.Name == PlaceholderAssemblyName;
    }
}
