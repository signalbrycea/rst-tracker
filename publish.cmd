@echo off
cd /d "%~dp0"
dotnet publish -c Release -o dist
echo.
echo Output: %~dp0dist\RS3Tracker.exe
