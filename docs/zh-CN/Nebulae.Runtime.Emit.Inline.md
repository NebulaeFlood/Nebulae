# Nebulae.Runtime.Emit.Inline

`Nebulae.Runtime.Emit.Inline` 是一个简单的编译时 IL 内联库，
允许 C# 方法通过占位 API 描述方法体，并在编译后将这些调用替换为真实 IL。

该项目基于 [Mono.Cecil](https://github.com/jbevain/cecil) 完成 IL 内联，
并内置了自定义的 Roslyn 分析器，以帮助规范地使用占位 API。

## 安装

```xml
<PackageReference Include="Nebulae.Runtime.Emit.Inline"
                  Version="2.0.0"
                  PrivateAssets="all" />
```

项目内置了 `PrivateAssets="all"` 检查，若未设置此属性，将抛出编译时错误。

## 快速开始

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

每个 `IL.Emit` 调用对应一条 IL 操作码。
`IL.Ret<T>()` 是项目为了使用方便添加的拓展，对应 `ret` 操作码。

以下是不使用此拓展的等效实现：

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

项目在命令替换过程中不会执行完整的 IL 验证。
求值栈、控制流、操作数类型和指令顺序由调用方保证正确。

## 包含的组件

- `Nebulae.Runtime.Emit.Inline`——编译期占位 API。
- `Nebulae.Runtime.Emit.Inline.Analyzers`——Roslyn 语法分析器。
- `Nebulae.Runtime.Emit.Inline.MSBuild`——基于 Mono.Cecil 的程序集重写器。

成功重写后，这些包内组件都不会成为应用的运行时依赖。

## 构建过程

1. 分析器在 C# 源码中检查占位 API 的用法。
2. C# 编译器生成对占位 API 的普通调用。
3. `CoreCompile` 任务之后，MSBuild 任务 `InlineIL` 在中间程序集替换这些调用。
4. 移除占位程序集 `Nebulae.Runtime.Emit.Inline` 的引用，保存并验证。

如果程序集已不再引用 `Nebulae.Runtime.Emit.Inline`，任务会跳过内联处理。

## 指令与扩展

### 原生指令

`IL.Emit` 类包含了常规指令、分支、调用、对象构造、变量访问、前缀和其他受支持的操作码的占位方法。
描述元数据或指令结构的操作数必须在分析器要求的位置使用编译期常量。

### 扩展指令

`IL` 类还包含了辅助完成编写的指令，包括：

- `IL.Fail`，用于满足 C# 语法分析。
- `IL.Label`，用于声明类似 `ILGenerator` 中的 `Label`，其作用域是当前方法。
- `IL.Pop`，用于弹出当前方法运算栈顶的值。
- `IL.Push`，用于将指定值压入当前方法的运算栈顶。
- `IL.Ref`，用于声明元数据引用。
- `IL.Ret`，用于解决 `IL.Emit.Ret` 在部分场景无法满足 C# 语法分析的问题。

## 引用类型或成员

对于需要引用类型的情况，直接使用 `typeof` 传入对应类型：

```csharp
IL.Emit.Box(typeof(int));
```

对于需要引用类型中成员的情况，使用 `IL.Ref` 声明引用：

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

对于需要匹配参数类型的 API，必须给出完整的对应类型，使用 `typeof(GenericRef)` 以匹配泛型参数声明。

```csharp
IL.Emit.Call(
    IL.Ref(typeof(Enumerable))
        .Method(nameof(Enumerable.Repeat), 1, typeof(GenericRef), typeof(int))
        .MakeGeneric(typeof(string)));
```

### 成员匹配

与默认反射参数不同，使用 `IL.Ref` 引用类型中成员时，查找范围仅限 `IL.Ref` 指明的类型，
需要基类或接口成员时，应显式选择对应的基类或接口。

### 方法匹配

```csharp
// 非泛型方法。
IL.Emit.Call(
    IL.Ref(typeof(int))
        .Method(nameof(int.Parse), typeof(string)));

// 具有一个泛型参数的泛型方法定义。
IL.Emit.Call(
    IL.Ref(typeof(Enumerable))
        .Method(nameof(Enumerable.Empty), 1)
        .MakeGeneric(typeof(string)));
```

- 不带 `genericParameterCount` 的重载只选择非泛型方法。
- 没有匹配项时，将抛出 `MissingMethodException`。

## 参数与局部变量

对于 `Ldarg` 等引用参数的指令，可以传入：

- `this`
- `value`
- 方法定义的参数。
- `in`、`out`、`ref` 的方法参数。

对于 `Ldloc` 等引用局部变量的指令，可以传入：

- 方法定义的局部变量。
- `in`、`out`、`ref` 的局部变量。

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

注意，部分情况可能导致 IL 内联时找不到参数或局部变量，例如：

- 编译器优化导致局部变量被移除。
- `async`、`yield return` 等代码自动生成状态机时，参数或局部被提升为状态机字段。
