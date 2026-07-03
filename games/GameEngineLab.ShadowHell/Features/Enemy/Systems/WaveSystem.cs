using GameEngineLab.Core.Features.Ecs.Resources;
using GameEngineLab.Core.Features.Ecs.Systems;
using GameEngineLab.Core.Features.Ecs.Entities;
using GameEngineLab.Core.Features.Physics.Components;
using GameEngineLab.Core.Features.Rendering.Components;
using GameEngineLab.ShadowHell.Features.Player.Components;
using GameEngineLab.ShadowHell.Features.Enemy.Components;
using GameEngineLab.ShadowHell.Features.Enemy.Resources;
using Microsoft.Xna.Framework;
using System;
using System.Linq;

namespace GameEngineLab.ShadowHell.Features.Enemy.Systems;

public sealed class WaveSystem : IGameSystem
{
    public int Order => 99; // Runs before EnemySystem updates

    private const float WorldWidth = 2048f;
    private const float WorldHeight = 1536f;
    private const float SpawnMargin = 150f;

    private readonly Random _random = new();

    public void Update(World world, FrameContext frameContext)
    {
        float dt = frameContext.DeltaSeconds;
        if (dt <= 0) return;

        if (!world.TryGetResource<WaveState>(out var waveState) || waveState == null)
        {
            return;
        }

        // 1. Count currently alive enemies
        int aliveCount = world.GetEntitiesWith<EnemyComponent>().Count();
        waveState.EnemiesRemaining = aliveCount;

        // 2. Manage Wave Progression
        if (waveState.EnemiesRemaining == 0 && waveState.EnemiesToSpawn == 0)
        {
            if (waveState.IsWaveActive)
            {
                // Wave just cleared! Transition to next wave
                waveState.IsWaveActive = false;
                waveState.NextWaveTimer = 3.0f; // 3 seconds rest time
            }
            else
            {
                // Wait for next wave timer to expire
                if (waveState.NextWaveTimer > 0f)
                {
                    waveState.NextWaveTimer -= dt;
                }
                else
                {
                    // Start Next Wave!
                    waveState.CurrentWave++;
                    
                    // Wave size formula: 4 + 2 * CurrentWave
                    int totalEnemies = 4 + waveState.CurrentWave * 2;
                    waveState.EnemiesToSpawn = totalEnemies;
                    waveState.TotalWaveEnemies = totalEnemies;
                    waveState.IsWaveActive = true;
                    waveState.SpawnCooldownTimer = 0f; // spawn first immediately
                    // Spawn faster for larger waves (cooldown scales down slightly, min 0.4s)
                    waveState.SpawnCooldown = Math.Max(0.4f, 1.2f - waveState.CurrentWave * 0.08f);
                }
            }
        }

        // 3. Handle Enemy Spawning
        if (waveState.IsWaveActive && waveState.EnemiesToSpawn > 0)
        {
            waveState.SpawnCooldownTimer -= dt;
            if (waveState.SpawnCooldownTimer <= 0f)
            {
                SpawnSingleEnemy(world, waveState);
                waveState.EnemiesToSpawn--;
                waveState.SpawnCooldownTimer = waveState.SpawnCooldown;
            }
        }
    }

    private void SpawnSingleEnemy(World world, WaveState waveState)
    {
        // Find Player position to spawn relative to them
        Vector2 playerPos = new Vector2(WorldWidth / 2f, WorldHeight / 2f);
        foreach (var entity in world.GetEntitiesWith<PlayerComponent, TransformComponent>())
        {
            if (world.TryGetComponent<TransformComponent>(entity, out var pt))
            {
                playerPos = pt.Position;
            }
            break;
        }

        // Choose a spawn location in a circular ring around the player (clamped to map)
        Vector2 spawnPos = playerPos;
        bool positionOk = false;
        int attempts = 0;

        float minX = SpawnMargin;
        float maxX = WorldWidth - SpawnMargin;
        float minY = SpawnMargin;
        float maxY = WorldHeight - SpawnMargin;

        while (!positionOk && attempts < 50)
        {
            float angle = (float)(_random.NextDouble() * Math.PI * 2);
            float distance = _random.Next(350, 600); // far enough to not spawn on player
            
            spawnPos = playerPos + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * distance;
            spawnPos.X = MathHelper.Clamp(spawnPos.X, minX, maxX);
            spawnPos.Y = MathHelper.Clamp(spawnPos.Y, minY, maxY);

            // Ensure not too close to player
            if (Vector2.Distance(spawnPos, playerPos) > 250f)
            {
                positionOk = true;
            }
            attempts++;
        }

        // Determine enemy type: Ranged ratio scales up with wave number
        // e.g. Wave 1 has only melee. Wave 2 has some ranged, etc.
        EnemyType type = EnemyType.Melee;
        if (waveState.CurrentWave >= 2)
        {
            // Chance of ranged increases with wave number
            double rangedChance = Math.Min(0.45, 0.15 + (waveState.CurrentWave - 2) * 0.05);
            if (_random.NextDouble() < rangedChance)
            {
                type = EnemyType.Ranged;
            }
        }

        // Enemy Speed increases slightly per wave to raise difficulty
        float speedScaling = 1f + waveState.CurrentWave * 0.03f;
        float baseSpeed = (type == EnemyType.Melee) ? 65f : 80f;
        float speed = baseSpeed * speedScaling;

        // Create ECS Enemy Entity
        var enemy = world.CreateEntity();
        world.SetComponent(enemy, new EnemyComponent(type, speed));
        world.SetComponent(enemy, new TransformComponent { Position = spawnPos });
        world.SetComponent(enemy, new VelocityComponent { Value = Vector2.Zero });
        world.SetComponent(enemy, new DrawColorComponent(Color.Black));
        world.SetComponent(enemy, new RigidBodyComponent
        {
            Shape = RigidBodyShape.Circle,
            BoundingRadius = 20f,
            Mass = 1.2f,
            Restitution = 0.3f,
            Friction = 0.9f,
            CollisionGroup = 2, // Shadow enemies collision group
            CollisionMask = 1 | 4 // collides with walls (1) and player (4)
        });
    }

    public void Draw(World world, FrameContext frameContext) { }
}
