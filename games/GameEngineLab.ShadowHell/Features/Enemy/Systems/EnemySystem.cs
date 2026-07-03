using GameEngineLab.Core.Features.Ecs.Resources;
using GameEngineLab.Core.Features.Ecs.Systems;
using GameEngineLab.Core.Features.Ecs.Entities;
using GameEngineLab.Core.Features.Physics.Components;
using GameEngineLab.Core.Features.Rendering.Components;
using GameEngineLab.Core.Features.Rendering.Resources;
using GameEngineLab.ShadowHell.Features.Player.Components;
using GameEngineLab.ShadowHell.Features.Enemy.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;

namespace GameEngineLab.ShadowHell.Features.Enemy.Systems;

public sealed class EnemySystem : IGameSystem
{
    public int Order => 101; // Runs after physics updates and shape rendering

    private const float WorldWidth = 2048f;
    private const float WorldHeight = 1536f;

    public void Update(World world, FrameContext frameContext)
    {
        float dt = frameContext.DeltaSeconds;
        if (dt <= 0) return;

        // Find Player components
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

        if (!playerFound) return;

        Vector2 playerPos = playerTransform.Position;

        foreach (var entityId in world.GetEntitiesWith<EnemyComponent, TransformComponent, VelocityComponent, RigidBodyComponent>())
        {
            world.TryGetComponent<EnemyComponent>(entityId, out var enemy);
            world.TryGetComponent<TransformComponent>(entityId, out var transform);
            world.TryGetComponent<VelocityComponent>(entityId, out var velocity);
            world.TryGetComponent<RigidBodyComponent>(entityId, out var body);

            if (enemy.Health <= 0f)
            {
                world.DestroyEntity(entityId);
                continue;
            }

            enemy.AnimTime += dt;

            Vector2 direction = playerPos - transform.Position;
            float dist = direction.Length();

            if (enemy.Type == EnemyType.Melee)
            {
                // Simple AI: steer towards the player
                if (dist > 15f)
                {
                    Vector2 dir = direction;
                    dir.Normalize();
                    velocity.Value = dir * enemy.Speed;
                }
                else
                {
                    velocity.Value = Vector2.Zero;
                }

                // Melee contact damage: reduce a full life point (1.0f)
                if (dist < 32f) // player radius (18) + enemy radius (20) = 38. Collision overlaps happen around 32f.
                {
                    bool isInvincible = player.InvincibilityTimer > 0f || player.State == PlayerState.Rolling;
                    if (!isInvincible)
                    {
                        player.Health = Math.Max(0f, player.Health - 1.0f);
                        player.InvincibilityTimer = 1.2f;

                        // Respawn if defeated
                        if (player.Health <= 0f)
                        {
                            player.Health = player.MaxHealth;
                            player.InvincibilityTimer = 2.0f;
                            playerTransform.Position = new Vector2(WorldWidth / 2f, WorldHeight / 2f);
                            world.SetComponent(playerEntity, playerTransform);
                        }

                        world.SetComponent(playerEntity, player);
                    }
                }
            }
            else if (enemy.Type == EnemyType.Ranged)
            {
                // Ranged AI: maintain distance (shoot from afar)
                Vector2 dir = direction;
                if (dist > 0.01f) dir.Normalize();

                if (dist > 220f)
                {
                    velocity.Value = dir * enemy.Speed;
                }
                else if (dist < 140f)
                {
                    velocity.Value = -dir * enemy.Speed; // Retreat slightly if too close
                }
                else
                {
                    velocity.Value = Vector2.Zero; // Hold position
                }

                // Shooting mechanics: shoot every 1.5s
                enemy.ShootTimer -= dt;
                if (enemy.ShootTimer <= 0f)
                {
                    enemy.ShootTimer = enemy.ShootCooldown;

                    // Spawn bullet heading towards the player
                    if (dist > 10f)
                    {
                        var bulletEntity = world.CreateEntity();
                        world.SetComponent(bulletEntity, new TransformComponent { Position = transform.Position });
                        world.SetComponent(bulletEntity, new BulletComponent(dir * 280f, damage: 0.5f));
                    }
                }
            }

            // Procedural crawl animation: squish and stretch the shadow body
            float baseRadius = 20f;
            body.BoundingRadius = baseRadius + (float)Math.Sin(enemy.AnimTime * 8f) * 4f;

            world.SetComponent(entityId, enemy);
            world.SetComponent(entityId, velocity);
            world.SetComponent(entityId, body);
        }
    }

    public void Draw(World world, FrameContext frameContext)
    {
        if (frameContext.SpriteBatch == null || frameContext.DebugPixel == null) return;

        // Draw black and white style enemies
        foreach (var entityId in world.GetEntitiesWith<EnemyComponent, TransformComponent, RigidBodyComponent>())
        {
            world.TryGetComponent<EnemyComponent>(entityId, out var enemy);
            world.TryGetComponent<TransformComponent>(entityId, out var transform);
            world.TryGetComponent<RigidBodyComponent>(entityId, out var body);

            if (enemy.Type == EnemyType.Melee)
            {
                // Draw single white outline
                ShapeRenderer.DrawCircleOutline(
                    frameContext.SpriteBatch, 
                    frameContext.DebugPixel, 
                    transform.Position, 
                    body.BoundingRadius, 
                    Color.White, 
                    2
                );
            }
            else
            {
                // Draw double orange outline for Ranged enemies
                Color orangeGlow = new Color(255, 100, 0);
                ShapeRenderer.DrawCircleOutline(
                    frameContext.SpriteBatch, 
                    frameContext.DebugPixel, 
                    transform.Position, 
                    body.BoundingRadius, 
                    orangeGlow, 
                    2
                );
                ShapeRenderer.DrawCircleOutline(
                    frameContext.SpriteBatch, 
                    frameContext.DebugPixel, 
                    transform.Position, 
                    Math.Max(1f, body.BoundingRadius - 4f), 
                    orangeGlow, 
                    1
                );
            }

            // Glowing eye in the center of the body
            Vector2 eyePos = transform.Position;
            Color eyeColor = (enemy.Type == EnemyType.Melee) ? Color.White : new Color(255, 100, 0);
            
            // Draw eye core
            ShapeRenderer.DrawCircle(frameContext.SpriteBatch, frameContext.DebugPixel, eyePos, 4f, eyeColor);
            // Draw eye glow
            ShapeRenderer.DrawCircle(frameContext.SpriteBatch, frameContext.DebugPixel, eyePos, 8f, eyeColor * 0.45f);

            // Draw Enemy Health Bar
            if (enemy.Health < enemy.MaxHealth && enemy.Health > 0f)
            {
                float barWidth = 24f;
                float barHeight = 4f;
                Vector2 barPos = transform.Position + new Vector2(0f, -body.BoundingRadius - 10f);
                float percent = MathHelper.Clamp(enemy.Health / enemy.MaxHealth, 0f, 1f);

                // Red background (representing missing health)
                ShapeRenderer.DrawRectangle(
                    frameContext.SpriteBatch,
                    frameContext.DebugPixel,
                    barPos,
                    new Vector2(barWidth, barHeight),
                    Color.Red * 0.6f
                );

                // Green foreground (representing remaining health)
                float healthWidth = barWidth * percent;
                if (healthWidth > 0f)
                {
                    // Draw centered green bar offset based on percent
                    Vector2 greenCenter = barPos + new Vector2((healthWidth - barWidth) / 2f, 0f);
                    ShapeRenderer.DrawRectangle(
                        frameContext.SpriteBatch,
                        frameContext.DebugPixel,
                        greenCenter,
                        new Vector2(healthWidth, barHeight),
                        Color.Green
                    );
                }
            }
        }
    }
}
