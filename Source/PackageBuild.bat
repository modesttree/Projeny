@echo off

set PYTHONPATH=%~dp0;%PYTHONPATH%
python -m prj.main.PackageBuild %*
