using GameEngineLab.Core.Features.Ecs.Resources;
using GameEngineLab.Core.Features.Ecs.Systems;
using GameEngineLab.Core.Features.Physics.Components;
using GameEngineLab.Core.Features.Rendering.Resources;
using GameEngineLab.ShadowHell.Features.Player.Components;
using GameEngineLab.ShadowHell.Features.Environment.Resources;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace GameEngineLab.ShadowHell.Features.Player.Systems;

public sealed class PlayerRendererSystem : IGameSystem
{
    public int Order => 105; // Render player on top of shape renderings and enemies

    public void Update(World world, FrameContext frameContext) { }

    public void Draw(World world, FrameContext frameContext)
    {
        if (frameContext.SpriteBatch == null || frameContext.DebugPixel == null) return;

        // Retrieve generated textures
        world.TryGetResource<GameTextureResource>(out var textures);

        foreach (var entityId in world.GetEntitiesWith<PlayerComponent, TransformComponent, RigidBodyComponent>())
        {
            world.TryGetComponent<PlayerComponent>(entityId, out var player);
            world.TryGetComponent<TransformComponent>(entityId, out var transform);
            world.TryGetComponent<RigidBodyComponent>(entityId, out var body);

            // 1. Draw floor shadow (scales down as the player flies higher)
            float shadowScale = Math.Max(0.4f, 1f - (player.JumpZ / 120f));
            float shadowRadius = body.BoundingRadius * shadowScale;
            
            // Adjust the centers and offsets to account for top-down perspective, size, and object alignment.
            // In top-down 2D, the shadow is projected at the feet of the object (bottom of the collision circle),
            // while the body is offset upwards.
            float verticalOffset = body.BoundingRadius * 0.6f;
            Vector2 shadowCenter = transform.Position + new Vector2(0f, verticalOffset);

            ShapeRenderer.DrawEllipse(
                frameContext.SpriteBatch, 
                frameContext.DebugPixel, 
                shadowCenter, 
                new Vector2(shadowRadius * 2f, shadowRadius * 1f), 
                new Color(0, 0, 0, 100)
            );

            // 2. Draw player body offset by JumpZ (elevation) and offset upwards slightly
            Vector2 bodyCenter = transform.Position - new Vector2(0f, player.JumpZ + verticalOffset * 0.5f);

            // Flashing effect during invincibility frames
            if (player.InvincibilityTimer > 0f && (int)(player.InvincibilityTimer * 20f) % 2 == 0)
            {
                continue;
            }

            Color glowColor = new Color(177, 0, 255); // Neon Purple
            Color innerGlowColor = new Color(220, 120, 255); // Light Violet

            // Draw soft breathing purple light aura around the player (smaller and less bright)
            if (textures != null)
            {
                float pulse = (float)Math.Sin(player.AnimationTime * 4f) * 0.03f;
                float heightFactor = player.JumpZ / 64f;
                float auraScale = (0.4f + heightFactor * 0.05f) + pulse;
                Vector2 auraSize = new Vector2(textures.LightTexture.Width, textures.LightTexture.Height) * auraScale;
                Vector2 auraTopLeft = bodyCenter - auraSize / 2f;
                Color auraColor = glowColor * (0.22f - heightFactor * 0.04f);

                frameContext.SpriteBatch.Draw(
                    textures.LightTexture,
                    new Rectangle((int)auraTopLeft.X, (int)auraTopLeft.Y, (int)auraSize.X, (int)auraSize.Y),
                    auraColor
                );
            }

            // Draw smooth flying/motion trail if rolling
            if (player.State == PlayerState.Rolling)
            {
                for (int i = 1; i <= 3; i++)
                {
                    float offsetDist = i * 8f;
                    Vector2 trailPos = bodyCenter - player.RollDirection * offsetDist;
                    float alpha = 0.45f / i;
                    
                    // Trail circles
                    ShapeRenderer.DrawCircleOutline(
                        frameContext.SpriteBatch, 
                        frameContext.DebugPixel, 
                        trailPos, 
                        body.BoundingRadius, 
                        glowColor * alpha, 
                        2
                    );
                }
            }

            // Draw solid deep black core circle
            ShapeRenderer.DrawCircle(
                frameContext.SpriteBatch, 
                frameContext.DebugPixel, 
                bodyCenter, 
                body.BoundingRadius, 
                new Color(10, 8, 15) // Deep shadow black core
            );

            // Draw glowing purple/violet borders
            // Outer soft glow outline
            ShapeRenderer.DrawCircleOutline(
                frameContext.SpriteBatch, 
                frameContext.DebugPixel, 
                bodyCenter, 
                body.BoundingRadius + 1f, 
                glowColor * 0.6f, 
                2
            );

            // Inner crisp outline
            ShapeRenderer.DrawCircleOutline(
                frameContext.SpriteBatch, 
                frameContext.DebugPixel, 
                bodyCenter, 
                body.BoundingRadius, 
                innerGlowColor, 
                2
            );

            // 3. Draw glowing melee swipe arc if attacking
            if (player.AttackTimer > 0f)
            {
                float progress = 1f - (player.AttackTimer / player.AttackDuration);
                float attackAngle = (float)Math.Atan2(player.AttackDirection.Y, player.AttackDirection.X);
                
                // Sweep angle bounds
                float startSweep = attackAngle - 1.1f;
                float endSweep = attackAngle + 1.1f;
                // Swing direction can depend on facing/movement, here standard clockwise sweep
                float currentSweep = startSweep + (endSweep - startSweep) * progress;

                int segments = 8;
                float sweepRadius = body.BoundingRadius + 22f; // extends outward from body
                Color swipeCore = new Color(220, 120, 255); // Light Violet neon
                Color swipeGlow = new Color(177, 0, 255);   // Neon Purple

                Vector2 prevPos = Vector2.Zero;
                for (int i = 0; i <= segments; i++)
                {
                    float ratio = (float)i / segments;
                    float angle = startSweep + (currentSweep - startSweep) * ratio;
                    Vector2 offset = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * sweepRadius;
                    Vector2 pos = bodyCenter + offset;

                    if (i > 0)
                    {
                        // Draw outer glow segment (thicker, semi-transparent)
                        ShapeRenderer.DrawLine(
                            frameContext.SpriteBatch, 
                            frameContext.DebugPixel, 
                            prevPos, 
                            pos, 
                            swipeGlow * (0.2f + ratio * 0.5f), 
                            6
                        );

                        // Draw inner crisp core segment
                        ShapeRenderer.DrawLine(
                            frameContext.SpriteBatch, 
                            frameContext.DebugPixel, 
                            prevPos, 
                            pos, 
                            swipeCore * (0.4f + ratio * 0.6f), 
                            2
                        );
                    }
                    prevPos = pos;
                }
            }
        }
    }
}
