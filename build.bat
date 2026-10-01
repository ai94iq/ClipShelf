@echo off
setlocal EnableExtensions
cd /d "%~dp0"
set "RESULT=0"
set "OUT=artifacts\publish\win-x64"

if exist "%OUT%" rmdir /s /q "%OUT%"
dotnet publish "src\ClipShelf.App\ClipShelf.App.csproj" -c Release -r win-x64 --self-contained true -p:PublishReadyToRun=true -o "%OUT%" -v q -nologo
if errorlevel 1 goto :fail

echo [build] OK: %OUT%\ClipShelf.exe
goto :end

:fail
set "RESULT=1"
echo [build] FAILED

:end
if /i not "%~1"=="--no-pause" pause
exit /b %RESULT%
