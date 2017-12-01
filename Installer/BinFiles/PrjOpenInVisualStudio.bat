@echo off
"%~dp0\Data\OpenInVisualStudio.exe" %*
IF '%ERRORLEVEL%'=='0' GOTO OK
echo Error occurred!  Unable to open visual studio.
pause
:OK
