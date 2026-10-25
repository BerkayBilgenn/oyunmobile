#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Gatefall.Core;
using UnityEngine;

namespace Gatefall
{
    /// <summary>Only runs when explicitly requested by the editor smoke-test command.</summary>
    public sealed class SmokeProbe : MonoBehaviour
    {
        private readonly List<string> errors = new List<string>();
        private string destination;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Attach()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-gatefall-smoke") >= 0)
                new GameObject("Runtime verification").AddComponent<SmokeProbe>();
        }

        private void Awake() { Application.logMessageReceived += Log; }
        private void Log(string message, string trace, LogType type)
        { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message); }

        private IEnumerator Start()
        {
            destination = Path.GetFullPath("Evidence"); Directory.CreateDirectory(destination);
            yield return null;
            var root = FindFirstObjectByType<GameRoot>();
            if (root == null) { errors.Add("Runtime root missing"); Finish(); yield break; }
            root.Session.SetPaused(false);
            yield return new WaitForSecondsRealtime(.5f);
            var startButton = root.Hud.GetComponentsInChildren<UnityEngine.UI.Button>(true);
            foreach (var button in startButton)
                if (button.name == "Başlat" && !root.Hud.BlocksPointer(RectTransformUtility.WorldToScreenPoint(null, button.transform.position), 0))
                    errors.Add("A current-frame menu touch was not blocked");
            yield return Capture("01-title.png");
            root.StartRun();
            for (int i = 0; i < 180; i++) yield return null;
            if (root.Session.Elapsed <= 0 || root.Session.Shots.Count == 0) errors.Add("Live simulation did not start");
            foreach (var button in startButton)
                if (button.name == "Duraklat" && !root.Hud.BlocksPointer(RectTransformUtility.WorldToScreenPoint(null, button.transform.position), 0))
                    errors.Add("A current-frame pause touch was not blocked");
            yield return Capture("02-playing.png");
            root.Session.SetPaused(true);
            float elapsed = root.Session.Elapsed;
            yield return new WaitForSecondsRealtime(.2f);
            if (root.Session.Elapsed != elapsed) errors.Add("Pause allowed simulation to advance");
            yield return Capture("03-pause.png");
            root.Session.SetPaused(false);
            for (int wave = 1; wave < 5; wave++)
            {
                root.Session.Damage = 100; root.Session.FireRate = 18;
                for (int step = 0; step < 1800 && root.Session.State == RunState.Playing; step++)
                {
                    Enemy nearest = null;
                    foreach (Enemy enemy in root.Session.Enemies)
                        if (nearest == null || enemy.Z < nearest.Z) nearest = enemy;
                    if (nearest != null) root.Session.Move(nearest.X);
                    root.Session.Advance(1f / 30f);
                    root.Session.ClearEvents();
                }
                if (root.Session.State != RunState.Upgrade) { errors.Add("Upgrade screen unreachable at wave " + wave); break; }
                yield return null;
                if (wave == 1) yield return Capture("04-upgrade.png");
                root.ChooseUpgrade(0);
            }
            root.Session.Advance(.05f);
            root.Session.SetPaused(true);
            yield return null;
            // Show the boss without a pause overlay, then capture after the view has synchronized.
            root.Session.SetPaused(false);
            yield return null;
            if (!root.Session.IsBossWave) errors.Add("Boss wave not reached");
            yield return Capture("05-boss.png");
            root.Session.FireRate = 0;
            for (int i = 0; i < 400 && root.Session.State == RunState.Playing; i++) root.Session.Advance(1);
            if (root.Session.State != RunState.GameOver) errors.Add("Game over unreachable");
            yield return null;
            yield return Capture("06-game-over.png");
            root.StartRun();
            if (root.Session.Wave != 1 || root.Session.Score != 0 || root.Session.Damage != 1) errors.Add("Retry did not reset run");
            Finish();
        }

        private IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            Texture2D picture = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(destination, name), picture.EncodeToPNG());
            Destroy(picture);
        }

        private void Finish()
        {
            string message = errors.Count == 0 ? "PASS: boot, firing, pause, upgrade, boss, game over, retry; no runtime errors."
                : "FAIL:\n" + string.Join("\n", errors);
            File.WriteAllText(Path.Combine(destination, "runtime-verification.txt"), message);
            Debug.Log(message);
            UnityEditor.EditorApplication.Exit(errors.Count == 0 ? 0 : 1);
        }

        private void OnDestroy() { Application.logMessageReceived -= Log; }
    }
}
#endif
