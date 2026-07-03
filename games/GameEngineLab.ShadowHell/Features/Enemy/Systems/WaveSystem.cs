using GameEngineLab.Core.Features.Ecs.Resources;
using GameEngineLab.Core.Features.Ecs.Systems;
using GameEngineLab.Core.Features.Ecs.Entities;
using GameEngineLab.ShadowHell.Features.Enemy.Resources;
using System;

namespace GameEngineLab.ShadowHell.Features.Enemy.Systems;

public sealed class WaveSystem : IGameSystem
{
    public int Order => 99; // Runs before EnemySystem/physics

    public void Update(World world, FrameContext frameContext)
    {
        // Stub for now, to be implemented in Commit 10
    }

    public void Draw(World world, FrameContext frameContext) { }
}
