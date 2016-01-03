@echo off

set PYTHONPATH=%~dp0\..\..\Source;%PYTHONPATH%
set errorlevel=
python -m prj.main.EditorApi %*
REM Forward the error level
exit /b %errorlevel%
