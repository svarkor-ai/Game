#!/bin/bash
set -e
export PATH="$HOME/.dotnet:$PATH"

# Kill any existing Xvfb safely
pkill -f "Xvfb :99" 2>/dev/null || true
sleep 0.5

# Start Xvfb on :99
Xvfb :99 -screen 0 1280x720x24 -ac 2>&1 &
XVFB_PID=$!
sleep 1
echo "Xvfb started, PID: $XVFB_PID"

export DISPLAY=:99

# Build
echo "=== Building minimal-test ==="
cd /tmp/djurspel-test
dotnet build 2>&1
echo "=== Build complete ==="

# Run - capture stdout/stderr to file
echo "=== Running minimal test ==="
timeout 10 dotnet run 2>&1 > /tmp/djurspel-test-output.txt &
DOTNET_PID=$!
sleep 5

# Check if process is alive
if kill -0 $DOTNET_PID 2>/dev/null; then
    echo "Process $DOTNET_PID is still running"
else
    echo "Process $DOTNET_PID died"
fi

# Show output
echo "=== Program output ==="
cat /tmp/djurspel-test-output.txt 2>/dev/null || echo "(no output)"

# Take screenshot
echo "=== Taking screenshot ==="
scrot /tmp/minimal-test.png 2>&1 && echo "Screenshot saved" || echo "Screenshot FAILED"

# Kill
echo "=== Cleanup ==="
kill $DOTNET_PID 2>/dev/null || true
kill $XVFB_PID 2>/dev/null || true
echo "=== Done ==="
