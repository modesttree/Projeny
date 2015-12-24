@echo off

REM Option A - use the source directly - note that you will need python 3 installed for this to work
set PYTHONPATH=%~dp0\Source;%PYTHONPATH%
cd %~dp0\Source
python -m upm.main.Upm -ocf %1:%2

REM Option B - use compiled exe - this is easier for teams since you don't need python
REM cd %~dp0\Bin
REM Upm.exe -ocf %1:%2

if errorlevel 1 goto onerror
exit
:onerror
echo UPM.Bat completed with errors.  See ProjenyLog.txt for details.
pause
