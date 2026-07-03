using GameEngineLab.Core.Features.Ecs.Resources;
using GameEngineLab.Core.Features.Ecs.Systems;
using GameEngineLab.Core.Features.Physics.Components;
using GameEngineLab.ShadowHell.Features.Player.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

namespace GameEngineLab.ShadowHell.Features.Player.Systems;

public sealed class PlayerInputSystem : IGameSystem
{
    public int Order => 1; // Runs early, before physics

    public void Update(World world, FrameContext frameContext)
    {
        float dt = frameContext.DeltaSeconds;
        if (dt <= 0) return;

        var kState = frameContext.CurrentKeyboard;
        var prevKState = frameContext.PreviousKeyboard;

        foreach (var entityId in world.GetEntitiesWith<PlayerComponent, VelocityComponent, TransformComponent, RigidBodyComponent>())
        {
            world.TryGetComponent<PlayerComponent>(entityId, out var player);
            world.TryGetComponent<VelocityComponent>(entityId, out var velocity);
            world.TryGetComponent<TransformComponent>(entityId, out var transform);
            world.TryGetComponent<RigidBodyComponent>(entityId, out var body);

            player.AnimationTime += dt;
            if (player.InvincibilityTimer > 0f)
            {
                player.InvincibilityTimer -= dt;
            }

            if (player.AttackTimer > 0f)
            {
                player.AttackTimer -= dt;
            }
            if (player.AttackCooldownTimer > 0f)
            {
                player.AttackCooldownTimer -= dt;
            }

            // 1. Standing constraint: The player NEVER rotates (Isaac-style)
            transform.Rotation = 0f;

            // 2. Read directional movement inputs
            Vector2 moveDir = Vector2.Zero;
            if (kState.IsKeyDown(Keys.W) || kState.IsKeyDown(Keys.Up)) moveDir.Y -= 1f;
            if (kState.IsKeyDown(Keys.S) || kState.IsKeyDown(Keys.Down)) moveDir.Y += 1f;
            if (kState.IsKeyDown(Keys.A) || kState.IsKeyDown(Keys.Left)) moveDir.X -= 1f;
            if (kState.IsKeyDown(Keys.D) || kState.IsKeyDown(Keys.Right)) moveDir.X += 1f;

            if (moveDir != Vector2.Zero)
            {
                moveDir.Normalize();
                player.MovementDirection = moveDir;
                
                // Track facing direction (Left vs Right) for skeleton flipping
                if (moveDir.X > 0.01f) player.FacingRight = true;
                else if (moveDir.X < -0.01f) player.FacingRight = false;
            }
            else
            {
                player.MovementDirection = Vector2.Zero;
            }

            // 3. Handle Roll and Ground States
            if (player.State == PlayerState.Rolling)
            {
                // Dodge Rolling state
                player.RollTimer -= dt;
                
                // Allow slight steering adjustment during the roll for a smooth, flying steering feel
                if (moveDir != Vector2.Zero)
                {
                    player.RollDirection = Vector2.Lerp(player.RollDirection, moveDir, 5f * dt);
                    if (player.RollDirection != Vector2.Zero) player.RollDirection.Normalize();
                }

                // Smooth ease-out speed curve
                float progress = 1f - (player.RollTimer / player.RollDuration);
                float speedFactor = 1.25f - 0.75f * (progress * progress); // starts fast, decelerates
                velocity.Value = player.RollDirection * player.RollSpeed * speedFactor;
                
                // Smooth rise and fall for the roll (acting like a smooth hover/flight jump)
                // Peaks at progress = 0.5 (sine goes to 1)
                player.JumpZ = (float)Math.Sin(progress * Math.PI) * 24f;

                if (player.RollTimer <= 0f)
                {
                    player.JumpZ = 0f;
                    player.State = player.MovementDirection != Vector2.Zero ? PlayerState.Walking : PlayerState.Idle;
                }
            }
            else
            {
                // Ground State (Idle / Walking)
                player.JumpZ = 0f;

                if (player.MovementDirection != Vector2.Zero)
                {
                    player.State = PlayerState.Walking;
                    float speedMult = player.AttackTimer > 0f ? 0.40f : 1f;
                    velocity.Value = player.MovementDirection * player.NormalSpeed * speedMult;
                }
                else
                {
                    player.State = PlayerState.Idle;
                    velocity.Value = Vector2.Zero;
                }

                // Trigger Attack
                bool attackJustPressed = (kState.IsKeyDown(Keys.F) && prevKState.IsKeyUp(Keys.F)) ||
                                         (kState.IsKeyDown(Keys.E) && prevKState.IsKeyUp(Keys.E)) ||
                                         (frameContext.CurrentMouse.LeftButton == ButtonState.Pressed && frameContext.PreviousMouse.LeftButton == ButtonState.Released);

                if (attackJustPressed && player.State != PlayerState.Rolling && player.AttackCooldownTimer <= 0f)
                {
                    player.AttackTimer = player.AttackDuration;
                    player.AttackCooldownTimer = player.AttackCooldown;
                    player.AttackDirection = player.MovementDirection != Vector2.Zero ? player.MovementDirection : 
                        new Vector2(player.FacingRight ? 1f : -1f, 0f);
                    player.JustAttacked = true;
                }

                // Trigger Roll from ground (both Shift and Space trigger it)
                bool shiftJustPressed = (kState.IsKeyDown(Keys.LeftShift) || kState.IsKeyDown(Keys.RightShift)) &&
                                        (prevKState.IsKeyUp(Keys.LeftShift) && prevKState.IsKeyUp(Keys.RightShift));
                bool spaceJustPressed = kState.IsKeyDown(Keys.Space) && prevKState.IsKeyUp(Keys.Space);

                if (shiftJustPressed || spaceJustPressed)
                {
                    player.State = PlayerState.Rolling;
                    player.RollTimer = player.RollDuration;
                    player.RollDirection = player.MovementDirection != Vector2.Zero ? player.MovementDirection : 
                        new Vector2(player.FacingRight ? 1f : -1f, 0f);
                    player.AttackTimer = 0f; // Cancel any active attack on roll
                }
            }

            // 4. Ground attack immunity during flight
            // Adjust collision groups or masks so that when player is flying (player.JumpZ > 20f), 
            // enemies (CollisionGroup 2) ignore collisions, but player still collides with walls (CollisionGroup 1).
            // Let's implement this!
            // Walls are Static (Mass = 0), Enemies are Dynamic (Mass = 1.2).
            // In our cavern generator, we can set CollisionGroups:
            // - Cavern walls & pillars: CollisionGroup = 1 (Static cavern structures)
            // - Player: CollisionGroup = 4
            // - Enemies: CollisionGroup = 2
            // Normally, Player collides with both walls (1) and enemies (2).
            // When flying, Player changes CollisionMask to only collide with walls (1), ignoring enemies (2)!
            // This is mathematically elegant and fully resolves the flight gameplay mechanics!
            if (player.JumpZ > 20f || player.State == PlayerState.Rolling)
            {
                // Only collide with walls (Group 1)
                body.CollisionMask = 1;
            }
            else
            {
                // Collide with both walls (Group 1) and enemies (Group 2)
                body.CollisionMask = 1 | 2;
            }

            // Save components back
            world.SetComponent(entityId, player);
            world.SetComponent(entityId, velocity);
            world.SetComponent(entityId, transform);
            world.SetComponent(entityId, body);
        }
    }

    public void Draw(World world, FrameContext frameContext) { }
}
