@echo off
title Madanuru Voter Seva - Backend API
echo Starting Backend API (ASP.NET Core on http://localhost:5000)...
cd /d "%~dp0MadanuruVoterSeva\backend"
dotnet run --no-restore
pause
