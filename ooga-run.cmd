@echo off
rem Runs one .ooga file in its own window, then waits so you can read what it said.
rem install.ps1 makes double-clicking a .ooga file use this.
title Ooga - %~nx1
cd /d "%~dp1"
call "%~dp0ooga.cmd" "%~nx1"
echo.
echo (ooga done. press any key to close this window)
pause >nul
