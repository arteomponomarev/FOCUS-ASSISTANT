$ErrorActionPreference = 'Stop'
$manifest = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'model.json') | ConvertFrom-Json
$models = Join-Path $PSScriptRoot 'models'
New-Item -ItemType Directory -Force -Path $models | Out-Null
$target = Join-Path $models $manifest.name
$temporary = "$target.$([Guid]::NewGuid().ToString('N')).tmp"
try {
    Invoke-WebRequest -UseBasicParsing -Uri $manifest.url -OutFile $temporary
    if ((Get-Item -LiteralPath $temporary).Length -ne $manifest.size -or
        (Get-FileHash -LiteralPath $temporary -Algorithm SHA256).Hash -ne $manifest.sha256) {
        throw 'The YuNet model does not match the pinned OpenCV Zoo artifact.'
    }
    Invoke-WebRequest -UseBasicParsing -Uri $manifest.licenseUrl -OutFile (Join-Path $models 'YuNet-LICENSE.txt')
    Move-Item -LiteralPath $temporary -Destination $target -Force
    Write-Output 'Verified YuNet model installed.'
} finally {
    Remove-Item -LiteralPath $temporary -ErrorAction SilentlyContinue
}
