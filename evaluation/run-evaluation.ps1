$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location (Join-Path $root 'backend')
try {
    $env:RUN_EVALUATION = '1'
    dotnet test tests/GymBooking.Tests --filter "FullyQualifiedName~GymBooking.Tests.Evaluation" --logger "console;verbosity=normal"
}
finally {
    Remove-Item Env:RUN_EVALUATION -ErrorAction SilentlyContinue
    Pop-Location
}
