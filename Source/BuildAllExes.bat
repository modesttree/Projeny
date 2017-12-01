@echo off

REM NOTE: You need to install python cx_freeze for this to work

set PYTHONPATH=%~dp0
python BuildAllExes.py build
