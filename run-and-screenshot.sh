#!/bin/bash
export PATH="$HOME/.dotnet:$PATH"

# Kill any existing Xvfb
pkill -f "Xvfb :99" 2>/dev/null || true
sleep 0.5

# Start Xvfb
Xvfb :99 -screen 0 1280x720x24 -ac 2>&1 &
XVFB_PID=$!
sleep 1

export DISPLAY=:99

# Start Djurspel in background
cd /home/svarkor/svarkor-builds/djurspel/src/Djurspel.Program
dotnet run --no-build -c Debug 2>&1 &
DOTNET_PID=$!

# Wait a moment for rendering to start
sleep 3

# Take screenshot
scrot /tmp/djurspel-render.png 2>&1 && echo "Screenshot saved" || echo "Screenshot FAILED"

# Wait a bit more and take another
sleep 2
scrot /tmp/djurspel-render2.png 2>&1 && echo "Screenshot 2 saved" || echo "Screenshot 2 FAILED"

# Cleanup
kill $DOTNET_PID 2>/dev/null || true
kill $XVFB_PID 2>/dev/null || true
wait 2>/dev/null

echo "=== Done ==="
