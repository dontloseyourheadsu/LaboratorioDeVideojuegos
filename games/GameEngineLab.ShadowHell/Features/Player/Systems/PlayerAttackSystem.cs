using GameEngineLab.Core.Features.Ecs.Resources;
using GameEngineLab.Core.Features.Ecs.Systems;
using GameEngineLab.Core.Features.Ecs.Entities;
using GameEngineLab.Core.Features.Physics.Components;
using GameEngineLab.ShadowHell.Features.Player.Components;
using GameEngineLab.ShadowHell.Features.Enemy.Components;
using Microsoft.Xna.Framework;
using System;

namespace GameEngineLab.ShadowHell.Features.Player.Systems;

public sealed class PlayerAttackSystem : IGameSystem
{
    public int Order => 15; // Runs after inputs and physics step, but before render

    public void Update(World world, FrameContext frameContext)
    {
        // 1. Locate the player
        EntityId playerEntity = default;
        PlayerComponent player = default;
        TransformComponent playerTransform = default;
        bool playerFound = false;

        foreach (var entity in world.GetEntitiesWith<PlayerComponent, TransformComponent>())
        {
            world.TryGetComponent<PlayerComponent>(entity, out player);
            world.TryGetComponent<TransformComponent>(entity, out playerTransform);
            playerEntity = entity;
            playerFound = true;
            break;
        }

        if (!playerFound || !player.JustAttacked) return;

        // Reset the JustAttacked flag
        player.JustAttacked = false;
        world.SetComponent(playerEntity, player);

        // Apply a small forward lunge to the player on attack trigger!
        if (world.TryGetComponent<VelocityComponent>(playerEntity, out var playerVelocity))
        {
            playerVelocity.Value += player.AttackDirection * 160f;
            world.SetComponent(playerEntity, playerVelocity);
        }

        Vector2 playerPos = playerTransform.Position;
        float attackRange = 65f;

        // 2. Scan all enemies in range and cone
        foreach (var enemyEntity in world.GetEntitiesWith<EnemyComponent, TransformComponent, VelocityComponent>())
        {
            world.TryGetComponent<EnemyComponent>(enemyEntity, out var enemy);
            world.TryGetComponent<TransformComponent>(enemyEntity, out var enemyTransform);
            world.TryGetComponent<VelocityComponent>(enemyEntity, out var enemyVelocity);

            Vector2 toEnemy = enemyTransform.Position - playerPos;
            float dist = toEnemy.Length();

            if (dist <= attackRange)
            {
                Vector2 dirToEnemy = toEnemy;
                if (dist > 0.01f) dirToEnemy.Normalize();

                // Compute cosine of angle between attack direction and direction to enemy
                float dot = Vector2.Dot(player.AttackDirection, dirToEnemy);

                // cos(65 degrees) ≈ 0.42. We check if enemy is within a 130-degree cone (dot >= 0.42f)
                if (dot >= 0.42f)
                {
                    // Deal damage
                    enemy.Health = Math.Max(0f, enemy.Health - 1.0f);
                    
                    // Apply knockback velocity (pushing them back in the attack direction)
                    enemyVelocity.Value += player.AttackDirection * 380f;

                    world.SetComponent(enemyEntity, enemy);
                    world.SetComponent(enemyEntity, enemyVelocity);
                }
            }
        }
    }

    public void Draw(World world, FrameContext frameContext) { }
}
