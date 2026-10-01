@echo off
setlocal EnableExtensions
cd /d "%~dp0"
set "RESULT=0"

rem MTP mode (global.json): extra flags are forwarded to the test host, so keep it minimal.
dotnet test --solution "ClipShelf.slnx" -c Release
if errorlevel 1 goto :fail

echo [test] OK
goto :end

:fail
set "RESULT=1"
echo [test] FAILED

:end
if /i not "%~1"=="--no-pause" pause
exit /b %RESULT%
