@echo off

set PYTHONPATH=%~dp0\..\Source;%PYTHONPATH%
python -m prj.main.OpenInVisualStudio %1 %2

if errorlevel 1 goto onerror
exit
:onerror
echo ProjenyOpenInVisualStudio.Bat completed with errors.  See log file for details.
pause
