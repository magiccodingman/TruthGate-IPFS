$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$composeFile = Join-Path $repositoryRoot 'compose.yaml'
$devComposeFile = Join-Path $repositoryRoot 'compose.dev.yaml'
$outputFile = Join-Path $repositoryRoot 'compose.dev.generated.yaml'
$tempFile = Join-Path $repositoryRoot ('.compose.dev.generated.{0}.tmp' -f [Guid]::NewGuid().ToString('N'))

try {
    Push-Location $repositoryRoot

    & docker compose -f $composeFile -f $devComposeFile config | Set-Content -Path $tempFile -Encoding utf8

    if ($LASTEXITCODE -ne 0) {
        throw "docker compose config failed with exit code $LASTEXITCODE."
    }

    if (-not (Test-Path $tempFile) -or (Get-Item $tempFile).Length -eq 0) {
        throw 'Generated Compose file is empty.'
    }

    Move-Item -Path $tempFile -Destination $outputFile -Force
    Write-Host "Generated $outputFile"
}
finally {
    if (Test-Path $tempFile) {
        Remove-Item $tempFile -Force
    }

    Pop-Location
}
