[CmdletBinding()]
param(
    # 手动指定的分组
    [ValidatePattern('^(default|[a-z0-9-]+)$')]
    [string] $CICD,

    # 是否使用 JSON 格式输出
    [switch] $Json,

    # 可选的结果输出路径
    [string] $OutputPath
)

# 启用严格模式
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# 不参与 CI/CD 的项目
# 这些项目在仓库中只用于本地实验、临时验证或手动调试
$excludedProjectPaths = @(
    'src/Tests/Tests.Demo/Tests.Demo.csproj'
)

# 定位仓库根目录
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))

# 把绝对路径转换成相对路径
function Get-RelativePath {
    param(
        [Parameter(Mandatory)]
        [string] $Path
    )

    # 把 Windows 风格的 \ 统一转换成 /
    return [IO.Path]::GetRelativePath($repositoryRoot, $Path).Replace('\', '/')
}

# 使用 Git 获取已跟踪的 .csproj 文件
function Get-TrackedProjectFiles {
    $projectPaths = @(& git -C $repositoryRoot ls-files -- '*.csproj')
    $gitExitCode = $LASTEXITCODE

    if ($gitExitCode -ne 0) {
        throw "Failed to list tracked .csproj files with git ls-files. Exit code: $gitExitCode"
    }

    foreach ($relativePath in @($projectPaths | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object -Unique)) {
        
        # 把 Windows 风格的 \ 统一转换成 /
        $normalizedPath = $relativePath.Replace('\', '/')

        if ($normalizedPath -in $excludedProjectPaths) {
            continue
        }

        $fullPath = Join-Path $repositoryRoot $relativePath

        if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
            throw "Git lists project file '$relativePath', but it does not exist in the working tree."
        }

        Get-Item -LiteralPath $fullPath
    }
}

# 获取项目的 CICD、IsPackable、IsTestProject 属性
function Get-EvaluatedProjectProperties {
    param(
        [Parameter(Mandatory)]
        [string] $ProjectPath
    )

    # 临时移除 CICD 环境变量
    $propertyNames = @('CICD', 'IsPackable', 'IsTestProject')
    $environmentValues = @{}

    try {
        foreach ($propertyName in $propertyNames) {

            $environmentItem = Get-Item -LiteralPath "Env:$propertyName" -ErrorAction SilentlyContinue
            
            if ($null -ne $environmentItem) {
                $environmentValues[$propertyName] = $environmentItem.Value
                Remove-Item -LiteralPath "Env:$propertyName"
            }
        }

        $output = @(& dotnet msbuild $ProjectPath -nologo '-getProperty:CICD,IsPackable,IsTestProject' 2>&1)
        $exitCode = $LASTEXITCODE
    }
    finally {
        foreach ($propertyName in $propertyNames) {
            Remove-Item -LiteralPath "Env:$propertyName" -ErrorAction SilentlyContinue
            
            if ($environmentValues.ContainsKey($propertyName)) {
                Set-Item -LiteralPath "Env:$propertyName" -Value $environmentValues[$propertyName]
            }
        }
    }

    if ($exitCode -ne 0) {
         throw "MSBuild could not evaluate '$ProjectPath':`n$($output -join [Environment]::NewLine)"
    }

    try {
        return (($output -join [Environment]::NewLine) | ConvertFrom-Json -ErrorAction Stop).Properties
    }
    catch {
        throw "MSBuild returned invalid property JSON for '$ProjectPath':`n$($output -join [Environment]::NewLine)"
    }
}

# 收集所有已跟踪的项目文件
$projectFiles = @(Get-TrackedProjectFiles | Sort-Object FullName)

if ($projectFiles.Count -eq 0) {
    throw "No tracked .csproj files were found under '$repositoryRoot'."
}

# 解析项目并归类
$projects = foreach ($projectFile in $projectFiles) {
    try {
        [xml] $document = Get-Content -LiteralPath $projectFile.FullName -Raw
    }
    catch {
        throw "Project '$($projectFile.FullName)' is not valid XML: $($_.Exception.Message)"
    }

    # 读取项目中的静态 <CICD> 声明
    # 没有 <CICD> 时，项目属于隐式 default 分组
    $cicdNodes = @($document.SelectNodes("//*[local-name()='CICD']"))

    if ($cicdNodes.Count -gt 1) {
        $values = ($cicdNodes | ForEach-Object { $_.InnerText.Trim() }) -join ', '
        throw "Project '$(Get-RelativePath $projectFile.FullName)' has duplicate or conflicting CICD declarations: $values"
    }

    $groupName = 'default'

    if ($cicdNodes.Count -eq 1) {
        $cicdNode = $cicdNodes[0]
        $groupName = $cicdNode.InnerText.Trim()

        # 禁止条件化的 CICD 声明
        # CI/CD 分组必须是静态的，否则变更分析结果会变得不确定
        if ($null -ne $cicdNode.SelectSingleNode('ancestor-or-self::*[@Condition]')) {
            throw "Project '$(Get-RelativePath $projectFile.FullName)' uses a conditional CICD declaration. CICD must be static."
        }

        # 禁止显式声明 default
        # default 是保留分组，项目不写 <CICD> 才表示 default
        if ($groupName -eq 'default') {
            throw "Project '$(Get-RelativePath $projectFile.FullName)' explicitly declares the reserved default group. Remove the CICD property instead."
        }

        # 限制分组名称格式
        if ($groupName -cnotmatch '^[a-z0-9-]+$') {
            throw "Project '$(Get-RelativePath $projectFile.FullName)' has invalid CICD value '$groupName'. Use only lowercase letters, digits, and hyphens."
        }
    }

    # 校验 XML 声明和 MSBuild 求值结果是否一致
    # 防止 Directory.Build.props、Import 或环境变量改变 CICD 分组
    $properties = Get-EvaluatedProjectProperties $projectFile.FullName
    $declaredGroupName = if ($groupName -eq 'default') { '' } else { $groupName }

    if ($properties.CICD -cne $declaredGroupName) {
        throw "Project '$(Get-RelativePath $projectFile.FullName)' evaluates CICD as '$($properties.CICD)', but its project file declares '$declaredGroupName'. CICD must be declared once in the project file."
    }

    # 判断是否为测试项目
    # 支持三种来源：
    # - MSBuild 求值后的 IsTestProject
    # - 使用 MSTest.Sdk
    # - 项目 XML 中显式声明 <IsTestProject>true</IsTestProject>
    $rootSdk = [string] $document.Project.Sdk
    $isTestProject = $properties.IsTestProject -eq 'true' -or
        $rootSdk -match '(^|;)MSTest\.Sdk/' -or
        $null -ne $document.SelectSingleNode("//*[local-name()='IsTestProject' and translate(normalize-space(text()), 'TRUE', 'true')='true']")

    # 判断项目是否可打包
    # 只要 IsPackable 不是 false，就视为可打包
    $isPackable = $properties.IsPackable -ne 'false'

    # 输出规范化后的项目元数据
    [pscustomobject][ordered]@{
        path           = Get-RelativePath $projectFile.FullName
        cicd           = $groupName
        isTestProject  = $isTestProject
        isPackable     = $isPackable
    }
}

# 构建稳定的分组顺序
# default 始终排在最前面，其他显式分组按名称排序
$groupNames = @('default') + @($projects.cicd | Where-Object { $_ -ne 'default' } | Sort-Object -Unique)

# 如果指定了 -CICD，则验证分组是否存在
if ($CICD -and $CICD -notin $groupNames) {
    throw "Unknown CICD group '$CICD'. Available groups: $($groupNames -join ', ')"
}

# 如果指定了 -CICD，只输出该分组；否则输出全部分组
$selectedGroupNames = if ($CICD) { @($CICD) } else { $groupNames }

# 构建最终分组结果
# 每个分组包含：
# - projects：该分组下的所有项目
# - testProjects：该分组下的测试项目
# - packableProjects：该分组下可打包的项目
$groups = foreach ($groupName in $selectedGroupNames) {
    $groupProjects = @($projects | Where-Object cicd -eq $groupName)

    if ($groupProjects.Count -eq 0) {
        continue
    }

    [pscustomobject][ordered]@{
        cicd             = $groupName
        projects         = @($groupProjects.path)
        testProjects     = @($groupProjects | Where-Object isTestProject | ForEach-Object path)
        packableProjects = @($groupProjects | Where-Object isPackable | ForEach-Object path)
    }
}

# 序列化结果
# -Json 模式下使用压缩 JSON
$result = [pscustomobject][ordered]@{ groups = @($groups) }
$serialized = $result | ConvertTo-Json -Depth 5 -Compress:$Json

# 写入可选的输出路径
if ($OutputPath) {
    $resolvedOutputPath = if ([IO.Path]::IsPathRooted($OutputPath)) {
        $OutputPath
    }
    else {
        Join-Path (Get-Location) $OutputPath
    }

    $outputDirectory = Split-Path -Parent $resolvedOutputPath

    if ($outputDirectory) {
        $null = New-Item -ItemType Directory -Path $outputDirectory -Force
    }

    [IO.File]::WriteAllText($resolvedOutputPath, $serialized + [Environment]::NewLine)
}

# 写入控制台输出
# 普通模式输出可读的分组摘要
if ($Json) {
    $serialized
}
else {
    foreach ($group in $groups) {
        "[$($group.cicd)]"
        "  Projects: $($group.projects.Count)"
        $group.projects | ForEach-Object { "    $_" }
        "  Test projects: $($group.testProjects.Count)"
        $group.testProjects | ForEach-Object { "    $_" }
        "  Packable projects: $($group.packableProjects.Count)"
        $group.packableProjects | ForEach-Object { "    $_" }
    }
}