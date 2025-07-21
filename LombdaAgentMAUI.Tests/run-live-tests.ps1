# PowerShell script to run LombdaAgent MAUI live integration tests
param(
    [string]$ApiUrl = "http://localhost:5000/",
    [string]$TestFilter = "Category=Network",
    [switch]$SkipSlowTests = $false,
    [switch]$ShowHelp = $false
)

if ($ShowHelp) {
    Write-Host "LombdaAgent MAUI Live Integration Test Runner" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Usage:" -ForegroundColor Yellow
    Write-Host "  .\run-live-tests.ps1 [-ApiUrl <url>] [-TestFilter <filter>] [-SkipSlowTests] [-ShowHelp]"
    Write-Host ""
    Write-Host "Parameters:" -ForegroundColor Yellow
    Write-Host "  -ApiUrl        API URL to test against (default: https://localhost:5001/)"
    Write-Host "  -TestFilter    NUnit test filter (default: Category=Network)"
    Write-Host "  -SkipSlowTests Skip slow tests that involve AI processing"
    Write-Host "  -ShowHelp      Show this help message"
    Write-Host ""
    Write-Host "Examples:" -ForegroundColor Yellow
    Write-Host "  .\run-live-tests.ps1"
    Write-Host "  .\run-live-tests.ps1 -ApiUrl http://localhost:5000/"
    Write-Host "  .\run-live-tests.ps1 -SkipSlowTests"
    Write-Host "  .\run-live-tests.ps1 -TestFilter 'TestName=LiveServer_CreateAgent_ReturnsValidAgent'"
    Write-Host ""
    exit 0
}

Write-Host "🚀 LombdaAgent MAUI Live Integration Test Runner" -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host ""

# Set environment variables
$env:LOMBDA_TEST_API_URL = $ApiUrl
$env:LOMBDA_RUN_LIVE_TESTS = "true"

# Adjust test filter for slow tests
if ($SkipSlowTests) {
    if ($TestFilter -eq "Category=Network") {
        $TestFilter = "Category=Network&Category!=Slow"
    } else {
        $TestFilter = "$TestFilter&Category!=Slow"
    }
}

Write-Host "Configuration:" -ForegroundColor Yellow
Write-Host "  API URL: $ApiUrl"
Write-Host "  Test Filter: $TestFilter"
Write-Host "  Skip Slow Tests: $SkipSlowTests"
Write-Host "  OpenAI Key Set: $($env:OPENAI_API_KEY -ne $null -and $env:OPENAI_API_KEY -ne '')"
Write-Host ""

# Check if API is accessible
Write-Host "🔍 Checking API accessibility..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$($ApiUrl.TrimEnd('/') + '/v1/agents')" -Method GET -TimeoutSec 10 -ErrorAction Stop
    if ($response.StatusCode -eq 200) {
        Write-Host "✅ API is accessible!" -ForegroundColor Green
    } else {
        Write-Host "⚠️ API returned status code: $($response.StatusCode)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "❌ Cannot access API at $ApiUrl" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please ensure:" -ForegroundColor Yellow
    Write-Host "  1. The LombdaAgentAPI is running"
    Write-Host "  2. The URL is correct"
    Write-Host "  3. Your OpenAI API key is set: `$env:OPENAI_API_KEY = 'your-key'"
    Write-Host ""
    Write-Host "To start the API, run:" -ForegroundColor Cyan
    Write-Host "  cd LombdaAgentAPI"
    Write-Host "  dotnet run"
    Write-Host ""
    exit 1
}

Write-Host ""

# Navigate to test directory
$testDir = Join-Path $PSScriptRoot ".."
if (-not (Test-Path $testDir)) {
    Write-Host "❌ Test directory not found: $testDir" -ForegroundColor Red
    exit 1
}

Set-Location $testDir

# Run the tests
Write-Host "🧪 Running tests..." -ForegroundColor Yellow
Write-Host "Command: dotnet test --filter `"$TestFilter`" --verbosity normal" -ForegroundColor Gray
Write-Host ""

try {
    $testResult = dotnet test --filter $TestFilter --verbosity normal
    $exitCode = $LASTEXITCODE
    
    Write-Host ""
    if ($exitCode -eq 0) {
        Write-Host "✅ All tests passed!" -ForegroundColor Green
    } else {
        Write-Host "❌ Some tests failed. Exit code: $exitCode" -ForegroundColor Red
    }
    
    Write-Host ""
    Write-Host "🔧 Quick Commands:" -ForegroundColor Cyan
    Write-Host "  Run single test: dotnet test --filter 'TestName=LiveServer_GetAgents_ReturnsAgentList'" -ForegroundColor Gray
    Write-Host "  Skip slow tests:  .\run-live-tests.ps1 -SkipSlowTests" -ForegroundColor Gray
    Write-Host "  Different URL:    .\run-live-tests.ps1 -ApiUrl 'http://localhost:5000/'" -ForegroundColor Gray
    Write-Host ""
    
    exit $exitCode
} catch {
    Write-Host "❌ Failed to run tests: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}