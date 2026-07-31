using OpenTK.Mathematics;
using Djurspel.Core;
using System;
using System.Collections.Generic;

namespace Djurspel.Gameplay;

/// <summary>
/// Enemy AI med wander/chase/attack state maskin.
/// Simulerar enkelt beteende för fiender i ARPG.
/// </summary>
public class EnemyAI
{
    public enum State
    {
        Wander,
        Chase,
        Attack,
        Retreat
    }

    public Vector2 Position { get; set; }
    public Vector2 Target { get; set; }
    public float Speed { get; set; } = 2.0f;
    public float DetectionRange { get; set; } = 8.0f;
    public float AttackRange { get; set; } = 1.5f;
    public float WanderRadius { get; set; } = 5.0f;
    public float WanderTime { get; set; } = 3.0f;
    public State CurrentState { get; set; }
    public float StateTimer { get; set; }
    public int Health { get; set; } = 100;
    public int MaxHealth { get; set; } = 100;
    public int AttackDamage { get; set; } = 10;
    public float AttackCooldown { get; set; } = 1.0f;
    public float LastAttackTime { get; set; }

    private Vector2 _wanderTarget;
    private Random _random;

    public EnemyAI(Vector2 startPosition, Random? random = null)
    {
        Position = startPosition;
        Target = startPosition;
        _random = random ?? new Random();
        _wanderTarget = GenerateWanderTarget();
        CurrentState = State.Wander;
        StateTimer = 0f;
    }

    /// <summary>
    /// Uppdaterar AI med given frameTime och player position.
    /// </summary>
    public void Update(float frameTime, Vector2 playerPosition, IEventDispatcher? dispatcher = null)
    {
        StateTimer += frameTime;
        
        float distanceToPlayer = Vector2.Distance(Position, playerPosition);
        
        // State machine
        switch (CurrentState)
        {
            case State.Wander:
                UpdateWander(frameTime);
                
                // Transition to chase if player is near
                if (distanceToPlayer < DetectionRange)
                {
                    CurrentState = State.Chase;
                    Target = playerPosition;
                }
                break;
                
            case State.Chase:
                UpdateChase(frameTime, playerPosition);
                
                // Transition to attack if in range
                if (distanceToPlayer < AttackRange)
                {
                    CurrentState = State.Attack;
                    StateTimer = 0f;
                }
                
                // Return to wander if player moved too far
                if (distanceToPlayer > DetectionRange * 1.5f)
                {
                    CurrentState = State.Wander;
                    _wanderTarget = GenerateWanderTarget();
                }
                break;
                
            case State.Attack:
                UpdateAttack(frameTime, playerPosition);
                
                // Return to chase if player moved away during attack
                if (distanceToPlayer > AttackRange * 1.2f)
                {
                    CurrentState = State.Chase;
                    Target = playerPosition;
                }
                
                // Return to wander if attacked for too long without killing player
                if (StateTimer > 5.0f)
                {
                    CurrentState = State.Wander;
                    _wanderTarget = GenerateWanderTarget();
                }
                break;
                
            case State.Retreat:
                UpdateRetreat(frameTime);
                break;
        }
    }

    private void UpdateWander(float frameTime)
    {
        // Move towards wander target
        float distance = Vector2.Distance(Position, _wanderTarget);
        
        if (distance < 0.5f)
        {
            // Choose new wander target
            _wanderTarget = GenerateWanderTarget();
            StateTimer = 0f;
        }
        else
        {
            Vector2 direction = (_wanderTarget - Position).Normalized();
            Position += direction * Speed * 0.5f * frameTime; // Wander at half speed
        }
    }

    private void UpdateChase(float frameTime, Vector2 playerPosition)
    {
        // Move directly towards player
        Vector2 direction = (playerPosition - Position).Normalized();
        Position += direction * Speed * frameTime;
    }

    private void UpdateAttack(float frameTime, Vector2 playerPosition)
    {
        // Face towards player and attack
        if (StateTimer >= AttackCooldown)
        {
            // Perform attack
            // In a full implementation, this would trigger combat
            // For now, just log the attack event
            if (frameTime > 0.1f) // Prevent spam
            {
                StateTimer = 0f;
            }
        }
    }

    private void UpdateRetreat(float frameTime)
    {
        // Move away from target
        Vector2 direction = (Position - Target).Normalized();
        Position += direction * Speed * frameTime;
    }

    private Vector2 GenerateWanderTarget()
    {
        float angle = (float)(_random.NextDouble() * 2.0 * Math.PI);
        float radius = (float)(_random.NextDouble() * WanderRadius);
        
        return new Vector2(
            Position.X + MathF.Cos(angle) * radius,
            Position.Y + MathF.Sin(angle) * radius
        );
    }

    public void TakeDamage(int damage)
    {
        Health = Math.Max(0, Health - damage);
    }

    public bool IsDead => Health <= 0;
}
