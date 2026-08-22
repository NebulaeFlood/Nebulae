# Nebulae.Runtime.Emit.Inline

`Nebulae.Runtime.Emit.Inline` is a simple compile-time IL inlining library that allows C# methods to describe their bodies through placeholder APIs and replaces those calls with real IL after compilation.

The project uses [Mono.Cecil](https://github.com/jbevain/cecil) to perform IL inlining and includes a custom Roslyn analyzer to help ensure that the placeholder APIs are used correctly.

## Installation

```xml
<PackageReference Include="Nebulae.Runtime.Emit.Inline"
                  Version="2.0.0"
                  PrivateAssets="all" />
```

The project includes a built-in check for `PrivateAssets="all"`. A compile-time error is reported if this property is not set.

## Quick start

```csharp
using Nebulae.Runtime.Emit.Inline;

static int Add(int left, int right)
{
    IL.Emit.Ldarg(left);
    IL.Emit.Ldarg(right);
    IL.Emit.Add();
    return IL.Ret<int>();
}
```

Each `IL.Emit` call corresponds to an IL opcode.
`IL.Ret<T>()` is a convenience helper provided by the project and corresponds to the `ret` opcode.

The following is the equivalent implementation without this helper:

```csharp
using Nebulae.Runtime.Emit.Inline;

static int Add(int left, int right)
{
    IL.Emit.Ldarg(left);
    IL.Emit.Ldarg(right);
    IL.Emit.Add();
    IL.Emit.Ret();

    throw IL.Fail();
}
```

The project does not perform complete IL verification while replacing instructions.
The caller is responsible for ensuring that the evaluation stack, control flow, operand types, and instruction order are correct.

## Included components

- `Nebulae.Runtime.Emit.Inline` — compile-time placeholder API.
- `Nebulae.Runtime.Emit.Inline.Analyzers` — Roslyn analyzer.
- `Nebulae.Runtime.Emit.Inline.MSBuild` — Mono.Cecil-based assembly rewriter.

After a successful rewrite, none of these package components becomes a runtime dependency of the application.

## Build process

1. The analyzer checks placeholder API usage in the C# source code.
2. The C# compiler emits ordinary calls to the placeholder APIs.
3. After the `CoreCompile` target, the `InlineIL` MSBuild task replaces those calls in the intermediate assembly.
4. The task removes the reference to the `Nebulae.Runtime.Emit.Inline` placeholder assembly, then saves and validates the result.

If the assembly no longer references `Nebulae.Runtime.Emit.Inline`, the task skips IL inlining.

## Instructions and extensions

### Native instructions

The `IL.Emit` class contains placeholder methods for ordinary instructions, branches, calls, object construction, variable access, prefixes, and other supported opcodes.
Operands that describe metadata or instruction structure must be compile-time constants where required by the analyzer.

### Extended instructions

The `IL` class also contains instructions that make authoring easier, including:

- `IL.Fail`, which satisfies C# control-flow analysis.
- `IL.Label`, which declares a label similar to `Label` in `ILGenerator`; its scope is the current method.
- `IL.Pop`, which pops the value at the top of the current method's evaluation stack.
- `IL.Push`, which pushes the specified value onto the current method's evaluation stack.
- `IL.Ref`, which declares a metadata reference.
- `IL.Ret`, which handles cases where `IL.Emit.Ret` cannot satisfy C# control-flow analysis.

## Referencing types or members

When an instruction needs to reference a type, pass the corresponding type directly with `typeof`:

```csharp
IL.Emit.Box(typeof(int));
```

When an instruction needs to reference a member of a type, declare the reference with `IL.Ref`:

```csharp
IL.Emit.Newobj(
    IL.Ref(typeof(string))
        .Constructor(typeof(char), typeof(int)));
IL.Emit.Ldsfld(
    IL.Ref(typeof(string))
        .Field(nameof(string.Empty)));
IL.Emit.Call(
    IL.Ref(typeof(string))
        .Method(nameof(string.StartsWith), typeof(string)));
IL.Emit.Callvirt(
    IL.Ref(typeof(string))
        .Property(nameof(string.Length))
        .Get);
IL.Emit.Callvirt(
    IL.Ref(typeof(string))
        .Indexer(typeof(int))
        .Get);
IL.Emit.Callvirt(
    IL.Ref(typeof(AppDomain))
        .Event(nameof(AppDomain.AssemblyLoad))
        .Add);
```

APIs that match parameter types require every corresponding type to be specified. Use `typeof(GenericRef)` to match a generic parameter declaration.

```csharp
IL.Emit.Call(
    IL.Ref(typeof(Enumerable))
        .Method(nameof(Enumerable.Repeat), 1, typeof(GenericRef), typeof(int))
        .MakeGeneric(typeof(string)));
```

### Member matching

Unlike the default reflection lookup behavior, member lookup through `IL.Ref` is limited to members declared by the type specified by `IL.Ref`.
When a member belongs to a base type or interface, select that base type or interface explicitly.

### Method matching

```csharp
// A non-generic method.
IL.Emit.Call(
    IL.Ref(typeof(int))
        .Method(nameof(int.Parse), typeof(string)));

// A generic method definition with one generic parameter.
IL.Emit.Call(
    IL.Ref(typeof(Enumerable))
        .Method(nameof(Enumerable.Empty), 1)
        .MakeGeneric(typeof(string)));
```

- The overload without `genericParameterCount` selects only non-generic methods.
- If no match is found, a `MissingMethodException` is thrown.

## Parameters and local variables

Instructions that reference parameters, such as `Ldarg`, can accept:

- `this`
- `value`
- Parameters declared by the method.
- `in`, `out`, and `ref` method parameters.

Instructions that reference local variables, such as `Ldloc`, can accept:

- Local variables declared by the method.
- `in`, `out`, and `ref` local variables.

```csharp
static int Func(int left, int right, out int value)
{
    IL.Emit.Ldarg(left);
    IL.Emit.Ldarg(right);
    IL.Emit.Add();
    IL.Emit.Dup();
    IL.Emit.Stloc(out value);
    IL.Emit.Stloc(out int result);
    IL.Emit.Ldloc(result);
    return IL.Ret<int>();
}
```

In some cases, the IL inliner may be unable to find a parameter or local variable. For example:

- Compiler optimizations remove a local variable.
- Code such as `async` methods or `yield return` generates a state machine that hoists a parameter or local variable into a state-machine field.
