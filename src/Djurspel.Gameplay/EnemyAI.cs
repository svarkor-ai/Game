using OpenTK.Mathematics;
using System;

namespace Djurspel.Gameplay;

/// <summary>
/// Fiende med AI, olika typer och beteenden.
/// </summary>
public class EnemyAI
{
    public enum Type
    {
        Goblin,    // Liten, snabb, svag
        Orc,       // Stor, långsam, stark
        Skeleton,  // Medelstor, medelstark
        Demon      // Stor, långsam, mycket stark
    }

    public enum State
    {
        Idle,
        Wander,
        Chase,
        Attack,
        Flee
    }

    public Vector2 Position { get; set; }
    public Type EnemyType { get; }
    public State CurrentState { get; set; }
    public int Health { get; set; }
    public int MaxHealth { get; }
    public int Damage { get; }
    public float Speed { get; }
    public float AttackRange { get; }
    public float ChaseRange { get; }
    public bool IsDead => Health <= 0;
    public Vector2 Velocity { get; private set; }
    
    private readonly Random _rng;
    private float _wanderTimer;
    private Vector2 _wanderTarget;
    private float _attackCooldown;
    private float _fleeThreshold;
    
    private static readonly Vector4[] TypeColors = {
        new Vector4(0.6f, 0.8f, 0.2f, 1.0f), // Goblin - green
        new Vector4(0.8f, 0.4f, 0.2f, 1.0f), // Orc - orange
        new Vector4(0.9f, 0.9f, 0.9f, 1.0f), // Skeleton - white
        new Vector4(0.9f, 0.1f, 0.1f, 1.0f)  // Demon - red
    };

    public EnemyAI(Vector2 position, Random? rng = null)
    {
        Position = position;
        _rng = rng ?? new Random();
        EnemyType = (Type)_rng.Next(0, 4);
        
        // Set properties based on type
        switch (EnemyType)
        {
            case Type.Goblin:
                Health = MaxHealth = 30;
                Damage = 5;
                Speed = 3.0f;
                AttackRange = 1.0f;
                ChaseRange = 5.0f;
                _fleeThreshold = 0.3f;
                break;
            case Type.Orc:
                Health = MaxHealth = 60;
                Damage = 15;
                Speed = 1.5f;
                AttackRange = 1.5f;
                ChaseRange = 4.0f;
                _fleeThreshold = 0.2f;
                break;
            case Type.Skeleton:
                Health = MaxHealth = 45;
                Damage = 10;
                Speed = 2.0f;
                AttackRange = 1.2f;
                ChaseRange = 4.5f;
                _fleeThreshold = 0.25f;
                break;
            case Type.Demon:
                Health = MaxHealth = 100;
                Damage = 25;
                Speed = 1.0f;
                AttackRange = 2.0f;
                ChaseRange = 6.0f;
                _fleeThreshold = 0.1f;
                break;
        }
        
        CurrentState = State.Wander;
        _wanderTimer = 0;
        _attackCooldown = 0;
        GenerateWanderTarget();
    }

    public void Update(float frameTime, Vector2 playerPosition)
    {
        if (IsDead) return;
        
        _attackCooldown = Math.Max(0, _attackCooldown - frameTime);
        
        float distanceToPlayer = Vector2.Distance(Position, playerPosition);
        bool shouldFlee = Health < MaxHealth * _fleeThreshold;
        
        // State machine
        switch (CurrentState)
        {
            case State.Idle:
                UpdateIdle(frameTime);
                break;
            case State.Wander:
                UpdateWander(frameTime);
                break;
            case State.Chase:
                UpdateChase(frameTime, playerPosition);
                break;
            case State.Attack:
                UpdateAttack(frameTime, playerPosition);
                break;
            case State.Flee:
                UpdateFlee(frameTime);
                break;
        }
        
        // State transitions
        if (shouldFlee && !IsDead)
        {
            CurrentState = State.Flee;
        }
        else if (distanceToPlayer <= AttackRange && _attackCooldown <= 0)
        {
            CurrentState = State.Attack;
        }
        else if (distanceToPlayer <= ChaseRange)
        {
            CurrentState = State.Chase;
        }
        else if (distanceToPlayer > ChaseRange * 1.5f)
        {
            CurrentState = State.Idle;
        }
    }

    private void UpdateIdle(float frameTime)
    {
        _wanderTimer -= frameTime;
        if (_wanderTimer <= 0)
        {
            CurrentState = State.Wander;
            GenerateWanderTarget();
        }
    }

    private void UpdateWander(float frameTime)
    {
        float distanceToTarget = Vector2.Distance(Position, _wanderTarget);
        if (distanceToTarget > 0.1f)
        {
            Vector2 direction = Vector2.Normalize(_wanderTarget - Position);
            Velocity = direction * Speed * 0.5f;
            Position += Velocity * frameTime;
        }
        else
        {
            CurrentState = State.Idle;
            _wanderTimer = 2.0f + (float)_rng.NextDouble() * 3.0f;
        }
    }

    private void UpdateChase(float frameTime, Vector2 playerPosition)
    {
        Vector2 direction = Vector2.Normalize(playerPosition - Position);
        Velocity = direction * Speed;
        Position += Velocity * frameTime;
    }

    private void UpdateAttack(float frameTime, Vector2 playerPosition)
    {
        if (_attackCooldown <= 0)
        {
            // Attack!
            _attackCooldown = 1.0f;
            // Return damage that would be dealt
            // In real implementation, this would damage the player
        }
    }

    private void UpdateFlee(float frameTime)
    {
        // Flee away from player
        Vector2 direction = Vector2.Normalize(Position - new Vector2(0, 0)); // Flee from origin
        Velocity = direction * Speed;
        Position += Velocity * frameTime;
        
        // Return to wander when health is low enough
        if (Health > MaxHealth * 0.5f)
        {
            CurrentState = State.Wander;
            GenerateWanderTarget();
        }
    }

    private void GenerateWanderTarget()
    {
        float angle = (float)_rng.NextDouble() * MathF.PI * 2;
        float distance = 2.0f + (float)_rng.NextDouble() * 3.0f;
        _wanderTarget = Position + new Vector2(
            MathF.Cos(angle) * distance,
            MathF.Sin(angle) * distance
        );
    }

    public void TakeDamage(int damage)
    {
        Health = Math.Max(0, Health - damage);
    }

    public Vector4 GetColor() => TypeColors[(int)EnemyType];
}
