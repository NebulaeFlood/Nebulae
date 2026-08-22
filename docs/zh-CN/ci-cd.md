# CI/CD 配置

## 项目发现

`Nebulae.slnx` 是仓库正式项目的唯一来源。项目发现脚本只读取解决方案中的 `.csproj` 条目。构建场景、测试夹具等辅助项目只有在加入解决方案后才会被视为 CI/CD 项目。

解决方案中的项目默认参与 CI/CD。可以使用保留值 `none` 将其排除：

```xml
<PropertyGroup>
  <CICD>none</CICD>
</PropertyGroup>
```

项目发现脚本会直接从项目 XML 中读取该值，并在 MSBuild 求值前跳过项目。只修改 `none` 项目目录中的文件不会调度任何分组；如果参与 CI/CD 的项目显式使用了该目录中的文件，使用方的输入规则仍会触发对应分组。

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

完成静态校验后，项目发现脚本会通过 MSBuild 计算 `CICD`、`IsPackable` 和 `IsTestProject`。`CICD` 的计算结果必须与项目文件中的直接声明一致，从而防止 Import、环境变量或仓库级构建配置隐式改变项目分组。

## 测试执行元数据

测试项目可以静态声明要运行的操作系统、配置和流水线阶段：

```xml
<PropertyGroup>
  <CICDOperatingSystems>linux;windows</CICDOperatingSystems>
  <CICDConfigurations>Debug;Release</CICDConfigurations>
  <CICDStage>source</CICDStage>
</PropertyGroup>
```

支持的操作系统为 `linux` 和 `windows`，工作流会将它们映射到实际 runner；支持的配置为 `Debug` 和 `Release`。`CICDStage` 必须是 `source` 或 `package`：

- `source` 测试会在声明的每个操作系统和配置上构建、测试当前源码。
- `package` 测试在打包后运行，并消费从工作流制品下载的确切候选包。

默认值分别是 `linux`、`Release` 和 `source`。这些属性只能用于测试项目。与 `CICD` 一样，显式声明必须没有条件、在项目文件中最多出现一次，并且 MSBuild 求值结果必须与静态声明完全一致。

包含 package 阶段测试的分组必须提供 `eng/workflows/hooks/<group>-package.ps1`。该适配脚本接收发现出的测试计划和候选包目录，负责将候选包交给本分组的消费者测试。CI 和 CD 都只在 Linux 上生成一份候选包，把所有包文件的名称、长度和 SHA-256 写入清单，再在 package 阶段声明的每个操作系统上验证同一份制品。只有所有 package 阶段任务通过后，CD 才会发布清单中列出的包。

## 运行流程

### CI

CI 由 `master` 分支的推送和拉取请求触发，也可以手动运行。同一拉取请求或分支出现新运行时，尚未完成的旧运行会被取消。

1. `CI Orchestrator` 比较提交范围并计算受影响的 CI/CD 分组；手动运行、无法确定前一提交的首次推送以及全局变更会运行全部分组。
2. 每个分组先执行 `discover`，验证项目元数据并生成 source 和 package 阶段的操作系统矩阵。
3. 如果分组包含 source 阶段测试，`source-test` 会在每个声明的操作系统上调用 `Invoke-SourceTests.ps1`，按声明的 Debug 和 Release 配置还原、构建并测试当前源码。
4. 如果分组包含 package 阶段测试，并且 source 阶段成功或被跳过，`pack` 会在 Linux 上以 Release 配置生成一次临时版本候选包和包清单，然后将它们上传为工作流制品。
5. `package-test` 在每个声明的操作系统上下载同一份候选制品，先校验分组、版本、文件名、长度和 SHA-256，再调用 `eng/workflows/hooks/<group>-package.ps1` 执行成品包消费测试。

CI 不发布包；没有对应测试阶段时，该阶段及其后续专属任务会被跳过。

### CD

CD 由发布标签触发，也可以使用已存在的发布标签手动运行。`v1.2.3` 选择 `default` 分组，`<group>-v1.2.3` 选择指定分组；标签中的版本同时作为 NuGet 包版本。

1. `CD Orchestrator` 解析并验证标签、分组、版本和发布源码引用；同一分组和版本的发布运行不会互相取消。
2. `discover` 从发布标签对应的源码生成测试计划，随后按与 CI 相同的矩阵执行可用的 source 阶段测试。
3. source 阶段成功或被跳过后，`pack` 在 Linux 上以 Release 配置构建所有项目，打包分组中的所有可打包项目，生成包清单并上传唯一的发布候选制品。
4. 如果分组包含 package 阶段测试，`package-test` 会在每个声明的操作系统上下载、校验并通过分组钩子消费该候选制品。
5. source 和 package 阶段均成功或被跳过后，`publish` 下载同一份候选制品并再次校验清单，然后通过 NuGet Trusted Publishing 只发布清单中标记为主包的 `.nupkg` 文件。

发布命令不忽略重复版本，因此 NuGet.org 已存在同版本时，CD 会明确失败。

## 添加项目

1. 将正式项目或测试项目加入 `Nebulae.slnx`。
2. 不声明 `CICD` 即可使用 `default` 分组；需要独立流水线时，声明一个自定义分组。
3. 仅当项目需要保留在解决方案中、但绝不能参与 CI/CD 时，才使用 `<CICD>none</CICD>`。
4. 仅当测试项目的执行契约不同于默认值时，才声明 `CICDOperatingSystems`、`CICDConfigurations` 或 `CICDStage`。
5. 由测试或构建工具调用的辅助 `.csproj` 不应加入解决方案。
6. 提交前运行下方的项目发现命令。

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
