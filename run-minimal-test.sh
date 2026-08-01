#!/bin/bash
# Run minimal OpenGL test with Xvfb and capture screenshot

export PATH="$HOME/.dotnet:$PATH"

# Kill any existing Xvfb
pkill -f "Xvfb :99" 2>/dev/null || true
sleep 0.5

# Start Xvfb
Xvfb :99 -screen 0 1280x720x24 -ac 2>&1 &
XVFB_PID=$!
sleep 1

export DISPLAY=:99

cd /tmp/repos/game

# Run the minimal test (it exits after one frame)
# The test opens a window, clears to blue, and exits
timeout 10 dotnet run --project minimal-test.csproj 2>&1 &
DOTNET_PID=$!

# Give it a moment to render
sleep 3

# Take screenshot
if command -v scrot &>/dev/null; then
    scrot /tmp/game-minimal-test.png 2>&1 && echo "Screenshot saved: /tmp/game-minimal-test.png" || echo "Screenshot FAILED (scrot)"
elif command -v import &>/dev/null; then
    import -window root /tmp/game-minimal-test.png 2>&1 && echo "Screenshot saved: /tmp/game-minimal-test.png" || echo "Screenshot FAILED (import)"
else
    echo "WARNING: No screenshot tool found (scrot or import)"
fi

# Cleanup
kill $DOTNET_PID 2>/dev/null || true
kill $XVFB_PID 2>/dev/null || true
wait 2>/dev/null

echo "=== Done ==="