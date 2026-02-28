$outputDir = ".\bin\publish\win-x64"
$projectPath = ".\Hoi4BlueprintBuilder.Windows.csproj"

function Show-Menu {
    param ([string[]]$Options, [string]$Title)
    $selectedIndex = 0
    try {
        $cursorVisible = [Console]::CursorVisible
        [Console]::CursorVisible = $false
        
        # 预留空间防止控制台滚动导致坐标错乱
        Write-Host $Title
        foreach ($opt in $Options) { Write-Host "" }
        $startPos = [Console]::CursorTop - $Options.Length - 1
    }
    catch {
        # 兼容不支持控制台光标操作的环境
        $choice = Read-Host "$Title (1-$($Options.Length))"
        return $Options[[int]$choice - 1]
    }

    while ($true) {
        [Console]::SetCursorPosition(0, $startPos)
        Write-Host $Title -ForegroundColor Cyan
        for ($i = 0; $i -lt $Options.Length; $i++) {
            if ($i -eq $selectedIndex) {
                Write-Host "> $($Options[$i])  " -ForegroundColor Green
            }
            else {
                Write-Host "  $($Options[$i])  " -ForegroundColor Gray
            }
        }

        $key = [Console]::ReadKey($true).Key
        if ($key -eq 'UpArrow') {
            $selectedIndex = ($selectedIndex - 1 + $Options.Length) % $Options.Length
        }
        elseif ($key -eq 'DownArrow') {
            $selectedIndex = ($selectedIndex + 1) % $Options.Length
        }
        elseif ($key -eq 'Enter') {
            break
        }
    }

    [Console]::CursorVisible = $cursorVisible
    return $Options[$selectedIndex]
}
# --channel 参数的值格式为 "平台-通道"，例如 "win-x64-stable" 或 "win-x64-beta"
# 需要更改时需要同步修改程序中的相关代码
$channel = Show-Menu -Title "请选择打包通道 (使用上下方向键选择，回车确认):" -Options @("stable", "beta", "all")
$platform = "win-x64"

if ($channel -eq "all") {
    $channelsToProcess = @("stable", "beta")
    $channelDisplay = "所有渠道 (stable, beta)"
}
else {
    $channelsToProcess = @($channel)
    $channelDisplay = "$platform-$channel"
}

Write-Host "开始构建并打包项目..." -ForegroundColor Cyan
Write-Host "打包通道: " -ForegroundColor Cyan -NoNewline
Write-Host $channelDisplay -ForegroundColor Magenta

# 从 .csproj 提取版本号
# 查找 <Version>...</Version> 标签
$versionMatch = Select-String -Path $projectPath -Pattern "<Version>(.*?)</Version>"
if ($versionMatch) {
    $version = $versionMatch.Matches.Groups[1].Value
    Write-Host "正在构建版本: $version" -ForegroundColor Cyan
}
else {
    Write-Error "Could not find <Version> tag in $projectPath. Please add it manually."
    exit 1
}
# ------------------------------------

dotnet publish $projectPath -r win-x64 -o $outputDir
if ($LASTEXITCODE -ne 0) {
    Write-Error "构建失败";
    exit $LASTEXITCODE
}
else { Write-Host "开始打包" -ForegroundColor Cyan }

foreach ($c in $channelsToProcess) {
    $fullChannel = "$platform-$c"
    Write-Host ">>> 正在生成打包文件: $fullChannel" -ForegroundColor Cyan
    vpk pack --delta BestSize --channel $fullChannel --packId ParadoxArt -v $version --packDir $outputDir --mainExe ParadoxArt.exe --icon ..\Hoi4BlueprintBuilder.Core\Assets\logo.ico --releaseNotes .\releasenotes.md
}