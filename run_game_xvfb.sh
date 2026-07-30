#!/bin/bash
export PATH=/home/svarkor/.dotnet:$PATH
export DISPLAY=${DISPLAY:-:99}
cd /home/svarkor/svarkor-builds/djurspel
/home/svarkor/.dotnet/dotnet src/Djurspel.Program/bin/Debug/net8.0/Djurspel.Program.dll 2>&1 | tee /tmp/game_xvfb.log
