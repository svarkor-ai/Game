using OpenTK.Mathematics;
using Djurspel.Core;
using System.Collections.Generic;

namespace Djurspel.Graphics;

/// <summary>
/// 2D top-down camera med ortografisk projection och camera-follow.
/// Använder ortografisk projection istället för isometrisk projection.
/// </summary>
public class TopDownCamera : ICamera
{
    private Vector2 _position2d = Vector2.Zero;
    private float _zoom = 1.0f;
    private Vector2 _target = Vector2.Zero;
    private float _followSpeed = 5.0f;
    private int _windowWidth = 1280;
    private int _windowHeight = 720;
    private Matrix4 _viewMatrix = Matrix4.Identity;
    private Matrix4 _projectionMatrix = Matrix4.Identity;

    public Vector3 Position 
    { 
        get => new(_position2d.X, _position2d.Y, 0f); 
        set 
        {
            _position2d = new Vector2(value.X, value.Y);
            _target = _position2d;
        }
    }

    public float Zoom 
    { 
        get => _zoom; 
        set => _zoom = Math.Max(0.1f, Math.Min(5.0f, value));
    }

    public int WindowWidth 
    { 
        get => _windowWidth; 
        set => _windowWidth = Math.Max(100, value);
    }

    public int WindowHeight 
    { 
        get => _windowHeight; 
        set => _windowHeight = Math.Max(100, value);
    }

    /// <summary>
    /// Uppdaterar camera position med follow-logik.
    /// Lerp mot Target med FollowSpeed * frameTime.
    /// </summary>
    public void Update(float frameTime)
    {
        if (Target != Vector2.Zero || Position != Vector3.Zero)
        {
            float smoothFactor = 1.0f - MathF.Exp(-_followSpeed * frameTime);
            _position2d = Vector2.Lerp(_position2d, _target, smoothFactor);
        }
        
        // Update view and projection matrices
        _viewMatrix = GetViewMatrix();
        _projectionMatrix = GetProjectionMatrix();
    }

    /// <summary>
    /// Sätter camera-target till given position.
    /// </summary>
    public void SetTarget(Vector2 target)
    {
        _target = target;
    }

    public Vector2 Target { get => _target; set => _target = value; }
    public float FollowSpeed { get => _followSpeed; set => _followSpeed = value; }

    /// <summary>
    /// Skapar ortografisk projection-matrix för 2D top-down visning.
    /// Coordinates: X right, Y up, Z out of screen (negative Z is into screen).
    /// </summary>
    public Matrix4 GetProjectionMatrix()
    {
        float aspectRatio = _windowWidth / (float)_windowHeight;
        float viewSize = 25.0f * _zoom;
        float frustumWidth = viewSize * aspectRatio;
        float frustumHeight = viewSize;

        _projectionMatrix = Matrix4.CreateOrthographicOffCenter(
            -frustumWidth, frustumWidth,
            -frustumHeight, frustumHeight,
            -100.0f, 100.0f
        );

        return _projectionMatrix;
    }

    /// <summary>
    /// Skapar view-matrix för top-down kamera.
    /// Kikar neråt på scenen från +Z riktning.
    /// </summary>
    public Matrix4 GetViewMatrix()
    {
        // Top-down view: camera looks straight down along -Z axis
        // Camera is above the play field at Z=20, looking down at Z=0
        Vector3 cameraPos = new(_position2d.X, _position2d.Y, 20.0f);
        Vector3 lookAt = new(_position2d.X, _position2d.Y, 0.0f);
        Vector3 up = new(0, -1, 0); // "up" in the screen plane is -Y direction

        _viewMatrix = Matrix4.LookAt(cameraPos, lookAt, up);
        return _viewMatrix;
    }

    /// <summary>
    /// Konverterar screen-koordinater till world-koordinater.
    /// Returns Vector3 with Z=0 for 2D world coordinates.
    /// </summary>
    public Vector3 ScreenToWorld(Vector2 screenPos)
    {
        float aspectRatio = _windowWidth / (float)_windowHeight;
        float viewSize = 25.0f * _zoom;
        float frustumWidth = viewSize * aspectRatio;
        float frustumHeight = viewSize;

        float nx = (screenPos.X / _windowWidth - 0.5f) * 2.0f; // -1 to 1
        float ny = (screenPos.Y / _windowHeight - 0.5f) * 2.0f; // -1 to 1

        return new Vector3(
            _position2d.X + nx * frustumWidth,
            _position2d.Y + ny * frustumHeight,
            0f
        );
    }

    /// <summary>
    /// Konverterar world-koordinater till screen-koordinater.
    /// Z-coordinate is ignored in the conversion.
    /// </summary>
    public Vector2 WorldToScreen(Vector3 worldPos)
    {
        float aspectRatio = _windowWidth / (float)_windowHeight;
        float viewSize = 25.0f * _zoom;
        float frustumWidth = viewSize * aspectRatio;
        float frustumHeight = viewSize;

        float relX = (worldPos.X - _position2d.X) / frustumWidth;
        float relY = (worldPos.Y - _position2d.Y) / frustumHeight;

        return new Vector2(
            (relX + 0.5f) * _windowWidth,
            (relY + 0.5f) * _windowHeight
        );
    }

    /// <summary>
    /// Konverterar tile-koordinater till screen-koordinater.
    /// Förlitar sig på WorldToScreen för conversion.
    /// </summary>
    public Vector2 TileToScreen(Vector3i tile, float tilePixelWidth = 64f, float tilePixelHeight = 32f)
    {
        Vector3 worldPos = new(tile.X * tilePixelWidth, tile.Y * tilePixelHeight, 0f);
        return WorldToScreen(worldPos);
    }

    /// <summary>
    /// Konverterar screen-koordinater till tile-koordinater.
    /// Förlitar sig på ScreenToWorld för conversion.
    /// </summary>
    public Vector3i ScreenToTile(Vector2 screen, float tilePixelWidth = 64f, float tilePixelHeight = 32f)
    {
        Vector3 worldPos = ScreenToWorld(screen);
        return new Vector3i(
            (int)(worldPos.X / tilePixelWidth),
            (int)(worldPos.Y / tilePixelHeight),
            0
        );
    }

    /// <summary>
    /// Returnerar tom lista — top-down har ingen djursortering.
    /// </summary>
    public IEnumerable<Vector3i> GetDepthSortedOrder(IEnumerable<Vector3i> entities)
    {
        // In top-down 2D, depth sorting is not needed
        return entities;
    }

    /// <summary>
    /// Låter kameran följa en entitet med mjuk övergång.
    /// </summary>
    public void FollowEntity(object target, float smoothingFactor = 5f, float dt = 0.016f)
    {
        // Extract position from target using reflection or type checking
        if (target != null)
        {
            var positionProperty = target.GetType().GetProperty("Position");
            if (positionProperty != null && positionProperty.PropertyType == typeof(Vector2))
            {
                _target = (Vector2)positionProperty.GetValue(target)!;
            }
            else
            {
                // Try X and Y properties
                var xProp = target.GetType().GetProperty("X");
                var yProp = target.GetType().GetProperty("Y");
                if (xProp != null && yProp != null)
                {
                    float x = xProp.CanRead ? Convert.ToSingle(xProp.GetValue(target)) : 0f;
                    float y = yProp.CanRead ? Convert.ToSingle(yProp.GetValue(target)) : 0f;
                    _target = new Vector2(x, y);
                }
            }
        }
        _followSpeed = smoothingFactor;
    }
}
