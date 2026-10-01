@echo off
setlocal EnableExtensions
cd /d "%~dp0"

for /d /r %%d in (bin obj) do if exist "%%d" rmdir /s /q "%%d"
if exist "artifacts" rmdir /s /q "artifacts"

echo [clean] OK
if /i not "%~1"=="--no-pause" pause
exit /b 0
