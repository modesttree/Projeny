@echo off

set PYTHONPATH=%~dp0\..;%PYTHONPATH%
set errorlevel=
python -m prj.main.EditorApi %*
REM Forward the error level
exit /b %errorlevel%
