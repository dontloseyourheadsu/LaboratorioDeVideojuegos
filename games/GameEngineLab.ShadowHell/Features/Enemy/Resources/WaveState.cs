using System;

namespace GameEngineLab.ShadowHell.Features.Enemy.Resources;

public sealed class WaveState
{
    public int CurrentWave { get; set; } = 0;
    public int EnemiesRemaining { get; set; } = 0;
    public int EnemiesToSpawn { get; set; } = 0;
    public float SpawnCooldownTimer { get; set; } = 0f;
    public float SpawnCooldown { get; set; } = 1.0f;
    public float NextWaveTimer { get; set; } = 0f;
    public bool IsWaveActive { get; set; } = false;
    public int TotalWaveEnemies { get; set; } = 0;
}
