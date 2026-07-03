using GameEngineLab.Core.Features.Ecs.Components;
using Microsoft.Xna.Framework;

namespace GameEngineLab.ShadowHell.Features.Player.Components;

public enum PlayerState
{
    Idle,
    Walking,
    Flying, // elevated flight state
    Rolling  // dodge roll on ground
}

public struct PlayerComponent : IComponent
{
    public PlayerState State;
    
    // Movement configuration
    public float NormalSpeed;
    public float RollSpeed;
    public Vector2 MovementDirection;
    public float RotationAngle;
    public bool FacingRight; // true = Right, false = Left

    // Dodge Roll state
    public float RollDuration;
    public float RollTimer;
    public Vector2 RollDirection;

    // Flight/Hover state (z-axis in 2D topdown)
    public float JumpZ;
    public float JumpVelocityZ;
    public float GravityZ;
    public float JumpStrength;
    public Vector2 JumpStartPos;
    
    // Flight control
    public float FlightDuration; // maximum flight duration in seconds
    public float FlightTimer;

    // Procedural animation clocks
    public float AnimationTime;

    // Life points & invincibility
    public float Health;
    public float MaxHealth;
    public float InvincibilityTimer;

    // Melee attack state
    public float AttackTimer;
    public float AttackCooldownTimer;
    public Vector2 AttackDirection;
    public float AttackDuration;
    public float AttackCooldown;
    
    public PlayerComponent()
    {
        State = PlayerState.Idle;
        NormalSpeed = 220f;
        RollSpeed = 480f;
        MovementDirection = Vector2.Zero;
        RotationAngle = 0f;
        FacingRight = true;

        RollDuration = 0.45f;
        RollTimer = 0f;
        RollDirection = Vector2.Zero;

        JumpZ = 0f;
        JumpVelocityZ = 0f;
        GravityZ = 900f; // matches top-down arc feel
        JumpStrength = 350f;
        JumpStartPos = Vector2.Zero;

        FlightDuration = 2.0f; // float for 2 seconds max
        FlightTimer = 0f;

        AnimationTime = 0f;

        // Base 2 life points
        Health = 2f;
        MaxHealth = 2f;
        InvincibilityTimer = 0f;

        // Attack settings
        AttackTimer = 0f;
        AttackCooldownTimer = 0f;
        AttackDirection = Vector2.Zero;
        AttackDuration = 0.22f;
        AttackCooldown = 0.35f;
    }
}
