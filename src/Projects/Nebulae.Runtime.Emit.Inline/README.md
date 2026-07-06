# Nebulae.Runtime.Emit.Inline

`Nebulae.Runtime.Emit.Inline` is a simple compile‑time IL inlining library built on [Mono.Cecil](https://github.com/jbevain/cecil).

You can describe IL instructions in C# methods using placeholder APIs.
After the project is compiled, the included MSBuild task replaces these calls with actual IL
and removes the dependency on the placeholder assembly from the generated output.

The package also provides a simple Roslyn analyzer that prevents unsafe rewrites or usage patterns that do not meet the constraints.

## Installation

This package should only be used as a compile‑time dependency, so you must set `PrivateAssets="all"`:

```xml
<PackageReference Include="Nebulae.Runtime.Emit.Inline"
                  Version="1.0.0"
                  PrivateAssets="all" />
```

If `PrivateAssets="all"` is not set, a build‑time check in the package will report an error directly.

## Example

```csharp
using Nebulae.Runtime.Emit.Inline;

static int Return42()
{
    IL.Emit.Ldc_I4(42);
    IL.Emit.Ret();
    throw IL.Fail();
}
```

After compilation, the placeholder calls in the method are replaced with the corresponding IL instructions.
`IL.Fail()` is only used to satisfy the C# compiler’s control‑flow analysis; it is not retained in the final method.

## Included Components

- `Nebulae.Runtime.Emit.Inline` – the placeholder API referenced by user code.
- `Nebulae.Runtime.Emit.Inline.Analyzers` – a Roslyn analyzer that checks for common invalid usage.
- `Nebulae.Runtime.Emit.Inline.MSBuild` – the MSBuild task that performs the IL rewriting using Mono.Cecil.

Both the analyzer and the MSBuild task are compile‑time components and do not become runtime dependencies of your application.