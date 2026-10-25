using System;
using Gatefall.Core;

internal static class Program
{
    private static int failures;
    private static int passes;

    private static void Main()
    {
        Check("A new run resets score, upgrades, actors and health", Restart);
        Check("Cannon movement is clamped to the arena", Movement);
        Check("Pause freezes enemies, firing and run time", Pause);
        Check("Non-finite elapsed time cannot corrupt the simulation", InvalidTime);
        Check("Simulation consumes a large frame without skipping time", FrameTime);
        Check("The fifth wave is a boss and rewards only after its death", BossSchedule);
        Check("Crossing a gate multiplies shots once", GateMultiplication);
        Check("Fast projectiles still hit enemies between frames", SweptCollision);
        Check("An enemy before a gate stops a shot before it can multiply", EnemyBeforeGate);
        Check("Firing cadence matches the selected rate over one second", FireCadence);
        Check("A breached defense line removes health and ends the run", Breach);
        Check("An invalid upgrade cannot resume or modify the run", InvalidUpgrade);
        Check("An upgrade applies exactly once and resumes the next wave", Upgrade);
        Check("A full-health repair cannot exceed maximum health", Repair);
        Check("Population stays bounded during an extended run", Population);
        Check("Same seed and input give the same run", Determinism);
        Console.WriteLine("\n" + passes + " passed; " + failures + " failed.");
        Environment.ExitCode = failures == 0 ? 0 : 1;
    }

    private static void Check(string name, Action test)
    {
        try { test(); passes++; Console.WriteLine("PASS " + name); }
        catch (Exception error) { failures++; Console.WriteLine("FAIL " + name + ": " + error.Message); }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static void Advance(GameSession game, float seconds)
    {
        int count = (int)Math.Round(seconds * 60f);
        for (int i = 0; i < count; i++) game.Advance(1f / 60f);
    }

    private static GameSession NewGame(int seed = 23)
    {
        var game = new GameSession();
        game.Start(seed);
        return game;
    }

    private static void ClearWave(GameSession game)
    {
        game.FireRate = 12;
        game.Damage = 80;
        for (int i = 0; i < 18000 && game.State == RunState.Playing; i++)
        {
            Enemy nearest = null;
            foreach (var enemy in game.Enemies)
                if (nearest == null || enemy.Z < nearest.Z) nearest = enemy;
            if (nearest != null) game.Move(nearest.X);
            game.Advance(1f / 60f);
        }
        Assert(game.State == RunState.Upgrade, "Wave did not finish: " + game.State);
    }

    private static void Restart()
    {
        var game = NewGame();
        Advance(game, 1);
        game.Damage = 99;
        game.Start(12);
        Assert(game.State == RunState.Playing && game.Wave == 1, "Run did not restart");
        Assert(game.Score == 0 && game.Health == 6 && game.Damage == 1, "Old run values survived");
        Assert(game.Enemies.Count == 0 && game.Shots.Count == 0, "Actors survived restart");
    }

    private static void Movement()
    {
        var game = NewGame();
        game.Move(500);
        Assert(game.CannonX == 3.65f, "Cannon escaped right wall");
        game.Move(-500);
        Assert(game.CannonX == -3.65f, "Cannon escaped left wall");
        game.Move(float.NaN);
        Assert(!float.IsNaN(game.CannonX), "NaN input corrupted cannon");
    }

    private static void Pause()
    {
        var game = NewGame();
        Advance(game, 1);
        float z = game.Enemies[0].Z;
        float elapsed = game.Elapsed;
        int shots = game.Shots.Count;
        game.SetPaused(true);
        Advance(game, 8);
        Assert(game.Enemies[0].Z == z && game.Elapsed == elapsed && game.Shots.Count == shots,
            "Actors or time advanced while paused");
        game.SetPaused(false);
        Advance(game, 1);
        Assert(game.Enemies[0].Z < z, "Resume failed");
    }

    private static void InvalidTime()
    {
        var game = NewGame();
        game.Advance(float.NaN); game.Advance(float.PositiveInfinity); game.Advance(-1);
        Assert(game.Elapsed == 0 && game.Shots.Count == 0, "Invalid dt changed run");
    }

    private static void FrameTime()
    {
        var game = NewGame();
        game.Advance(.5f);
        Assert(Math.Abs(game.Elapsed - .5f) < .001f, "Frame time was lost");
    }

    private static void BossSchedule()
    {
        var game = NewGame();
        for (int wave = 1; wave < 5; wave++)
        {
            ClearWave(game);
            Assert(game.Wave == wave, "Wave number advanced before upgrade");
            Assert(game.ChooseUpgrade(0), "Choice refused");
        }
        Advance(game, .1f);
        bool found = false;
        foreach (var enemy in game.Enemies) if (enemy.Kind == EnemyKind.Boss) found = true;
        Assert(game.IsBossWave && found, "Fifth wave has no boss");
        ClearWave(game);
        Assert(game.Score >= 750, "Boss reward was not scored");
    }

    private static void GateMultiplication()
    {
        var game = NewGame();
        game.Move(game.Gates[1].X);
        Advance(game, .8f);
        Assert(game.MultipliedShots > 0, "Gate did not amplify a crossing shot");
        foreach (var shot in game.Shots)
            Assert(shot.Amplifications <= 1, "Shot multiplied recursively");
        Assert(game.Shots.Count < 40, "Unbounded recursive multiplication");
    }

    private static void SweptCollision()
    {
        var game = NewGame();
        game.ProjectileSpeed = 900;
        game.Damage = 100;
        Advance(game, .05f);
        Assert(game.Enemies.Count > 0, "No target spawned");
        game.Move(game.Enemies[0].X);
        int before = game.Score;
        Advance(game, .3f);
        Assert(game.Score > before, "Fast shot tunneled through target");
    }

    private static void Breach()
    {
        var game = NewGame();
        game.FireRate = 0;
        Advance(game, 240);
        Assert(game.State == RunState.GameOver && game.Health == 0, "Defense never ended");
        float elapsed = game.Elapsed;
        Advance(game, 1);
        Assert(game.Elapsed == elapsed, "Game over kept running");
    }

    private static void EnemyBeforeGate()
    {
        var game = NewGame();
        game.FireRate = 0;
        game.Advance(1f / 60f);
        Enemy target = game.Enemies[0];
        target.X = game.Gates[1].X; target.Z = 5.35f; target.Radius = .32f; target.Speed = 0;
        game.Move(target.X);
        game.FireRate = 1; game.ProjectileSpeed = 30;
        game.Advance(1f / 60f);
        Shot shot = game.Shots[0];
        shot.Z = 5; shot.X = target.X;
        game.Advance(1f / 60f);
        Assert(game.MultipliedShots == 0 && game.Shots.Count == 0, "A blocked shot produced units beyond the gate");
        Assert(target.Health == target.MaxHealth - 1, "The earlier target was not hit");
    }

    private static void FireCadence()
    {
        var game = NewGame(); game.FireRate = 18;
        game.Advance(1);
        int fired = 0;
        foreach (GameEvent message in game.Events) if (message.Kind == GameEventKind.Shot) fired++;
        Assert(fired == 18, "Expected 18 volleys in one second, saw " + fired);
    }

    private static void InvalidUpgrade()
    {
        var game = NewGame();
        Assert(!game.ChooseUpgrade(0), "Upgrade accepted outside selection");
        ClearWave(game);
        int wave = game.Wave;
        Assert(!game.ChooseUpgrade(-1) && !game.ChooseUpgrade(3), "Bad index accepted");
        Advance(game, 10);
        Assert(game.Wave == wave && game.State == RunState.Upgrade, "Selection did not freeze run");
    }

    private static void Upgrade()
    {
        var game = NewGame();
        ClearWave(game);
        int levels = game.UpgradeLevel;
        Assert(game.Choices.Count == 3, "Need three choices");
        Assert(game.Choices[0] != game.Choices[1] && game.Choices[1] != game.Choices[2]
            && game.Choices[0] != game.Choices[2], "Duplicate choices");
        Assert(game.ChooseUpgrade(0), "Valid upgrade rejected");
        Assert(game.Wave == 2 && game.State == RunState.Playing, "No next wave");
        Assert(!game.ChooseUpgrade(0) && game.UpgradeLevel == levels + 1, "Double choice applied");
        Assert(game.Shots.Count == 0, "Projectiles leaked across upgrade");
    }

    private static void Repair()
    {
        var game = NewGame();
        for (int wave = 0; wave < 6; wave++)
        {
            ClearWave(game);
            int index = 0;
            for (int i = 0; i < game.Choices.Count; i++)
                if (game.Choices[i] == UpgradeKind.Repair) index = i;
            game.ChooseUpgrade(index);
            Assert(game.Health <= game.MaxHealth, "Repair exceeded maximum");
        }
    }

    private static void Population()
    {
        var game = NewGame();
        game.FireRate = 10000;
        game.MultiShot = 1000;
        for (int i = 0; i < 18000; i++)
        {
            game.Advance(1f / 60f);
            Assert(game.Shots.Count <= 240, "Projectile pool exceeded cap");
            Assert(game.Enemies.Count <= 64, "Enemy cap exceeded");
            if (game.State == RunState.Upgrade) game.ChooseUpgrade(0);
            if (game.State == RunState.GameOver) game.Start(23);
        }
    }

    private static void Determinism()
    {
        var a = NewGame(); var b = NewGame();
        Advance(a, 3); Advance(b, 3);
        Assert(a.Enemies.Count == b.Enemies.Count && a.Shots.Count == b.Shots.Count, "Different populations");
        for (int i = 0; i < a.Enemies.Count; i++)
            Assert(a.Enemies[i].X == b.Enemies[i].X && a.Enemies[i].Z == b.Enemies[i].Z,
                "Seed did not reproduce positions");
    }
}
