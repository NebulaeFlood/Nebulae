[CmdletBinding()]
param(
    # 用于对比的基准提交、分支或 ref
    # 在 push 事件中通常是 github.event.before
    # 在 pull_request 事件中通常是 PR base SHA
    [string] $BaseRef,

    # 用于对比的目标提交、分支或 ref
    # 默认使用 HEAD，也可以由 workflow 显式传入 PR head SHA 或 GITHUB_SHA
    [string] $HeadRef = 'HEAD',

    # 外部直接传入的变更文件列表
    [string[]] $ChangedFiles,

    # 强制返回所有 CI/CD group
    # 用于 workflow_dispatch、新分支 push、无法安全计算差异等场景
    [switch] $All,

    # 是否使用 JSON 格式输出
    [switch] $Json,

    # 可选的结果输出路径
    [string] $OutputPath
)

# 启用严格模式
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# 定位仓库根目录
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$discoveryScript = Join-Path $PSScriptRoot 'Get-Projects.ps1'

function ConvertTo-RepositoryPath {
    param([Parameter(Mandatory)][string] $Path)

    # 把 Windows 风格的 \ 统一转换成 /
    $normalized = $Path.Replace('\', '/')

    # 去掉开头的 ./，让路径保持相对路径格式
    while ($normalized.StartsWith('./', [StringComparison]::Ordinal)) {
        $normalized = $normalized.Substring(2)
    }

    return $normalized
}

function Write-Result {
    param([Parameter(Mandatory)][AllowEmptyCollection()][string[]] $Groups)

    # 根据 -Json 参数决定输出 JSON 数组，还是普通换行文本
    $serialized = if ($Json) {
        ConvertTo-Json -InputObject @($Groups) -Compress
    }
    else {
        @($Groups) -join [Environment]::NewLine
    }

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
    $serialized
}

# 查找所有 CI/CD
$discoveryJson = & $discoveryScript -Json

if ($LASTEXITCODE -ne 0) {
    throw 'CICD project discovery failed.'
}

$discovery = $discoveryJson | ConvertFrom-Json

# 提取所有 group 名称并保持顺序
$allGroups = @($discovery.groups | ForEach-Object cicd)

# 如果调用方要求跑全部 group，直接输出并退出
if ($All) {
    Write-Result $allGroups
    exit
}

# 计算本次变更的文件列表
# 每个元素会包含 status 和 path
$changes = [Collections.Generic.List[object]]::new()

if ($ChangedFiles) {
    # 如果外部直接传入 ChangedFiles，则不执行 git diff
    # 这种模式下，文件还存在视为修改，不存在视为删除。
    foreach ($changedFile in $ChangedFiles) {
        $relativePath = ConvertTo-RepositoryPath $changedFile
        $fullPath = Join-Path $repositoryRoot $relativePath
        $status = if (Test-Path -LiteralPath $fullPath) { 'M' } else { 'D' }

        $changes.Add([pscustomobject]@{
            status = $status
            path   = $relativePath
        })
    }
}
else {
    if (-not $BaseRef) {
        throw 'Specify -BaseRef, -ChangedFiles, or -All.'
    }

    # 使用 git diff 计算变更文件
    # --name-status 输出状态和路径
    # --find-renames 让 git 尝试识别 rename
    # core.quotepath=false 避免非 ASCII 路径被转义
    $diffOutput = @(& git -C $repositoryRoot -c core.quotepath=false diff --name-status --find-renames $BaseRef $HeadRef 2>&1)

    if ($LASTEXITCODE -ne 0) {
        # force push 可能导致事件里的 previous SHA 不可达
        # 即使 checkout 了完整历史，也可能无法安全 diff
        # 此时不能准确判断受影响 group，因此保守地跑全部 group
        Write-Warning "git diff failed for '$BaseRef..$HeadRef'; all CICD groups will run:`n$($diffOutput -join [Environment]::NewLine)"
        Write-Result $allGroups
        exit 0
    }

    foreach ($line in $diffOutput) {
        if ([string]::IsNullOrWhiteSpace($line)) {
            continue
        }

        # git diff --name-status 的格式通常为：
        # M<TAB>path
        # A<TAB>path
        # D<TAB>path
        # R100<TAB>oldPath<TAB>newPath
        $parts = $line -split "`t"
        $status = $parts[0]

        if ($status -match '^[RC]') {
            # rename 或 copy 会改变旧路径和新路径之间的输入关系。
            # 为了安全，把旧路径视为删除，新路径视为新增。
            # 后续遇到删除会直接跑全部 group。
            $changes.Add([pscustomobject]@{
                status = 'D'
                path   = ConvertTo-RepositoryPath $parts[1]
            })

            $changes.Add([pscustomobject]@{
                status = 'A'
                path   = ConvertTo-RepositoryPath $parts[2]
            })
        }
        else {
            # 普通新增、修改、删除。
            $changes.Add([pscustomobject]@{
                status = $status
                path   = ConvertTo-RepositoryPath $parts[1]
            })
        }
    }
}

# 如果没有任何变更，返回空数组
if ($changes.Count -eq 0) {
    Write-Result @()
    exit 0
}

# 只要存在删除，就跑全部 group
# 删除可能移除了某个项目 Import、ProjectReference 或共享文件，很难p判断是否只影响单个 group
if ($null -ne ($changes | Where-Object { $_.status -match '^D' } | Select-Object -First 1)) {
    Write-Result $allGroups
    exit 0
}

# 定义全局影响规则
# 这些文件或目录一旦变化，可能影响所有项目或 CI 自身，因此直接跑全部 group
$globalPatterns = @(
    '(^|/)Directory\.(Build|Packages)\..+$',
    '(^|/)global\.json$',
    '(^|/)NuGet\.Config$',
    '(^|/)\.editorconfig$',
    '\.(sln|slnx)$',
    '^\.github/workflows/',
    '^eng/workflows/Get-(Projects|DifferentGroups)\.ps1$'
)

foreach ($change in $changes) {
    if ($null -ne ($globalPatterns | Where-Object { $change.path -match $_ } | Select-Object -First 1)) {
        Write-Result $allGroups
        exit 0
    }
}

function Resolve-LocalInput {
    param(
        [Parameter(Mandatory)][string] $Value,
        [Parameter(Mandatory)][string] $SourceFile,
        [Parameter(Mandatory)][string] $ProjectDirectory
    )

    # 将 MSBuild 中的相对路径解析为仓库相对路径
    # SourceFile 是当前正在解析的项目文件或 imported 文件
    $sourceDirectory = Split-Path -Parent $SourceFile
    $expanded = $Value.Trim()

    # 支持常见 MSBuild 属性
    # 这里做的是有限展开，不做完整 MSBuild 求值
    $expanded = $expanded.Replace('$(MSBuildThisFileDirectory)', $sourceDirectory + [IO.Path]::DirectorySeparatorChar)
    $expanded = $expanded.Replace('$(MSBuildProjectDirectory)', $ProjectDirectory)

    # 如果仍然包含未展开的 MSBuild 表达式，则放弃解析
    # 这类输入无法安全映射到具体本地路径
    if ($expanded -match '(\$\(|@\()') {
        return $null
    }

    # 解析绝对路径或相对路径
    $fullPath = if ([IO.Path]::IsPathRooted($expanded)) {
        [IO.Path]::GetFullPath($expanded)
    }
    else {
        [IO.Path]::GetFullPath((Join-Path $sourceDirectory $expanded))
    }

    # 转成仓库相对路径，并统一使用 /
    $relativePath = [IO.Path]::GetRelativePath($repositoryRoot, $fullPath).Replace('\', '/')

    # 如果路径跳出仓库根目录，则忽略
    if ($relativePath -eq '..' -or $relativePath.StartsWith('../')) {
        return $null
    }

    return $relativePath
}

function Convert-GlobToRegex {
    param([Parameter(Mandatory)][string] $Pattern)

    # 将简单 glob 转换成 regex，用于匹配 Include 中的通配符路径：
    # - ** 表示跨目录匹配
    # - * 表示单个路径段内匹配
    # - ? 表示单个字符
    $regex = [regex]::Escape($Pattern)
    $regex = $regex.Replace('\*\*', '.*')
    $regex = $regex.Replace('\*', '[^/]*')
    $regex = $regex.Replace('\?', '[^/]')

    return '^' + $regex + '$'
}

# 为每个 CI/CD group 构建输入匹配 rule
# 每个 rule 包含：
# - group: group 名称
# - directories: 目录级输入
# - exactInputs: 精确文件输入
# - patterns: glob 转换来的 regex 输入
$rules = [Collections.Generic.List[object]]::new()

foreach ($group in $discovery.groups) {
    foreach ($projectPath in $group.projects) {
        # 当前项目的完整路径和目录
        $projectFullPath = Join-Path $repositoryRoot $projectPath
        $projectDirectory = Split-Path -Parent $projectFullPath
        $projectDirectoryRelative = [IO.Path]::GetRelativePath($repositoryRoot, $projectDirectory).Replace('\', '/').TrimEnd('/')

        # 当前 group 的输入集合
        $inputDirectories = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
        $exactInputs = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
        $inputPatterns = [Collections.Generic.List[string]]::new()

        # 防止递归解析 Import 或 ProjectReference 时重复访问同一文件
        $visitedFiles = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)

        # 待解析文件队列
        # 起点是当前项目文件
        $filesToInspect = [Collections.Generic.Queue[object]]::new()

        # 项目文件路径是精确输入
        $null = $exactInputs.Add((ConvertTo-RepositoryPath $projectPath))

        # 项目路径是目录级输入
        # 该目录下普通源文件变化时，应认为此 group 受影响
        $null = $inputDirectories.Add($projectDirectoryRelative + '/')

        # 将项目文件加入解析队列
        $filesToInspect.Enqueue([pscustomobject]@{
            path             = $projectFullPath
            projectDirectory = $projectDirectory
        })

        while ($filesToInspect.Count -gt 0) {
            # 取出一个项目文件或 imported 文件
            $inputFile = $filesToInspect.Dequeue()
            $sourceFile = $inputFile.path
            $currentProjectDirectory = $inputFile.projectDirectory
            $visitKey = "$sourceFile|$currentProjectDirectory"

            # 如果已经访问过，或文件不存在，则跳过
            if (-not $visitedFiles.Add($visitKey) -or -not (Test-Path -LiteralPath $sourceFile -PathType Leaf)) {
                continue
            }

            # 如果项目文件或 imported 文件不是合法 XML，直接失败
            try {
                [xml] $document = Get-Content -LiteralPath $sourceFile -Raw
            }
            catch {
                throw "MSBuild input '$sourceFile' is not valid XML: $($_.Exception.Message)"
            }

            # 解析所有 <Import Project="...">
            # 被 Import 的本地文件会被加入 exactInputs，并继续递归检查
            foreach ($importNode in @($document.SelectNodes("//*[local-name()='Import'][@Project]"))) {
                foreach ($importValue in ([string] $importNode.Project -split ';')) {
                    $resolvedImport = Resolve-LocalInput $importValue $sourceFile $currentProjectDirectory

                    # 不能解析或包含通配符的 Import 暂时跳过
                    if (-not $resolvedImport -or $resolvedImport -match '[*?]') {
                        continue
                    }

                    $null = $exactInputs.Add($resolvedImport)

                    $filesToInspect.Enqueue([pscustomobject]@{
                        path             = Join-Path $repositoryRoot $resolvedImport
                        projectDirectory = $currentProjectDirectory
                    })
                }
            }

            # 解析所有带 Include 属性的 XML 节点
            # 这包括 Compile、None、Content、ProjectReference 等
            foreach ($includeNode in @($document.SelectNodes('//*[@Include]'))) {
                foreach ($includeValue in ([string] $includeNode.Include -split ';')) {
                    $resolvedInclude = Resolve-LocalInput $includeValue $sourceFile $currentProjectDirectory

                    # 无法解析成本地路径的 Include 跳过
                    if (-not $resolvedInclude) {
                        continue
                    }

                    if ($resolvedInclude -match '[*?]') {
                        # Include 是 glob 时，转成 regex pattern
                        $inputPatterns.Add((Convert-GlobToRegex $resolvedInclude))
                    }
                    else {
                        # 普通 Include 作为精确输入
                        $null = $exactInputs.Add($resolvedInclude.TrimEnd('/'))

                        if ($includeNode.LocalName -eq 'ProjectReference') {
                            # ProjectReference 表示当前项目依赖另一个项目
                            # 被引用项目所在目录也应视为当前 group 的输入目录
                            $referencedProjectDirectory = Split-Path -Parent $resolvedInclude
                            $null = $inputDirectories.Add($referencedProjectDirectory.Replace('\', '/').TrimEnd('/') + '/')

                            # 被引用项目本身继续入队解析
                            # 这样可以沿着 ProjectReference 继续发现间接输入
                            $filesToInspect.Enqueue([pscustomobject]@{
                                path             = Join-Path $repositoryRoot $resolvedInclude
                                projectDirectory = Join-Path $repositoryRoot $referencedProjectDirectory
                            })
                        }
                    }
                }
            }
        }

        # 当前 project 的输入规则构建完成，加入 rules
        $rules.Add([pscustomobject]@{
            group       = [string] $group.cicd
            directories = $inputDirectories
            exactInputs = $exactInputs
            patterns    = $inputPatterns
        })
    }
}

# 把变更文件映射到受影响 group
$differences = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)

foreach ($change in $changes) {
    # group CI/CD hook
    # 例如 eng/workflows/foo-ci.ps1 或 eng/workflows/foo-cd.ps1
    $hookMatch = [regex]::Match($change.path, '^eng/workflows/(?<group>[a-z0-9-]+)-(ci|cd)\.ps1$')

    if ($hookMatch.Success) {
        $hookGroup = $hookMatch.Groups['group'].Value

        if ($hookGroup -in $allGroups) {
            $null = $differences.Add($hookGroup)
            continue
        }
    }

    # 标记当前变更是否能被某个 group 规则识别
    $matched = $false

    foreach ($rule in $rules) {
        # 先检查精确文件输入
        $isMatch = $rule.exactInputs.Contains($change.path)

        # 再检查目录级输入
        if (-not $isMatch) {
            foreach ($directory in $rule.directories) {
                if ($change.path.StartsWith($directory, [StringComparison]::OrdinalIgnoreCase)) {
                    $isMatch = $true
                    break
                }
            }
        }

        # 最后检查 glob 转换出的 regex pattern
        if (-not $isMatch) {
            foreach ($pattern in $rule.patterns) {
                if ($change.path -match $pattern) {
                    $isMatch = $true
                    break
                }
            }
        }

        # 如果命中当前 rule，则把对应 group 加入 differences 集合
        if ($isMatch) {
            $matched = $true
            $null = $differences.Add($rule.group)
        }
    }

    # 如果某个变更文件无法匹配任何已知输入规则，说明脚本无法证明它只影响某些 group
    # 为了避免漏跑 CI，保守地跑全部 group
    if (-not $matched) {
        Write-Result $allGroups
        exit 0
    }
}

# 按 allGroups 的顺序输出受影响 group
# HashSet 只负责去重，最终顺序仍由 discovery 结果决定
$orderedDifferentGroups = @($allGroups | Where-Object { $differences.Contains($_) })

Write-Result $orderedDifferentGroups
