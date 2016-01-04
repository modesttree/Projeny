@echo off
call PackageBuild.bat
START %~dp0\..\Installer\Dist\ProjenyInstaller.exe
