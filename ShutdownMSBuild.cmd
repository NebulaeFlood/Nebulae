@echo off
taskkill /F /IM MSBuild.exe /T 2>NUL
taskkill /F /IM dotnet.exe /T 2>NUL