@echo off
REM Unit Test Gap Analyzer - Quick Start Script
REM Usage: run-analyzer.bat [path-to-solution]

echo ========================================
echo Unit Test Gap Analyzer
echo ========================================
echo.

if "%~1"=="" (
    echo Usage: run-analyzer.bat [path-to-solution]
    echo.
    echo Example: run-analyzer.bat "C:\MyProject\MySolution"
    echo.
    echo Or run without arguments for interactive mode:
    dotnet run --project UnitTestGapAnalyzer.csproj
) else (
    echo Analyzing solution: %~1
    echo.
    dotnet run --project UnitTestGapAnalyzer.csproj "%~1"
)

echo.
echo ========================================
echo Analysis Complete
echo ========================================
pause
