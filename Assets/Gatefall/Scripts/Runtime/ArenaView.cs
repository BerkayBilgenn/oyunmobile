using System.Collections.Generic;
using Gatefall.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Gatefall
{
    public sealed class ArenaView : MonoBehaviour
    {
        public Camera GameCamera { get; private set; }
        private Transform cannon, barrel, actorRoot;
        private readonly Dictionary<int, ActorView> enemyViews = new Dictionary<int, ActorView>();
        private readonly Dictionary<int, ActorView> shotViews = new Dictionary<int, ActorView>();
        private readonly Stack<ActorView> robotPool = new Stack<ActorView>();
        private readonly Stack<ActorView> bossPool = new Stack<ActorView>();
        private readonly Stack<ActorView> scoutPool = new Stack<ActorView>();
        private readonly HashSet<int> seen = new HashSet<int>();
        private readonly List<int> stale = new List<int>();
        private readonly List<Material> materials = new List<Material>();
        private readonly Transform[] gateRoots = new Transform[2];
        private Material blue, cyan, coral, dark, white, purple, orange;
        private float recoil, shake, hitTextCooldown, visualClock;
        private Vector3 cameraPosition;
        private readonly List<ActorView> preview = new List<ActorView>();

        public void Build()
        {
            blue = Mat("Scout blue", new Color32(20, 128, 232, 255));
            cyan = Mat("Energy", new Color32(86, 241, 252, 255), true);
            coral = Mat("Raider coral", new Color32(242, 74, 78, 255));
            dark = Mat("Graphite", new Color32(36, 49, 66, 255));
            white = Mat("Ivory", new Color32(249, 237, 214, 255));
            purple = Mat("Multiplier", new Color32(175, 75, 235, 255), true);
            orange = Mat("Titan gold", new Color32(255, 183, 68, 255), true);
            var sand = Mat("Sand", new Color32(220, 183, 140, 255));
            var road = Mat("Road", new Color32(74, 83, 101, 255));
            var stone = Mat("Sandstone", new Color32(193, 137, 105, 255));
            var shadowStone = Mat("Sandstone shade", new Color32(164, 108, 88, 255));

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.68f, .72f, .8f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color32(234, 205, 170, 255);
            RenderSettings.fogStartDistance = 38; RenderSettings.fogEndDistance = 100;
            QualitySettings.shadowDistance = 48; QualitySettings.antiAliasing = 4;
            var sun = new GameObject("Late afternoon sun").AddComponent<Light>();
            sun.type = LightType.Directional; sun.intensity = 1.25f;
            sun.color = new Color(1, .91f, .78f); sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(48, -28, 0);
            sun.transform.SetParent(transform, false);

            var cameraObject = new GameObject("Main Camera"); cameraObject.tag = "MainCamera";
            GameCamera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            GameCamera.transform.SetParent(transform, false);
            cameraPosition = new Vector3(0, 17, -18);
            GameCamera.transform.position = cameraPosition;
            GameCamera.transform.LookAt(new Vector3(0, 1.2f, 9));
            GameCamera.fieldOfView = 48; GameCamera.nearClipPlane = .3f; GameCamera.farClipPlane = 180;
            GameCamera.clearFlags = CameraClearFlags.SolidColor; GameCamera.backgroundColor = RenderSettings.fogColor;

            Part("Desert", PrimitiveType.Cube, new Vector3(0, -.6f, 30), new Vector3(180, .4f, 220), sand);
            Part("Arena foundation", PrimitiveType.Cube, new Vector3(0, -.22f, 13), new Vector3(9.2f, .48f, 37), dark);
            Part("Road", PrimitiveType.Cube, new Vector3(0, .02f, 13), new Vector3(8.2f, .12f, 36), road);
            for (int side = -1; side <= 1; side += 2)
            {
                Part("Concrete wall", PrimitiveType.Cube, new Vector3(side * 4.35f, .23f, 13), new Vector3(.34f, .6f, 37), white);
                Part("Blue rail", PrimitiveType.Cube, new Vector3(side * 4.35f, .57f, 13), new Vector3(.14f, .08f, 37), blue);
                for (int z = -3; z < 31; z += 3)
                {
                    Part("Wall joint", PrimitiveType.Cube, new Vector3(side * 4.35f, .25f, z), new Vector3(.43f, .65f, .13f), dark);
                    Part("Wall light", PrimitiveType.Cube, new Vector3(side * 4.12f, .37f, z), new Vector3(.03f, .13f, .38f), cyan);
                }
            }
            for (int z = -1; z < 30; z += 3)
                for (int side = -1; side <= 1; side += 2)
                    Part("Lane dash", PrimitiveType.Cube, new Vector3(side * 1.35f, .095f, z), new Vector3(.045f, .015f, 1.1f), white);
            for (int i = -4; i <= 4; i++)
            {
                var stripe = Part("Defense marker", PrimitiveType.Cube, new Vector3(i * .9f, .105f, -1.6f), new Vector3(.48f, .02f, .25f), cyan);
                stripe.localRotation = Quaternion.Euler(0, 25, 0);
            }

            var rng = new System.Random(78);
            for (int i = 0; i < 32; i++)
            {
                float side = i % 2 == 0 ? -1 : 1;
                float x = side * (8 + (float)rng.NextDouble() * 20);
                float z = -6 + i * 2.6f;
                float width = 3 + (float)rng.NextDouble() * 7;
                float height = 2 + (float)rng.NextDouble() * 9;
                Mesa(new Vector3(x, -.5f, z), width, height, i % 3 == 0 ? shadowStone : stone);
                if (i % 3 == 0)
                    Part("Desert boulder", PrimitiveType.Sphere, new Vector3(side * 6.4f, .15f, z), new Vector3(1.3f, .8f, 1), stone);
            }

            cannon = new GameObject("Cannon").transform; cannon.SetParent(transform, false);
            ActorView.Part("Energy platform", PrimitiveType.Cylinder, cannon, new Vector3(0, .13f, 0), new Vector3(1.6f, .025f, 1.6f), cyan);
            ActorView.Part("Platform inset", PrimitiveType.Cylinder, cannon, new Vector3(0, .17f, 0), new Vector3(1.42f, .025f, 1.42f), dark);
            ActorView.Part("Chassis", PrimitiveType.Cube, cannon, new Vector3(0, .4f, 0), new Vector3(.9f, .45f, 1), blue);
            for (int side = -1; side <= 1; side += 2)
                for (int end = -1; end <= 1; end += 2)
                {
                    var wheel = ActorView.Part("Wheel", PrimitiveType.Cylinder, cannon, new Vector3(side * .49f, .33f, end * .32f), new Vector3(.42f, .12f, .42f), dark);
                    wheel.localRotation = Quaternion.Euler(0, 0, 90);
                }
            barrel = new GameObject("Turret").transform; barrel.SetParent(cannon, false);
            ActorView.Part("Turret housing", PrimitiveType.Sphere, barrel, new Vector3(0, .82f, .02f), new Vector3(.78f, .65f, .74f), blue);
            var tube = ActorView.Part("Barrel", PrimitiveType.Cylinder, barrel, new Vector3(0, .86f, .58f), new Vector3(.31f, .55f, .31f), blue);
            tube.localRotation = Quaternion.Euler(90, 0, 0);
            var muzzle = ActorView.Part("Muzzle", PrimitiveType.Cylinder, barrel, new Vector3(0, .86f, 1.13f), new Vector3(.24f, .04f, .24f), cyan);
            muzzle.localRotation = Quaternion.Euler(90, 0, 0);
            ActorView.Part("Sight", PrimitiveType.Cube, barrel, new Vector3(0, 1.17f, .06f), new Vector3(.16f, .1f, .22f), cyan);

            for (int i = 0; i < 2; i++)
            {
                var root = new GameObject("Multiplier " + i).transform; root.SetParent(transform, false);
                gateRoots[i] = root;
                for (int side = -1; side <= 1; side += 2)
                {
                    ActorView.Part("Pylon", PrimitiveType.Cube, root, new Vector3(side * 1.325f, .93f, 0), new Vector3(.15f, 1.8f, .2f), purple);
                    ActorView.Part("Pylon foot", PrimitiveType.Cube, root, new Vector3(side * 1.325f, .15f, 0), new Vector3(.4f, .23f, .5f), dark);
                }
                ActorView.Part("Header", PrimitiveType.Cube, root, new Vector3(0, 1.93f, 0), new Vector3(2.8f, .58f, .19f), purple);
                ActorView.Part("Threshold", PrimitiveType.Cube, root, new Vector3(0, .12f, 0), new Vector3(2.7f, .06f, .4f), purple);
            }

            actorRoot = new GameObject("Pooled actors").transform; actorRoot.SetParent(transform, false);
            for (int i = 0; i < 9; i++)
            {
                var robot = ActorView.Robot(actorRoot, coral, dark, orange);
                robot.Root.transform.position = new Vector3((i % 3 - 1) * 1.5f, .15f, 13 + (i / 3) * 2.5f);
                robot.Id = i; preview.Add(robot);
            }
            FitCamera();
        }

        public void FitCamera()
        {
            const float portrait = 9f / 16f;
            float aspect = Screen.width / (float)Mathf.Max(1, Screen.height);
            if (aspect > portrait)
            { float width = portrait / aspect; GameCamera.rect = new Rect((1 - width) * .5f, 0, width, 1); }
            else GameCamera.rect = new Rect(0, 0, 1, 1);
        }

        public void Render(GameSession game, float dt)
        {
            bool playing = game.State == RunState.Playing;
            if (playing || game.State == RunState.Title) visualClock += dt;
            hitTextCooldown -= dt;
            recoil = Mathf.MoveTowards(recoil, 0, dt * 6);
            shake = Mathf.MoveTowards(shake, 0, dt * 1.5f);
            GameCamera.transform.position = cameraPosition + new Vector3(Mathf.Sin(Time.unscaledTime * 73) * shake * .08f, 0, 0);
            cannon.position = new Vector3(game.CannonX, 0, -.55f);
            barrel.localPosition = new Vector3(0, 0, -recoil * .13f);
            foreach (var actor in preview)
            {
                actor.Root.SetActive(game.State == RunState.Title);
                if (game.State == RunState.Title) actor.Animate(visualClock, 1.05f, false, true);
            }

            seen.Clear();
            foreach (Enemy enemy in game.Enemies)
            {
                seen.Add(enemy.Id);
                if (!enemyViews.TryGetValue(enemy.Id, out ActorView view))
                {
                    bool boss = enemy.Kind == EnemyKind.Boss;
                    Stack<ActorView> pool = boss ? bossPool : robotPool;
                    view = pool.Count > 0 ? pool.Pop() : ActorView.Robot(actorRoot, coral, dark, orange, boss);
                    view.Id = enemy.Id; view.BaseScale = Vector3.one * (boss ? 8.5f : enemy.Kind == EnemyKind.Tank ? 1.5f : enemy.Kind == EnemyKind.Runner ? .9f : 1.2f);
                    view.Root.SetActive(true); enemyViews.Add(enemy.Id, view);
                }
                view.Root.transform.position = new Vector3(enemy.X, .13f, enemy.Z);
                view.Flash = Mathf.MoveTowards(view.Flash, 0, dt * 6);
                view.Animate(visualClock, view.BaseScale.x, enemy.Kind == EnemyKind.Boss, playing);
            }
            stale.Clear();
            foreach (var pair in enemyViews) if (!seen.Contains(pair.Key)) stale.Add(pair.Key);
            foreach (int id in stale)
            {
                var view = enemyViews[id]; view.Root.SetActive(false);
                (view.BaseScale.x > 3 ? bossPool : robotPool).Push(view); enemyViews.Remove(id);
            }

            seen.Clear();
            foreach (Shot shot in game.Shots)
            {
                seen.Add(shot.Id);
                if (!shotViews.TryGetValue(shot.Id, out ActorView view))
                {
                    view = scoutPool.Count > 0 ? scoutPool.Pop() : ActorView.Scout(actorRoot, blue, cyan);
                    view.Id = shot.Id; view.Root.SetActive(true); shotViews.Add(shot.Id, view);
                    view.Root.transform.localScale = Vector3.one * 1.45f;
                }
                view.Root.transform.position = new Vector3(shot.X, .12f + Mathf.Sin(visualClock * 25 + shot.Id) * .04f, shot.Z);
            }
            stale.Clear();
            foreach (var pair in shotViews) if (!seen.Contains(pair.Key)) stale.Add(pair.Key);
            foreach (int id in stale)
            { var view = shotViews[id]; view.Root.SetActive(false); scoutPool.Push(view); shotViews.Remove(id); }

            for (int i = 0; i < 2; i++)
                gateRoots[i].position = new Vector3(game.Gates[i].X, 0, game.Gates[i].Z);
        }

        public void React(GameEvent message)
        {
            if (message.Kind == GameEventKind.Shot) recoil = 1;
            if (message.Kind == GameEventKind.BaseHit) shake = 1;
            if (message.Kind == GameEventKind.Hit && hitTextCooldown <= 0)
            {
                hitTextCooldown = .14f;
                foreach (var view in enemyViews.Values)
                    if (Mathf.Abs(view.Root.transform.position.z - message.Z) < 1) view.Flash = 1;
            }
        }

        public void ResetEffects() { recoil = 0; shake = 0; }

        private Transform Part(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        { return ActorView.Part(name, type, transform, position, scale, material); }

        private Material Mat(string name, Color color, bool emissive = false)
        {
            var material = new Material(Shader.Find("Standard")) { name = name, color = color, enableInstancing = true };
            material.SetFloat("_Glossiness", .18f);
            if (emissive) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color * .35f); }
            materials.Add(material); return material;
        }

        private void Mesa(Vector3 position, float width, float height, Material material)
        {
            var baseRock = Part("Canyon mesa", PrimitiveType.Cylinder, position + Vector3.up * height * .35f, new Vector3(width, height * .35f, width * .8f), material);
            baseRock.rotation = Quaternion.Euler(0, position.x * 7, 0);
            Part("Mesa crown", PrimitiveType.Cylinder, position + Vector3.up * height * .8f, new Vector3(width * .67f, height * .15f, width * .54f), material);
        }

        private void OnDestroy() { foreach (var material in materials) if (material != null) Destroy(material); }
    }
}
