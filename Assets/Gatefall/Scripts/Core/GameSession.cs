using System;
using System.Collections.Generic;

namespace Gatefall.Core
{
    public enum RunState { Title, Playing, Paused, Upgrade, GameOver }
    public enum EnemyKind { Grunt, Runner, Tank, Boss }
    public enum UpgradeKind { FireRate, Damage, MultiShot, Repair, Shield, Velocity }
    public enum GameEventKind { Shot, Hit, Eliminated, BaseHit, WaveCleared, BossSpawned, GateCrossed, GameOver }

    public sealed class Enemy
    {
        public int Id;
        public EnemyKind Kind;
        public float X, Z, Health, MaxHealth, Speed, Radius, AttackTimer;
    }

    public sealed class Shot
    {
        public int Id, Amplifications;
        public float X, Z, PreviousZ, Damage;
    }

    public sealed class Gate
    {
        public int Id, Multiplier;
        public float X, Z, Width;
    }

    public struct GameEvent
    {
        public GameEventKind Kind;
        public float X, Z;
        public int Value;
        public GameEvent(GameEventKind kind, float x, float z, int value = 0)
        { Kind = kind; X = x; Z = z; Value = value; }
    }

    /// <summary>Deterministic gameplay model. Coordinates use X across the lane and +Z toward enemies.</summary>
    public sealed class GameSession
    {
        public const int MaxShots = 240;
        public const int MaxEnemies = 64;
        public const float LaneLimit = 3.65f;
        private readonly List<Enemy> enemies = new List<Enemy>(64);
        private readonly List<Shot> shots = new List<Shot>(240);
        private readonly List<Gate> gates = new List<Gate>(2);
        private readonly List<UpgradeKind> choices = new List<UpgradeKind>(3);
        private readonly List<GameEvent> events = new List<GameEvent>(256);
        private Random random = new Random(1);
        private float spawnTimer, fireTimer, waveTime;
        private int spawned, spawnBudget, nextId;

        public RunState State { get; private set; } = RunState.Title;
        public int Wave { get; private set; }
        public int Score { get; private set; }
        public int Kills { get; private set; }
        public int Health { get; private set; } = 6;
        public int MaxHealth { get; private set; } = 6;
        public int Shield { get; private set; }
        public int UpgradeLevel { get; private set; }
        public int MultipliedShots { get; private set; }
        public float CannonX { get; private set; }
        public float Elapsed { get; private set; }
        public float FireRate = 5f;
        public float Damage = 1f;
        public float ProjectileSpeed = 16f;
        public int MultiShot = 1;
        public bool IsBossWave { get { return Wave > 0 && Wave % 5 == 0; } }
        public float WaveProgress { get { return spawnBudget == 0 ? 0 : Math.Min(1f, (spawned - enemies.Count) / (float)spawnBudget); } }
        public IReadOnlyList<Enemy> Enemies { get; }
        public IReadOnlyList<Shot> Shots { get; }
        public IReadOnlyList<Gate> Gates { get; }
        public IReadOnlyList<UpgradeKind> Choices { get; }
        public IReadOnlyList<GameEvent> Events { get; }

        public GameSession()
        {
            Enemies = enemies.AsReadOnly(); Shots = shots.AsReadOnly();
            Gates = gates.AsReadOnly(); Choices = choices.AsReadOnly(); Events = events.AsReadOnly();
            ResetGates();
        }

        public void Start(int seed)
        {
            random = new Random(seed);
            enemies.Clear(); shots.Clear(); choices.Clear(); events.Clear();
            Wave = 1; Score = 0; Kills = 0; Health = 6; MaxHealth = 6; Shield = 0;
            UpgradeLevel = 0; MultipliedShots = 0; CannonX = 0; Elapsed = 0; nextId = 0;
            FireRate = 5; Damage = 1; ProjectileSpeed = 16; MultiShot = 1;
            ResetGates(); BeginWave();
        }

        private void ResetGates()
        {
            gates.Clear();
            gates.Add(new Gate { Id = 0, X = -2, Z = 5.4f, Width = 2.65f, Multiplier = 2 });
            gates.Add(new Gate { Id = 1, X = 2, Z = 5.4f, Width = 2.65f, Multiplier = 3 });
        }

        private void BeginWave()
        {
            State = RunState.Playing;
            spawnTimer = 0; fireTimer = 0; waveTime = 0; spawned = 0;
            spawnBudget = IsBossWave ? 1 : Math.Min(38, 8 + Wave * 2);
            shots.Clear(); choices.Clear();
            gates[0].Multiplier = Wave >= 8 ? 3 : 2;
            gates[1].Multiplier = Wave >= 8 ? 4 : 3;
        }

        public void Move(float x)
        {
            if (State != RunState.Playing || !Finite(x)) return;
            CannonX = Clamp(x, -LaneLimit, LaneLimit);
        }

        public void SetPaused(bool paused)
        {
            if (paused && State == RunState.Playing) State = RunState.Paused;
            else if (!paused && State == RunState.Paused) State = RunState.Playing;
        }

        public void Advance(float dt)
        {
            if (!Finite(dt) || dt <= 0 || State != RunState.Playing) return;
            // Substeps preserve crossings and firing cadence during slower frames.
            float remaining = Math.Min(dt, 1f);
            while (remaining > .000001f && State == RunState.Playing)
            {
                float step = Math.Min(remaining, 1f / 60f);
                Tick(step); remaining -= step;
            }
        }

        private void Tick(float dt)
        {
            Elapsed += dt; waveTime += dt;
            gates[0].X = -1.95f + (float)Math.Sin(waveTime * .55f) * .28f;
            gates[1].X = 1.95f + (float)Math.Sin(waveTime * .55f + .6f) * .28f;
            spawnTimer -= dt;
            if (spawned < spawnBudget && spawnTimer <= 0 && enemies.Count < MaxEnemies)
            {
                SpawnEnemy();
                spawned++;
                spawnTimer += Math.Max(.22f, .85f - Wave * .022f);
            }

            float rate = Finite(FireRate) ? Clamp(FireRate, 0, 18) : 0;
            if (rate > 0 && fireTimer <= 0)
            {
                int count = Math.Max(1, Math.Min(5, MultiShot));
                for (int i = 0; i < count; i++) AddShot(CannonX + (i - (count - 1) * .5f) * .22f, .7f, 0);
                fireTimer += 1f / rate;
                Emit(GameEventKind.Shot, CannonX, .7f);
            }
            else if (rate == 0) fireTimer = 0;
            if (rate > 0) fireTimer -= dt;

            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                Enemy enemy = enemies[i];
                if (enemy.Kind == EnemyKind.Boss && enemy.Z <= 13f)
                {
                    enemy.AttackTimer -= dt;
                    enemy.X = (float)Math.Sin(waveTime * .65f) * 1.7f;
                    if (enemy.AttackTimer <= 0)
                    { enemy.AttackTimer = 7; DamageBase(1, enemy.X, enemy.Z); }
                }
                else enemy.Z -= enemy.Speed * dt;
                if (enemy.Z < -1f)
                {
                    enemies.RemoveAt(i);
                    DamageBase(enemy.Kind == EnemyKind.Tank ? 2 : 1, enemy.X, enemy.Z);
                }
                if (State != RunState.Playing) return;
            }

            // Newly amplified children wait until the next tick. Every shot may cross only one gate.
            int countBeforeGates = shots.Count;
            for (int i = countBeforeGates - 1; i >= 0; i--)
            {
                Shot shot = shots[i];
                shot.PreviousZ = shot.Z;
                float speed = Finite(ProjectileSpeed) ? Clamp(ProjectileSpeed, 1, 1000) : 16;
                shot.Z += speed * dt;
                int hit = -1; float nearestContact = float.MaxValue;
                for (int j = 0; j < enemies.Count; j++)
                {
                    Enemy enemy = enemies[j];
                    if (Math.Abs(enemy.X - shot.X) > enemy.Radius + .12f) continue;
                    float contact = Math.Max(shot.PreviousZ, enemy.Z - enemy.Radius);
                    if (shot.PreviousZ <= enemy.Z + enemy.Radius && shot.Z >= contact && contact < nearestContact)
                    { hit = j; nearestContact = contact; }
                }
                if (shot.Amplifications == 0)
                {
                    foreach (Gate gate in gates)
                    {
                        if (shot.PreviousZ < gate.Z && shot.Z >= gate.Z && gate.Z < nearestContact
                            && Math.Abs(shot.X - gate.X) <= gate.Width * .5f)
                        {
                            shot.Amplifications = 1;
                            for (int n = 1; n < gate.Multiplier; n++)
                            {
                                float offset = (n % 2 == 0 ? -1 : 1) * ((n + 1) / 2) * .17f;
                                AddShot(Clamp(shot.X + offset, -LaneLimit, LaneLimit), gate.Z + .015f, 1, shot.Damage);
                            }
                            MultipliedShots = SaturatingAdd(MultipliedShots, gate.Multiplier - 1);
                            Emit(GameEventKind.GateCrossed, shot.X, gate.Z, gate.Multiplier);
                            break;
                        }
                    }
                }

                if (hit >= 0)
                {
                    Enemy enemy = enemies[hit];
                    enemy.Health -= shot.Damage;
                    Emit(GameEventKind.Hit, shot.X, enemy.Z, (int)Math.Ceiling(shot.Damage));
                    shots.RemoveAt(i);
                    if (enemy.Health <= 0)
                    {
                        int reward = enemy.Kind == EnemyKind.Boss ? 500 + Math.Min(Wave, 100000) * 50
                            : enemy.Kind == EnemyKind.Tank ? 35 : enemy.Kind == EnemyKind.Runner ? 15 : 10;
                        Score = SaturatingAdd(Score, reward); Kills = SaturatingAdd(Kills, 1);
                        Emit(GameEventKind.Eliminated, enemy.X, enemy.Z, reward);
                        enemies.RemoveAt(hit);
                    }
                }
                else if (shot.Z > 30) shots.RemoveAt(i);
            }

            if (spawned == spawnBudget && enemies.Count == 0)
            {
                Score = SaturatingAdd(Score, 50 + Math.Min(Wave, 100000) * 10);
                State = RunState.Upgrade;
                shots.Clear();
                PopulateChoices();
                Emit(GameEventKind.WaveCleared, 0, 0, Wave);
            }
        }

        private void SpawnEnemy()
        {
            EnemyKind kind = IsBossWave ? EnemyKind.Boss
                : Wave >= 3 && spawned % 6 == 4 ? EnemyKind.Tank
                : Wave >= 2 && spawned % 4 == 2 ? EnemyKind.Runner : EnemyKind.Grunt;
            float health = kind == EnemyKind.Boss ? 140 + Math.Min(Wave, 10000) * 22
                : (kind == EnemyKind.Tank ? 9 : kind == EnemyKind.Runner ? 2 : 3) + (Wave - 1) * .55f;
            var enemy = new Enemy
            {
                Id = ++nextId, Kind = kind, X = kind == EnemyKind.Boss ? 0 : ((float)random.NextDouble() * 6.3f - 3.15f),
                Z = kind == EnemyKind.Boss ? 24 : 22 + (float)random.NextDouble() * 2,
                Health = health, MaxHealth = health, AttackTimer = 5,
                Speed = kind == EnemyKind.Boss ? .65f : Math.Min(2.8f, .95f + Wave * .045f) * (kind == EnemyKind.Runner ? 1.65f : kind == EnemyKind.Tank ? .65f : 1),
                Radius = kind == EnemyKind.Boss ? 1.5f : kind == EnemyKind.Tank ? .52f : .32f
            };
            enemies.Add(enemy);
            if (kind == EnemyKind.Boss) Emit(GameEventKind.BossSpawned, enemy.X, enemy.Z, Wave);
        }

        private void AddShot(float x, float z, int amplified, float inheritedDamage = -1)
        {
            if (shots.Count >= MaxShots) return;
            float damage = inheritedDamage >= 0 ? inheritedDamage : (Finite(Damage) ? Clamp(Damage, .1f, 100000) : 1);
            shots.Add(new Shot { Id = ++nextId, X = Clamp(x, -LaneLimit, LaneLimit), Z = z, PreviousZ = z, Damage = damage, Amplifications = amplified });
        }

        private void DamageBase(int damage, float x, float z)
        {
            int absorbed = Math.Min(Shield, damage);
            Shield -= absorbed; damage -= absorbed;
            Health = Math.Max(0, Health - damage);
            Emit(GameEventKind.BaseHit, x, z, damage);
            if (Health == 0)
            {
                State = RunState.GameOver;
                Emit(GameEventKind.GameOver, CannonX, 0, Score);
            }
        }

        private void PopulateChoices()
        {
            choices.Clear();
            var pool = new List<UpgradeKind> { UpgradeKind.FireRate, UpgradeKind.Damage, UpgradeKind.MultiShot, UpgradeKind.Repair, UpgradeKind.Shield, UpgradeKind.Velocity };
            // Keep upgrades useful at their caps.
            if (MultiShot >= 5) pool.Remove(UpgradeKind.MultiShot);
            if (FireRate >= 17.9f) pool.Remove(UpgradeKind.FireRate);
            if (ProjectileSpeed >= 29.9f) pool.Remove(UpgradeKind.Velocity);
            for (int i = 0; i < 3; i++)
            {
                int index = random.Next(pool.Count);
                choices.Add(pool[index]); pool.RemoveAt(index);
            }
        }

        public bool ChooseUpgrade(int index)
        {
            if (State != RunState.Upgrade || index < 0 || index >= choices.Count) return false;
            switch (choices[index])
            {
                case UpgradeKind.FireRate: FireRate = Math.Min(18, FireRate * 1.2f); break;
                case UpgradeKind.Damage: Damage = Math.Min(100000, Damage * 1.3f); break;
                case UpgradeKind.MultiShot: MultiShot = Math.Min(5, MultiShot + 1); break;
                case UpgradeKind.Repair: Health = Math.Min(MaxHealth, Health + 2); Damage = Math.Min(100000, Damage + .2f); break;
                case UpgradeKind.Shield: Shield = Math.Min(12, Shield + 2); break;
                case UpgradeKind.Velocity: ProjectileSpeed = Math.Min(30, ProjectileSpeed * 1.15f); Damage = Math.Min(100000, Damage * 1.1f); break;
            }
            UpgradeLevel = SaturatingAdd(UpgradeLevel, 1);
            Wave = Math.Min(100000, Wave + 1);
            BeginWave(); return true;
        }

        public void ClearEvents() { events.Clear(); }
        private void Emit(GameEventKind kind, float x, float z, int value = 0)
        {
            if (events.Count >= 256) events.RemoveAt(0);
            events.Add(new GameEvent(kind, x, z, value));
        }
        private static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
        private static float Clamp(float value, float min, float max) { return Math.Max(min, Math.Min(max, value)); }
        private static int SaturatingAdd(int a, int b) { return (int)Math.Min(int.MaxValue, (long)a + b); }
    }
}
