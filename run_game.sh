#!/bin/bash
export DISPLAY=:99
export PATH=/home/svarkor/.dotnet:$PATH
cd /home/svarkor/svarkor-builds/djurspel
/home/svarkor/.dotnet/dotnet run --project src/Djurspel.Program/Djurspel.Program.csproj 2>&1 | tee /tmp/game_output.log
