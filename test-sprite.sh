#!/bin/bash
set -e
export PATH="$HOME/.dotnet:$PATH"

# Kill any existing Xvfb
pkill -f Xvfb 2>/dev/null || true
sleep 0.5

# Start Xvfb
Xvfb :99 -screen 0 1280x720x24 &
sleep 1

# Run Djurspel and take screenshot after 3 seconds
cd /home/svarkor/svarkor-builds/djurspel/src/Djurspel.Program
timeout 12 dotnet run 2>&1 &
DOTNET_PID=$!

sleep 5

# Take screenshot
DISPLAY=:99 scrot /tmp/djurspel-screenshot.png 2>/dev/null && echo "Screenshot saved" || echo "Screenshot failed"

# Kill the dotnet process
kill $DOTNET_PID 2>/dev/null || true
wait $DOTNET_PID 2>/dev/null || true

# Kill Xvfb
pkill -f Xvfb 2>/dev/null || true
