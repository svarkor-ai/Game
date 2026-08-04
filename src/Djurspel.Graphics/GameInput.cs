using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Mathematics;
using System.Collections.Generic;
using System;

namespace Djurspel.Graphics;

/// <summary>
/// Inputhanterare för GameWindow.
/// Hanterar tangentbord och musinput.
/// </summary>
public class GameInput
{
    private readonly HashSet<int> _pressedKeys = new();
    private readonly HashSet<int> _pressedButtons = new();
    private Vector2 _lastMouseMove = Vector2.Zero;
    private readonly ICamera _camera;

    public GameInput(ICamera camera)
    {
        _camera = camera;
    }

    public void OnKeyDown(KeyboardKeyEventArgs e)
    {
        _pressedKeys.Add((int)e.Key);
    }

    public void OnKeyUp(KeyboardKeyEventArgs e)
    {
        _pressedKeys.Remove((int)e.Key);
    }

    public void OnMouseDown(MouseButtonEventArgs e)
    {
        if (e.IsPressed)
            _pressedButtons.Add((int)e.Button);
        else
            _pressedButtons.Remove((int)e.Button);
    }

    public void OnMouseUp(MouseButtonEventArgs e)
    {
        if (!e.IsPressed)
            _pressedButtons.Remove((int)e.Button);
    }

    public void OnMouseMove(MouseMoveEventArgs e)
    {
        _lastMouseMove = e.Delta;
        UpdateCameraPositionFromDelta(e.Delta);
    }

    public void OnMouseWheel(MouseWheelEventArgs e)
    {
        _camera.Zoom -= e.OffsetY * 0.5f;
    }

    public void ProcessMovement(float deltaTime)
    {
        var cameraPos = _camera.Position;
        float moveSpeed = 5f * (float)deltaTime;
        
        if (_pressedKeys.Contains((int)Keys.W))
            cameraPos.Y += moveSpeed;
        if (_pressedKeys.Contains((int)Keys.S))
            cameraPos.Y -= moveSpeed;
        if (_pressedKeys.Contains((int)Keys.A))
            cameraPos.X -= moveSpeed;
        if (_pressedKeys.Contains((int)Keys.D))
            cameraPos.X += moveSpeed;

        _camera.Position = cameraPos;
    }

    public void ProcessMouseLook()
    {
        if (_lastMouseMove != Vector2.Zero)
        {
            UpdateCameraPositionFromDelta(_lastMouseMove);
            _lastMouseMove = Vector2.Zero;
        }
    }

    private void UpdateCameraPositionFromDelta(Vector2 delta)
    {
        var pos = _camera.Position;
        pos.X -= delta.X * 0.5f;
        pos.Z -= delta.Y * 0.5f;
        _camera.Position = pos;
    }

    public bool IsKeyDown(int key) => _pressedKeys.Contains(key);
    public bool IsMouseButtonPressed(int button) => _pressedButtons.Contains(button);
    public Vector2 MouseDelta => _lastMouseMove;
}