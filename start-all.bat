@echo off
title Madanuru Voter Seva Launcher
echo ========================================================
echo       MADANURU VOTER SEVA - DESK APPLICATION
echo ========================================================
echo.
echo Launching Backend API...
start "Madanuru Voter Seva - Backend" cmd /k "%~dp0start-backend.bat"
timeout /t 3 >nul
echo Launching Frontend Web UI...
start "Madanuru Voter Seva - Frontend" cmd /k "%~dp0start-frontend.bat"
echo.
echo Both services launched! 
echo Frontend Dev Server: http://localhost:5173
echo Backend API and Production UI: http://localhost:5000
echo.
pause
