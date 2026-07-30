#!/bin/bash
set -e
export PATH="$HOME/.dotnet:$PATH"

# Kill any existing Xvfb safely
pkill -f "Xvfb :99" 2>/dev/null || true
sleep 0.5

# Start Xvfb on :99
Xvfb :99 -screen 0 1280x720x24 -ac +extension GLX +extension GLX 2>&1 &
XVFB_PID=$!
sleep 1
echo "Xvfb started, PID: $XVFB_PID"

# Verify X server is running by trying xeyes or xset
export DISPLAY=:99
xset q > /dev/null 2>&1 && echo "X server is responding" || echo "WARNING: xset q failed, but continuing..."

# Build first
cd /home/svarkor/svarkor-builds/djurspel/src/Djurspel.Program
echo "=== Building ==="
dotnet build 2>&1
echo "=== Build complete ==="

# Run with visible output
echo "=== Starting Djurspel ==="
timeout 15 dotnet run --no-build 2>&1 &
DOTNET_PID=$!
sleep 4

# Check if process is alive
if kill -0 $DOTNET_PID 2>/dev/null; then
    echo "Process $DOTNET_PID is running"
else
    echo "Process $DOTNET_PID died"
fi

# Take screenshot
echo "=== Taking screenshot ==="
scrot /tmp/djurspel-debug.png 2>&1 && echo "Screenshot saved" || echo "Screenshot FAILED"

# Kill
echo "=== Cleanup ==="
kill $DOTNET_PID 2>/dev/null || true
kill $XVFB_PID 2>/dev/null || true
echo "=== Done ==="
