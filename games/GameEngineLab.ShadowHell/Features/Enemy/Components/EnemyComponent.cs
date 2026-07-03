using GameEngineLab.Core.Features.Ecs.Components;

namespace GameEngineLab.ShadowHell.Features.Enemy.Components;

public enum EnemyType
{
    Melee,
    Ranged
}

public struct EnemyComponent : IComponent
{
    public EnemyType Type;
    public float Speed;
    public float AnimTime;
    
    // Ranged shooting
    public float ShootTimer;
    public float ShootCooldown;

    // Enemy Health
    public float Health;
    public float MaxHealth;

    public EnemyComponent()
    {
        Type = EnemyType.Melee;
        Speed = 65f; // slower than player
        AnimTime = 0f;
        ShootTimer = 0f;
        ShootCooldown = 1.5f;
        Health = 3f;
        MaxHealth = 3f;
    }

    public EnemyComponent(EnemyType type, float speed = 65f, float shootCooldown = 1.5f)
    {
        Type = type;
        Speed = speed;
        AnimTime = 0f;
        ShootTimer = 0f;
        ShootCooldown = shootCooldown;
        Health = type == EnemyType.Melee ? 3f : 1.5f;
        MaxHealth = type == EnemyType.Melee ? 3f : 1.5f;
    }
}
