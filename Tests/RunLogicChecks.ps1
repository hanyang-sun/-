param([string]$OutputDirectory = (Join-Path $PSScriptRoot 'obj/logic-checks'))

$ErrorActionPreference = 'Stop'
# Capture the native exit code after logging, including on PowerShell versions that opt into native errors.
$PSNativeCommandUseErrorActionPreference = $false
$repoRoot = Split-Path -Parent $PSScriptRoot
$logDirectory = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
$logPath = Join-Path $logDirectory 'logic-checks.log'
Set-Content -LiteralPath $logPath -Value '' -NoNewline -Encoding utf8
$checkExitCode = 1

Push-Location $repoRoot
try {
    & dotnet run --project Tests/Brick2Life.Checks.csproj --configuration Release 2>&1 |
        Tee-Object -FilePath $logPath
    $checkExitCode = $LASTEXITCODE
    if ($checkExitCode -eq 0 -and -not (Select-String -LiteralPath $logPath -Pattern '^BRICK2LIFE_CHECKS_PASSED$' -Quiet)) {
        'Logic checks exited without the completion marker.' | Tee-Object -FilePath $logPath -Append
        $checkExitCode = 1
    }
}
finally {
    Pop-Location
}

exit $checkExitCode
