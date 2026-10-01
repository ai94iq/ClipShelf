@echo off
setlocal EnableExtensions
cd /d "%~dp0"
set "RESULT=0"

call test.bat --no-pause
if errorlevel 1 goto :fail
call build.bat --no-pause
if errorlevel 1 goto :fail
dotnet build "installer\ClipShelf.Installer.wixproj" -c Release -o "artifacts\installer" -v q -nologo
if errorlevel 1 goto :fail

echo [package] OK: artifacts\installer
goto :end

:fail
set "RESULT=1"
echo [package] FAILED

:end
if /i not "%~1"=="--no-pause" pause
exit /b %RESULT%
