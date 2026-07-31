using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Djurspel.Core;
using System;
using System.Collections.Generic;

namespace Djurspel.Gameplay;

/// <summary>
/// ARPG Input Manager — hanterar WASD/arrows movement, space för attack, E för interaction, I för inventory.
/// Ersätter den gamla InputManager:n för ARPG-style kontroll.
/// </summary>
public class ARPGInputManager
{
    private readonly object _window;
    private readonly IEventDispatcher? _dispatcher;
    
    // Movement state
    private readonly Dictionary<Keys, bool> _pressedKeys = new();
    public Vector2 MovementDirection { get; private set; }
    
    // Action state
    public bool AttackPressed { get; private set; }
    public bool InteractPressed { get; private set; }
    public bool InventoryToggled { get; private set; }
    public bool PreviousInventoryState { get; set; } = false;
    
    // Mouse state
    public Vector2 MousePosition { get; private set; }
    public bool MouseLeftPressed { get; private set; }
    public bool MouseRightPressed { get; private set; }

    public ARPGInputManager(object window, IEventDispatcher? dispatcher = null)
    {
        _window = window;
        _dispatcher = dispatcher;
    }

    /// <summary>
    /// Hook up callbacks from the window.
    /// </summary>
    public void AttachToWindow(object window)
    {
        // Use reflection to hook up events dynamically
        var keyDownEvent = window.GetType().GetEvent("KeyDown");
        var keyUpEvent = window.GetType().GetEvent("KeyUp");
        var mouseMoveEvent = window.GetType().GetEvent("MouseMove");
        var mouseButtonDownEvent = window.GetType().GetEvent("MouseButtonDown");
        var mouseButtonUpEvent = window.GetType().GetEvent("MouseButtonUp");

        if (keyDownEvent != null)
        {
            var handler = Delegate.CreateDelegate(
                keyDownEvent.EventHandlerType, this, nameof(OnKeyDown));
            keyDownEvent.AddEventHandler(window, handler);
        }
        if (keyUpEvent != null)
        {
            var handler = Delegate.CreateDelegate(
                keyUpEvent.EventHandlerType, this, nameof(OnKeyUp));
            keyUpEvent.AddEventHandler(window, handler);
        }
        if (mouseMoveEvent != null)
        {
            var handler = Delegate.CreateDelegate(
                mouseMoveEvent.EventHandlerType, this, nameof(OnMouseMove));
            mouseMoveEvent.AddEventHandler(window, handler);
        }
        if (mouseButtonDownEvent != null)
        {
            var handler = Delegate.CreateDelegate(
                mouseButtonDownEvent.EventHandlerType, this, nameof(OnMouseButtonDown));
            mouseButtonDownEvent.AddEventHandler(window, handler);
        }
        if (mouseButtonUpEvent != null)
        {
            var handler = Delegate.CreateDelegate(
                mouseButtonUpEvent.EventHandlerType, this, nameof(OnMouseButtonUp));
            mouseButtonUpEvent.AddEventHandler(window, handler);
        }
    }

    private void OnKeyDown(object? sender, object e)
    {
        var keyProp = e.GetType().GetProperty("Key");
        if (keyProp != null)
        {
            var key = (Keys)keyProp.GetValue(e)!;
            _pressedKeys[key] = true;
            
            if (key == Keys.I)
                InventoryToggled = true;
            if (key == Keys.E)
            {
                InteractPressed = true;
                if (_dispatcher != null)
                    _dispatcher.Dispatch(new InteractEvent { Position = MousePosition });
            }
        }
    }

    private void OnKeyUp(object? sender, object e)
    {
        var keyProp = e.GetType().GetProperty("Key");
        if (keyProp != null)
        {
            var key = (Keys)keyProp.GetValue(e)!;
            _pressedKeys.Remove(key);
        }
    }

    private void OnMouseMove(object? sender, object e)
    {
        var xProp = e.GetType().GetProperty("X");
        var yProp = e.GetType().GetProperty("Y");
        if (xProp != null && yProp != null)
        {
            MousePosition = new Vector2(
                Convert.ToSingle(xProp.GetValue(e)),
                Convert.ToSingle(yProp.GetValue(e)));
        }
    }

    private void OnMouseButtonDown(object? sender, object e)
    {
        var buttonProp = e.GetType().GetProperty("Button");
        if (buttonProp != null)
        {
            var button = (MouseButton)buttonProp.GetValue(e)!;
            if (button == MouseButton.Left)
            {
                MouseLeftPressed = true;
                AttackPressed = true;
            }
            else if (button == MouseButton.Right)
            {
                MouseRightPressed = true;
            }
        }
    }

    private void OnMouseButtonUp(object? sender, object e)
    {
        var buttonProp = e.GetType().GetProperty("Button");
        if (buttonProp != null)
        {
            var button = (MouseButton)buttonProp.GetValue(e)!;
            if (button == MouseButton.Left)
            {
                MouseLeftPressed = false;
                AttackPressed = false;
            }
            else if (button == MouseButton.Right)
            {
                MouseRightPressed = false;
            }
        }
    }

    /// <summary>
    /// Uppdaterar input state med given frameTime.
    /// Beräknar movement direction baserat på tangentbord.
    /// </summary>
    public void Update(float frameTime)
    {
        MovementDirection = Vector2.Zero;
        
        if (_pressedKeys.ContainsKey(Keys.W) || _pressedKeys.ContainsKey(Keys.Up))
            MovementDirection = MovementDirection with { Y = MovementDirection.Y + 1.0f };
        if (_pressedKeys.ContainsKey(Keys.S) || _pressedKeys.ContainsKey(Keys.Down))
            MovementDirection = MovementDirection with { Y = MovementDirection.Y - 1.0f };
        if (_pressedKeys.ContainsKey(Keys.A) || _pressedKeys.ContainsKey(Keys.Left))
            MovementDirection = MovementDirection with { X = MovementDirection.X - 1.0f };
        if (_pressedKeys.ContainsKey(Keys.D) || _pressedKeys.ContainsKey(Keys.Right))
            MovementDirection = MovementDirection with { X = MovementDirection.X + 1.0f };
        
        // Normalize movement vector
        if (MovementDirection.Length > 0)
        {
            MovementDirection = MovementDirection.Normalized();
        }
        
        // Clear one-frame presses
        AttackPressed = false;
        InteractPressed = false;
        InventoryToggled = false;
    }

    /// <summary>
    /// Hämtar movement direction normalized.
    /// </summary>
    public Vector2 GetNormalizedMovement()
    {
        if (MovementDirection.Length > 0)
        {
            return MovementDirection.Normalized();
        }
        return Vector2.Zero;
    }
}

// Simple event for interaction
public class InteractEvent : IEvent
{
    public Vector2 Position { get; set; }
    public double Timestamp { get; set; }
}
