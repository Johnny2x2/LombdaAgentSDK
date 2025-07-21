@echo off
REM Batch script to run LombdaAgent MAUI live integration tests

setlocal

REM Default configuration
set DEFAULT_API_URL=http://localhost:5000/
set DEFAULT_TEST_FILTER=Category=Network

REM Parse command line arguments
set API_URL=%DEFAULT_API_URL%
set TEST_FILTER=%DEFAULT_TEST_FILTER%
set SKIP_SLOW=false

:parse_args
if "%1"=="--help" goto show_help
if "%1"=="-h" goto show_help
if "%1"=="--api-url" (
    set API_URL=%2
    shift
    shift
    goto parse_args
)
if "%1"=="--skip-slow" (
    set SKIP_SLOW=true
    shift
    goto parse_args
)
if "%1"=="--test-filter" (
    set TEST_FILTER=%2
    shift
    shift
    goto parse_args
)
if not "%1"=="" (
    shift
    goto parse_args
)

goto main

:show_help
echo.
echo LombdaAgent MAUI Live Integration Test Runner
echo ===========================================
echo.
echo Usage:
echo   run-live-tests.bat [options]
echo.
echo Options:
echo   --api-url ^<url^>     API URL to test against (default: https://localhost:5001/)
echo   --test-filter ^<filter^> NUnit test filter (default: Category=Network)
echo   --skip-slow         Skip slow tests that involve AI processing
echo   --help, -h          Show this help message
echo.
echo Examples:
echo   run-live-tests.bat
echo   run-live-tests.bat --api-url http://localhost:5000/
echo   run-live-tests.bat --skip-slow
echo   run-live-tests.bat --test-filter "TestName=LiveServer_CreateAgent_ReturnsValidAgent"
echo.
echo Environment Variables:
echo   OPENAI_API_KEY      Required for the API to function
echo.
goto end

:main
echo.
echo 🚀 LombdaAgent MAUI Live Integration Test Runner
echo =================================================
echo.

REM Set environment variables
set LOMBDA_TEST_API_URL=%API_URL%
set LOMBDA_RUN_LIVE_TESTS=true

REM Adjust test filter for slow tests
if "%SKIP_SLOW%"=="true" (
    if "%TEST_FILTER%"=="Category=Network" (
        set TEST_FILTER=Category=Network^&Category!=Slow
    ) else (
        set TEST_FILTER=%TEST_FILTER%^&Category!=Slow
    )
)

echo Configuration:
echo   API URL: %API_URL%
echo   Test Filter: %TEST_FILTER%
echo   Skip Slow Tests: %SKIP_SLOW%
if defined OPENAI_API_KEY (
    echo   OpenAI Key Set: Yes
) else (
    echo   OpenAI Key Set: No
)
echo.

REM Check if API is accessible
echo 🔍 Checking API accessibility...
powershell -Command "try { $response = Invoke-WebRequest -Uri '%API_URL%v1/agents' -Method GET -TimeoutSec 10; if ($response.StatusCode -eq 200) { Write-Host '✅ API is accessible!' -ForegroundColor Green; exit 0 } else { Write-Host '⚠️ API returned status code:' $response.StatusCode -ForegroundColor Yellow; exit 1 } } catch { Write-Host '❌ Cannot access API' -ForegroundColor Red; exit 1 }"

if errorlevel 1 (
    echo.
    echo ❌ Cannot access API at %API_URL%
    echo.
    echo Please ensure:
    echo   1. The LombdaAgentAPI is running
    echo   2. The URL is correct
    echo   3. Your OpenAI API key is set: set OPENAI_API_KEY=your-key
    echo.
    echo To start the API, run:
    echo   cd LombdaAgentAPI
    echo   dotnet run
    echo.
    goto end_error
)

echo.

REM Navigate to test directory
cd /d "%~dp0.."
if errorlevel 1 (
    echo ❌ Failed to navigate to test directory
    goto end_error
)

REM Run the tests
echo 🧪 Running tests...
echo Command: dotnet test --filter "%TEST_FILTER%" --verbosity normal
echo.

dotnet test --filter "%TEST_FILTER%" --verbosity normal

if errorlevel 1 (
    echo.
    echo ❌ Some tests failed. Exit code: %errorlevel%
    echo.
    goto show_quick_commands
) else (
    echo.
    echo ✅ All tests passed!
    echo.
)

:show_quick_commands
echo 🔧 Quick Commands:
echo   Run single test: dotnet test --filter "TestName=LiveServer_GetAgents_ReturnsAgentList"
echo   Skip slow tests:  run-live-tests.bat --skip-slow
echo   Different URL:    run-live-tests.bat --api-url "http://localhost:5000/"
echo.
goto end

:end_error
exit /b 1

:end
exit /b 0