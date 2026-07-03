using GameEngineLab.Core.Features.Ecs.Resources;
using GameEngineLab.Core.Features.Ecs.Systems;
using GameEngineLab.Core.Features.Animation.Components;
using GameEngineLab.ShadowHell.Features.Player.Components;
using Microsoft.Xna.Framework;
using System;

namespace GameEngineLab.ShadowHell.Features.Player.Systems;

public sealed class PlayerAnimationSystem : IGameSystem
{
    public int Order => 50; // Runs after inputs/physics, before skeleton FK solver

    public void Update(World world, FrameContext frameContext)
    {
        foreach (var entityId in world.GetEntitiesWith<PlayerComponent, SkeletonComponent>())
        {
            world.TryGetComponent<PlayerComponent>(entityId, out var player);
            world.TryGetComponent<SkeletonComponent>(entityId, out var skeleton);

            var bones = skeleton.Bones;
            if (bones == null || bones.Count < 14) continue;

            float t = player.AnimationTime;

            // Extract default bone configurations to modify
            var torso = bones[0];
            var head = bones[1];
            var haloOuter = bones[2];
            var haloInner = bones[3];
            var lWing1 = bones[4];
            var lWing2 = bones[5];
            var rWing1 = bones[6];
            var rWing2 = bones[7];
            var lArm = bones[8];
            var lFist = bones[9];
            var rArm = bones[10];
            var rFist = bones[11];
            var lLeg = bones[12];
            var rLeg = bones[13];

            // Define base offsets & angles (matching values in PlayerFactory)
            Vector2 torsoBaseOffset = Vector2.Zero;
            float torsoBaseAngle = 0f;

            Vector2 headBaseOffset = new Vector2(0f, -20f);
            float headBaseAngle = 0f;

            Vector2 lWing1BaseOffset = new Vector2(-6f, -4f);
            float lWing1BaseAngle = (float)(Math.PI + 0.4f);

            Vector2 lWing2BaseOffset = new Vector2(32f, 0f);
            float lWing2BaseAngle = -0.6f;

            Vector2 rWing1BaseOffset = new Vector2(6f, -4f);
            float rWing1BaseAngle = -0.4f;

            Vector2 rWing2BaseOffset = new Vector2(32f, 0f);
            float rWing2BaseAngle = 0.6f;

            Vector2 lArmBaseOffset = new Vector2(-8f, 0f);
            float lArmBaseAngle = (float)Math.PI;

            Vector2 lFistBaseOffset = new Vector2(14f, 0f);

            Vector2 rArmBaseOffset = new Vector2(8f, 0f);
            float rArmBaseAngle = 0f;

            Vector2 rFistBaseOffset = new Vector2(14f, 0f);

            Vector2 lLegBaseOffset = new Vector2(-6f, 10f);
            float lLegBaseAngle = (float)Math.PI / 2f;

            Vector2 rLegBaseOffset = new Vector2(6f, 10f);
            float rLegBaseAngle = (float)Math.PI / 2f;

            // Apply height shift for jumping/hovering
            float heightOffset = player.JumpZ;
            torso.LocalOffset = torsoBaseOffset + new Vector2(0f, -heightOffset);

            // Compute procedural animations based on state
            switch (player.State)
            {
                case PlayerState.Idle:
                    // Soft breathing bob
                    float idleBob = (float)Math.Sin(t * 3.5f) * 3f;
                    torso.LocalOffset = torsoBaseOffset + new Vector2(0f, -heightOffset + idleBob);
                    head.LocalOffset = headBaseOffset + new Vector2(0f, (float)Math.Sin(t * 3.5f) * 0.5f);
                    head.LocalAngle = headBaseAngle + (float)Math.Sin(t * 1.5f) * 0.05f;

                    // Wings sway/flap slowly
                    float slowFlap = (float)Math.Sin(t * 3.5f);
                    lWing1.LocalAngle = lWing1BaseAngle + slowFlap * 0.15f;
                    lWing2.LocalAngle = lWing2BaseAngle - slowFlap * 0.25f;
                    rWing1.LocalAngle = rWing1BaseAngle - slowFlap * 0.15f;
                    rWing2.LocalAngle = rWing2BaseAngle + slowFlap * 0.25f;

                    // Arms rest gently
                    lArm.LocalAngle = lArmBaseAngle + (float)Math.Cos(t * 3.5f) * 0.08f;
                    rArm.LocalAngle = rArmBaseAngle - (float)Math.Cos(t * 3.5f) * 0.08f;

                    // Legs align down
                    lLeg.LocalAngle = lLegBaseAngle + (float)Math.Sin(t * 1.5f) * 0.03f;
                    rLeg.LocalAngle = rLegBaseAngle - (float)Math.Sin(t * 1.5f) * 0.03f;
                    break;

                case PlayerState.Walking:
                    // Stride bobbing
                    float walkBob = (float)Math.Sin(t * 14f) * 4f;
                    torso.LocalOffset = torsoBaseOffset + new Vector2(0f, -heightOffset + Math.Abs(walkBob));
                    head.LocalOffset = headBaseOffset + new Vector2(0f, (float)Math.Sin(t * 14f) * 1.2f);
                    head.LocalAngle = headBaseAngle + (float)Math.Sin(t * 14f) * 0.06f;

                    // Wings flap rapidly
                    float walkFlap = (float)Math.Sin(t * 15f);
                    lWing1.LocalAngle = lWing1BaseAngle + walkFlap * 0.3f;
                    lWing2.LocalAngle = lWing2BaseAngle - walkFlap * 0.4f;
                    rWing1.LocalAngle = rWing1BaseAngle - walkFlap * 0.3f;
                    rWing2.LocalAngle = rWing2BaseAngle + walkFlap * 0.4f;

                    // Alternating leg stride
                    float stride = (float)Math.Sin(t * 12f) * 0.5f;
                    lLeg.LocalAngle = lLegBaseAngle + stride;
                    rLeg.LocalAngle = rLegBaseAngle - stride;

                    // Arms swing opposite to legs
                    lArm.LocalAngle = lArmBaseAngle - stride * 0.6f;
                    rArm.LocalAngle = rArmBaseAngle - stride * 0.6f;
                    break;

                case PlayerState.Flying:
                    // Hovering flying bob
                    float flightBob = (float)Math.Sin(t * 7f) * 4f;
                    torso.LocalOffset = torsoBaseOffset + new Vector2(0f, -heightOffset + flightBob);
                    head.LocalOffset = headBaseOffset + new Vector2(0f, (float)Math.Sin(t * 7f) * 1f);

                    // Continuous energetic wing flaps
                    float flightFlap = (float)Math.Sin(t * 18f);
                    lWing1.LocalAngle = (float)(Math.PI + 0.3f) + flightFlap * 0.5f;
                    lWing2.LocalAngle = -0.5f - flightFlap * 0.6f;
                    rWing1.LocalAngle = -0.3f - flightFlap * 0.5f;
                    rWing2.LocalAngle = 0.5f + flightFlap * 0.6f;

                    // Legs dangle vertically
                    lLeg.LocalAngle = lLegBaseAngle + 0.2f * (float)Math.Sin(t * 3f);
                    rLeg.LocalAngle = rLegBaseAngle - 0.2f * (float)Math.Sin(t * 3f);

                    // Arms slightly raised/extended
                    lArm.LocalAngle = lArmBaseAngle - 0.2f;
                    rArm.LocalAngle = rArmBaseAngle + 0.2f;
                    break;

                case PlayerState.Rolling:
                    // Tight curl animation
                    lWing1.LocalAngle = (float)(Math.PI + 1.2f);
                    lWing2.LocalAngle = -1.4f;
                    rWing1.LocalAngle = -1.2f;
                    rWing2.LocalAngle = 1.4f;

                    lLeg.LocalAngle = lLegBaseAngle + 0.7f;
                    rLeg.LocalAngle = rLegBaseAngle - 0.7f;

                    lArm.LocalAngle = lArmBaseAngle - 1.0f;
                    rArm.LocalAngle = rArmBaseAngle + 1.0f;

                    // Spin the entire skeleton procedurally by rotating the root bone (Torso)!
                    float progress = 1f - (player.RollTimer / player.RollDuration);
                    float rollSpin = progress * (float)Math.PI * 2f;
                    // Spin opposite if facing left
                    torso.LocalAngle = player.FacingRight ? rollSpin : -rollSpin;

                    // Extra roll bounce height
                    float rollHop = (float)Math.Sin(progress * Math.PI) * 12f;
                    torso.LocalOffset = torsoBaseOffset + new Vector2(0f, -heightOffset - rollHop);
                    break;
            }

            // If we are NOT rolling, reset Torso local angle back to 0
            if (player.State != PlayerState.Rolling)
            {
                torso.LocalAngle = torsoBaseAngle;
            }

            if (player.AttackTimer > 0f)
            {
                // Swipe animation: angle starts high/up and swings down/forward
                float progress = 1f - (player.AttackTimer / player.AttackDuration);
                float swingAngle = -1.2f + progress * 2.4f; // swings from -1.2 to +1.2 radians

                // Both arms react to attack
                rArm.LocalAngle = swingAngle;
                rArm.LocalOffset = rArmBaseOffset + new Vector2(6f, -3f);
                rFist.LocalOffset = rFistBaseOffset + new Vector2(8f, 0f);

                lArm.LocalAngle = (float)Math.PI + swingAngle * 0.4f;
                lArm.LocalOffset = lArmBaseOffset + new Vector2(4f, -1f);
                lFist.LocalOffset = lFistBaseOffset + new Vector2(8f, 0f);
            }

            // 5. Horizontal flip mirroring if facing left (ScaleX logic)
            if (!player.FacingRight)
            {
                // To mirror the skeleton horizontally, we:
                // - Negate local offsets on the X-axis
                // - Negate local rotation angles
                
                // Torso
                torso.LocalOffset = new Vector2(-torso.LocalOffset.X, torso.LocalOffset.Y);
                torso.LocalAngle = -torso.LocalAngle;

                // Head
                head.LocalOffset = new Vector2(-head.LocalOffset.X, head.LocalOffset.Y);
                head.LocalAngle = -head.LocalAngle;

                // Halo (Outer/Inner offsets are X=0, so their X positions stay aligned)
                haloOuter.LocalAngle = -haloOuter.LocalAngle;
                haloInner.LocalAngle = -haloInner.LocalAngle;

                // Wings (Left becomes right, Right becomes left visually)
                // Swap left and right wing configurations to maintain structural sanity when flipped
                float tempLW1Angle = lWing1.LocalAngle;
                Vector2 tempLW1Offset = lWing1.LocalOffset;
                float tempLW2Angle = lWing2.LocalAngle;
                Vector2 tempLW2Offset = lWing2.LocalOffset;

                lWing1.LocalAngle = -rWing1.LocalAngle;
                lWing1.LocalOffset = new Vector2(-rWing1.LocalOffset.X, rWing1.LocalOffset.Y);
                lWing2.LocalAngle = -rWing2.LocalAngle;
                lWing2.LocalOffset = new Vector2(-rWing2.LocalOffset.X, rWing2.LocalOffset.Y);

                rWing1.LocalAngle = -tempLW1Angle;
                rWing1.LocalOffset = new Vector2(-tempLW1Offset.X, tempLW1Offset.Y);
                rWing2.LocalAngle = -tempLW2Angle;
                rWing2.LocalOffset = new Vector2(-tempLW2Offset.X, tempLW2Offset.Y);

                // Arms
                float tempLArmAngle = lArm.LocalAngle;
                Vector2 tempLArmOffset = lArm.LocalOffset;
                lArm.LocalAngle = -rArm.LocalAngle;
                lArm.LocalOffset = new Vector2(-rArm.LocalOffset.X, rArm.LocalOffset.Y);
                rArm.LocalAngle = -tempLArmAngle;
                rArm.LocalOffset = new Vector2(-tempLArmOffset.X, tempLArmOffset.Y);

                lFist.LocalOffset = new Vector2(-lFist.LocalOffset.X, lFist.LocalOffset.Y);
                lFist.LocalAngle = -lFist.LocalAngle;
                rFist.LocalOffset = new Vector2(-rFist.LocalOffset.X, rFist.LocalOffset.Y);
                rFist.LocalAngle = -rFist.LocalAngle;

                // Legs
                float tempLLegAngle = lLeg.LocalAngle;
                Vector2 tempLLegOffset = lLeg.LocalOffset;
                lLeg.LocalAngle = -rLeg.LocalAngle;
                lLeg.LocalOffset = new Vector2(-rLeg.LocalOffset.X, rLeg.LocalOffset.Y);
                rLeg.LocalAngle = -tempLLegAngle;
                rLeg.LocalOffset = new Vector2(-tempLLegOffset.X, tempLLegOffset.Y);
            }

            // Save modified bone structures back
            bones[0] = torso;
            bones[1] = head;
            bones[2] = haloOuter;
            bones[3] = haloInner;
            bones[4] = lWing1;
            bones[5] = lWing2;
            bones[6] = rWing1;
            bones[7] = rWing2;
            bones[8] = lArm;
            bones[9] = lFist;
            bones[10] = rArm;
            bones[11] = rFist;
            bones[12] = lLeg;
            bones[13] = rLeg;

            skeleton.Bones = bones;
            world.SetComponent(entityId, skeleton);
        }
    }

    public void Draw(World world, FrameContext frameContext) { }
}
