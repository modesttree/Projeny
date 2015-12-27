@echo off

REM Option A - use the source directly - note that you will need python 3 installed for this to work
set PYTHONPATH=%~dp0\..\..\Source;%PYTHONPATH%
set errorlevel=
python -m upm.main.Upm %*
REM Forward the error level
exit /b %errorlevel%

REM Option B - use compiled exe - this is easier for teams since you don't need python
REM Bin\Upm.exe %*
