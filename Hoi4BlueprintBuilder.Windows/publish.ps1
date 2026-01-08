& .\full-build.ps1

if ($LASTEXITCODE -ne 0)
{
    Write-Error "Build failed";
    exit $LASTEXITCODE
}

Write-Host "程序构建并打包完成." -ForegroundColor Green

$ossKey = $env:HOI4_OSS_KEY
$ossSecret = $env:HOI4_OSS_SECRET

if ([string]::IsNullOrWhiteSpace($ossSecret) -or [string]::IsNullOrWhiteSpace($ossKey)) {
    Write-Error "环境变量 HOI4_OSS_SECRET 未设置，无法上传！"
    exit 1
}

Write-Host "开始上传到 OSS" -ForegroundColor Cyan
vpk upload s3 --bucket app-update-packages --endpoint https://b7a7a340981df6606c000f71c361c854.r2.cloudflarestorage.com --keyId $ossKey --secret $ossSecret
Write-Host "上传完成." -ForegroundColor Green