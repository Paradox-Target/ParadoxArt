$outputDir = ".\bin\publish\win-x64"
$projectPath = ".\Hoi4BlueprintBuilder.Windows.csproj"

Write-Host "开始构建并打包项目..." -ForegroundColor Cyan

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

vpk pack --delta BestSize --packId Hoi4BlueprintBuilder -v $version --packDir $outputDir --mainExe Hoi4BlueprintBuilder.Windows.exe --icon ..\Hoi4BlueprintBuilder.Core\Assets\logo.ico --releaseNotes .\releasenotes.md