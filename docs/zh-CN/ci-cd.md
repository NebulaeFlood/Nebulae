# CI/CD 配置

[English](../en-US/ci-cd.md)

## 项目发现

`Nebulae.slnx` 是仓库正式项目的唯一来源。项目发现脚本只读取解决方案中的 `.csproj` 条目。构建场景、测试夹具等辅助项目只有在加入解决方案后才会被视为 CI/CD 项目。

解决方案中的项目默认参与 CI/CD。仅供本地使用、但仍需要保留在解决方案中的项目，可以使用保留值 `none` 退出：

```xml
<PropertyGroup>
  <CICD>none</CICD>
</PropertyGroup>
```

项目发现脚本会直接从项目 XML 中读取该值，并在 MSBuild 求值前跳过项目。`none` 不是可运行的 CI/CD 分组。只修改 `none` 项目目录中的文件不会调度任何分组；如果参与 CI/CD 的项目显式使用了该目录中的文件，使用方的输入规则仍会触发对应分组。

## CI/CD 分组

没有声明 `CICD` 属性的项目属于隐式的 `default` 分组：

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
</PropertyGroup>
```

需要独立分组时，在项目中静态声明 `CICD` 属性：

```xml
<PropertyGroup>
  <CICD>inline-il-package</CICD>
</PropertyGroup>
```

分组名称只能包含 ASCII 小写字母、数字和连字符。`default` 与 `none` 是保留值。不要为 `CICD` 属性添加 `Condition`，同一个项目文件中也不能重复声明该属性。

完成静态校验后，项目发现脚本会通过 MSBuild 求值 `CICD`、`IsPackable` 和 `IsTestProject`。求值后的 `CICD` 必须与项目文件中的直接声明一致，从而防止 Import、环境变量或仓库级构建配置隐式改变项目分组。

## 添加项目

1. 将正式项目或测试项目加入 `Nebulae.slnx`。
2. 不声明 `CICD` 即可使用 `default` 分组；需要独立流水线时，声明一个自定义分组。
3. 仅当项目需要保留在解决方案中、但绝不能参与 CI/CD 时，才使用 `<CICD>none</CICD>`。
4. 由测试或构建工具调用的辅助 `.csproj` 不应加入解决方案。
5. 提交前运行下方的项目发现命令。

修改 `Nebulae.slnx`、工作流定义或项目发现脚本会被视为全局变更，并触发所有已发现分组。

## 本地验证

显示所有已发现的项目和分组：

```powershell
./eng/workflows/Get-Projects.ps1
```

生成机器可读的输出：

```powershell
./eng/workflows/Get-Projects.ps1 -Json
```

查看某个自定义分组：

```powershell
./eng/workflows/Get-Projects.ps1 -CICD inline-il-package
```

列出编排器可以运行的全部分组：

```powershell
./eng/workflows/Get-DifferentGroups.ps1 -All -Json
```
