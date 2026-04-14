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
$channel = Show-Menu -Title "请选择打包通道 (使用上下方向键选择，回车确认):" -Options @("all", "stable", "beta")
$platform = Show-Menu -Title "请选择平台 (使用上下方向键选择，回车确认):" -Options @("all", "win-x64", "linux-x64")

if ($platform -eq "all") {
    $platformsToProcess = @("win-x64", "linux-x64")
    $platformDisplay = "所有平台"
} else {
    $platformsToProcess = @($platform)
    $platformDisplay = $platform
}

if ($channel -eq "all") {
    $channelsToProcess = @("stable", "beta")
    $channelDisplay = "所有渠道 (stable, beta)"
}
else {
    $channelsToProcess = @($channel)
    $channelDisplay = $channel
}

Write-Host "开始构建并打包项目..." -ForegroundColor Cyan
Write-Host "打包平台: " -ForegroundColor Cyan -NoNewline
Write-Host $platformDisplay -ForegroundColor Magenta
Write-Host "打包通道: " -ForegroundColor Cyan -NoNewline
Write-Host $channelDisplay -ForegroundColor Magenta

foreach ($currentPlatform in $platformsToProcess) {
    Write-Host "========== 开始处理平台: $currentPlatform ==========" -ForegroundColor Yellow
    
    if ($currentPlatform -eq "win-x64") {
        # 如果脚本在根目录运行或 Windows 目录运行
        if (Test-Path ".\Hoi4BlueprintBuilder.Windows.csproj") {
            $projectPath = ".\Hoi4BlueprintBuilder.Windows.csproj"
            $outputDir = ".\bin\publish\win-x64"
            $iconPath = "..\Hoi4BlueprintBuilder.Core\Assets\logo.ico"
        } else {
            $projectPath = ".\Hoi4BlueprintBuilder.Windows\Hoi4BlueprintBuilder.Windows.csproj"
            $outputDir = ".\Hoi4BlueprintBuilder.Windows\bin\publish\win-x64"
            $iconPath = ".\Hoi4BlueprintBuilder.Core\Assets\logo.ico"
        }
        $mainExe = "ParadoxArt.exe"
        $vpkPlatform = "[win]"
    } elseif ($currentPlatform -eq "linux-x64") {
        if (Test-Path "..\Hoi4BlueprintBuilder.Linux\Hoi4BlueprintBuilder.Linux.csproj") {
            $projectPath = "..\Hoi4BlueprintBuilder.Linux\Hoi4BlueprintBuilder.Linux.csproj"
            $outputDir = "..\Hoi4BlueprintBuilder.Linux\bin\publish\linux-x64"
            $iconPath = "..\Hoi4BlueprintBuilder.Core\Assets\logo.ico"
        } else {
            $projectPath = ".\Hoi4BlueprintBuilder.Linux\Hoi4BlueprintBuilder.Linux.csproj"
            $outputDir = ".\Hoi4BlueprintBuilder.Linux\bin\publish\linux-x64"
            $iconPath = ".\Hoi4BlueprintBuilder.Core\Assets\logo.ico"
        }
        $mainExe = "ParadoxArt"
        $vpkPlatform = "[linux]"
    } else {
        exit 1
    }

    # 从 .csproj 提取版本号
    $versionMatch = Select-String -Path ".\Hoi4BlueprintBuilder.Core\Hoi4BlueprintBuilder.Core.csproj" -Pattern "<Version>(.*?)</Version>"
    if ($versionMatch) {
        $version = $versionMatch.Matches.Groups[1].Value
        Write-Host "正在构建版本: $version ($currentPlatform)" -ForegroundColor Cyan
    }
    else {
        Write-Error "Could not find <Version> tag in $projectPath."
        exit 1
    }

    dotnet publish $projectPath -r $currentPlatform -o $outputDir
    if ($LASTEXITCODE -ne 0) {
        Write-Error "构建 $currentPlatform 失败";
        exit $LASTEXITCODE
    }
    else { Write-Host "开始打包 $currentPlatform" -ForegroundColor Cyan }

    foreach ($c in $channelsToProcess) {
        $fullChannel = "$currentPlatform-$c"
        Write-Host ">>> 正在生成打包文件: $fullChannel" -ForegroundColor Cyan
        vpk $vpkPlatform pack --delta BestSize --channel $fullChannel --packId ParadoxArt -v $version --packDir $outputDir --mainExe $mainExe --icon $iconPath --releaseNotes ".\releasenotes.md"
    }
}