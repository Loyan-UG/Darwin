@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0mustang-einvoice-wrapper.ps1" %*
exit /b %ERRORLEVEL%
