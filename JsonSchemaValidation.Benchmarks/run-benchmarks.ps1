param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$BenchmarkArgs
)

$ErrorActionPreference = "Stop"

$projectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectFile = Join-Path $projectDir "JsonSchemaValidation.Benchmarks.csproj"
$outputDir = Join-Path $projectDir "bin\\Release\\net10.0"
$benchmarkExe = Join-Path $outputDir "JsonSchemaValidation.Benchmarks.exe"

$running = Get-Process -Name "JsonSchemaValidation.Benchmarks" -ErrorAction SilentlyContinue |
    Where-Object { $_.Path -eq $benchmarkExe }

if ($running)
{
    $ids = ($running | Select-Object -ExpandProperty Id) -join ", "
    throw "A benchmark process is already running from '$benchmarkExe' (PID: $ids). Stop it or wait for it to finish before starting another run."
}

dotnet build $projectFile -c Release -f net10.0 | Out-Host

if (-not (Test-Path $benchmarkExe))
{
    throw "Expected benchmark executable was not created: $benchmarkExe"
}

& $benchmarkExe @BenchmarkArgs
