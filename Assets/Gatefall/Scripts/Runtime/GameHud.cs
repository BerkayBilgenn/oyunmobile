using Gatefall.Core;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gatefall
{
    public sealed class GameHud : MonoBehaviour
    {
        private static readonly Color Ink = new Color32(22, 35, 55, 255);
        private static readonly Color Muted = new Color32(164, 181, 199, 255);
        private static readonly Color Cyan = new Color32(114, 241, 231, 255);
        private static readonly Color Cream = new Color32(255, 248, 231, 255);
        private static readonly Color Coral = new Color32(255, 117, 112, 255);
        private GameRoot game;
        private Canvas canvas;
        private RectTransform viewport, safe;
        private Font font;
        private Sprite rounded;
        private Texture2D roundTexture;
        private GameObject live, title, pause, upgrade, gameOver, bossPanel;
        private Text wave, score, health, stats, hint, bossHealth, titleBest, resultScore, resultDetail, record;
        private Text pauseSound, titleSound, upgradeSubtitle;
        private Image progress, bossFill;
        private readonly Text[] choiceTitle = new Text[3];
        private readonly Text[] choiceDetail = new Text[3];
        private readonly Button[] choiceButtons = new Button[3];
        private Button startButton, resumeButton, retryButton;
        private RunState renderedState = (RunState)(-1);
        private Rect lastSafe, lastCamera;
        private Vector2 lastScreen;
        private readonly List<RaycastResult> pointerHits = new List<RaycastResult>();

        public void Build(GameRoot owner)
        {
            game = owner;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            rounded = RoundedSprite();
            canvas = gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            gameObject.AddComponent<GraphicRaycaster>();
            if (EventSystem.current == null)
            {
                var system = new GameObject("Event System", typeof(EventSystem), typeof(StandaloneInputModule));
                system.transform.SetParent(transform, false);
            }

            viewport = Rect("Portrait viewport", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            safe = Rect("Safe area", viewport, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            BuildLive(); BuildTitle(); BuildPause(); BuildUpgrade(); BuildGameOver();
            Render();
        }

        private void BuildLive()
        {
            live = Rect("Live HUD", safe, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            var top = Box("Top bar", live.transform, new Vector2(.5f, 1), new Vector2(0, -53), new Vector2(492, 74), Ink);
            wave = Label("DALGA 01", top, new Vector2(0, .5f), new Vector2(20, 8), new Vector2(130, 28), 20, Cream, TextAnchor.MiddleLeft, true);
            Label("SONSUZ KUŞATMA", top, new Vector2(0, .5f), new Vector2(20, -18), new Vector2(160, 16), 10, Muted, TextAnchor.MiddleLeft);
            score = Label("0", top, new Vector2(.68f, .5f), new Vector2(0, 6), new Vector2(112, 30), 25, Cyan, TextAnchor.MiddleRight, true);
            Label("SKOR", top, new Vector2(.68f, .5f), new Vector2(0, -19), new Vector2(112, 14), 10, Muted, TextAnchor.MiddleRight);
            Button pauseButton = Button("Duraklat", top, new Vector2(1, .5f), new Vector2(-34, 0), new Vector2(48, 48), new Color32(48, 62, 81, 255), "Ⅱ", 22, Cream, game.TogglePause);
            pauseButton.navigation = new Navigation { mode = Navigation.Mode.None };
            var healthBar = Box("Defense", live.transform, new Vector2(0, 1), new Vector2(24, -110), new Vector2(210, 35), new Color(Ink.r, Ink.g, Ink.b, .88f));
            healthBar.pivot = new Vector2(0, .5f);
            health = Label("SAVUNMA  ● ● ● ● ● ●", healthBar, new Vector2(.5f, .5f), Vector2.zero, new Vector2(194, 25), 12, Cream, TextAnchor.MiddleCenter, true);
            var rail = Box("Wave rail", live.transform, new Vector2(.5f, 1), new Vector2(0, -141), new Vector2(492, 5), new Color(Ink.r, Ink.g, Ink.b, .3f));
            progress = Fill("Progress", rail, Cyan);

            bossPanel = Box("Boss health", live.transform, new Vector2(.5f, 1), new Vector2(0, -184), new Vector2(376, 58), Ink).gameObject;
            bossHealth = Label("TİTAN", bossPanel.transform, new Vector2(.5f, .5f), new Vector2(0, 11), new Vector2(340, 21), 13, Coral, TextAnchor.MiddleCenter, true);
            var bossRail = Box("Boss rail", bossPanel.transform, new Vector2(.5f, .5f), new Vector2(0, -12), new Vector2(336, 7), new Color32(58, 64, 79, 255));
            bossFill = Fill("Boss health fill", bossRail, Coral);

            var bottom = Box("Loadout", live.transform, new Vector2(.5f, 0), new Vector2(0, 55), new Vector2(492, 56), Ink);
            stats = Label("", bottom, new Vector2(.5f, .5f), Vector2.zero, new Vector2(460, 42), 13, Cream, TextAnchor.MiddleCenter, true);
            hint = Label("ATEŞ OTOMATİK · HAREKET İÇİN SÜRÜKLE", live.transform, new Vector2(.5f, 0), new Vector2(0, 108), new Vector2(492, 30), 11, Ink, TextAnchor.MiddleCenter, true);
        }

        private void BuildTitle()
        {
            title = Overlay("Start screen", .52f);
            Label("GATEFALL", title.transform, new Vector2(.5f, .72f), Vector2.zero, new Vector2(490, 74), 57, Cream, TextAnchor.MiddleCenter, true);
            Label("S O N S U Z   K U Ş A T M A", title.transform, new Vector2(.5f, .72f), new Vector2(0, -52), new Vector2(480, 30), 12, Cyan, TextAnchor.MiddleCenter, true);
            var tag = Box("Prototype badge", title.transform, new Vector2(.5f, .72f), new Vector2(0, 69), new Vector2(160, 28), Cyan);
            Label("İLK OYNANABİLİR SÜRÜM", tag, new Vector2(.5f, .5f), Vector2.zero, new Vector2(156, 24), 9, Ink, TextAnchor.MiddleCenter, true);
            Label("Küçük bir top.\nBüyük bir ordu.", title.transform, new Vector2(.5f, .46f), Vector2.zero, new Vector2(470, 106), 35, Cream, TextAnchor.MiddleCenter, true);
            Label("Çarpanları kullan, kalabalığı durdur.\nHer beş dalgada bir titana meydan oku.", title.transform, new Vector2(.5f, .46f), new Vector2(0, -86), new Vector2(466, 60), 15, Cream, TextAnchor.MiddleCenter);
            startButton = Button("Başlat", title.transform, new Vector2(.5f, .19f), Vector2.zero, new Vector2(460, 64), Cyan, "SAVUNMAYI BAŞLAT  ›", 17, Ink, game.StartRun);
            var sound = Button("Ses", title.transform, new Vector2(.5f, .19f), new Vector2(0, -65), new Vector2(150, 44), new Color32(42, 57, 75, 255), "SES AÇIK", 11, Cream, game.ToggleSound);
            titleSound = sound.GetComponentInChildren<Text>();
            titleBest = Label("REKOR  0", title.transform, new Vector2(.5f, 0), new Vector2(0, 31), new Vector2(460, 25), 12, Cream, TextAnchor.MiddleCenter, true);
        }

        private void BuildPause()
        {
            pause = Overlay("Pause screen", .86f);
            Label("BİR NEFES.", pause.transform, new Vector2(.5f, .63f), Vector2.zero, new Vector2(470, 70), 42, Cream, TextAnchor.MiddleCenter, true);
            Label("Savunma seni bekliyor.", pause.transform, new Vector2(.5f, .63f), new Vector2(0, -56), new Vector2(450, 36), 17, Muted, TextAnchor.MiddleCenter);
            resumeButton = Button("Devam", pause.transform, new Vector2(.5f, .43f), Vector2.zero, new Vector2(460, 64), Cyan, "DEVAM ET", 17, Ink, game.TogglePause);
            var sound = Button("Ses ayarı", pause.transform, new Vector2(.5f, .43f), new Vector2(0, -82), new Vector2(460, 56), new Color32(42, 57, 75, 255), "SES AÇIK", 13, Cream, game.ToggleSound);
            pauseSound = sound.GetComponentInChildren<Text>();
            Label("Bilgisayarda A / D veya ok tuşlarıyla hareket et.\nP veya Esc ile duraklat.", pause.transform, new Vector2(.5f, .2f), Vector2.zero, new Vector2(460, 60), 13, Muted, TextAnchor.MiddleCenter);
        }

        private void BuildUpgrade()
        {
            upgrade = Overlay("Upgrade selection", .93f);
            Label("DALGA TAMAMLANDI", upgrade.transform, new Vector2(.5f, .85f), Vector2.zero, new Vector2(460, 30), 12, Cyan, TextAnchor.MiddleCenter, true);
            Label("Daha güçlü dön.", upgrade.transform, new Vector2(.5f, .85f), new Vector2(0, -47), new Vector2(480, 64), 35, Cream, TextAnchor.MiddleCenter, true);
            upgradeSubtitle = Label("Bir geliştirme seç ve kuşatmaya devam et.", upgrade.transform, new Vector2(.5f, .85f), new Vector2(0, -99), new Vector2(470, 43), 14, Muted, TextAnchor.MiddleCenter);
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                choiceButtons[i] = Button("Geliştirme " + (i + 1), upgrade.transform, new Vector2(.5f, .55f), new Vector2(0, -i * 136), new Vector2(460, 118), new Color32(42, 57, 75, 255), "", 16, Cream, () => game.ChooseUpgrade(index));
                Label("0" + (i + 1), choiceButtons[i].transform, new Vector2(0, .5f), new Vector2(29, 0), new Vector2(32, 32), 19, Cyan, TextAnchor.MiddleLeft, true);
                choiceTitle[i] = Label("", choiceButtons[i].transform, new Vector2(0, .5f), new Vector2(80, 19), new Vector2(338, 35), 20, Cream, TextAnchor.MiddleLeft, true);
                choiceDetail[i] = Label("", choiceButtons[i].transform, new Vector2(0, .5f), new Vector2(80, -19), new Vector2(338, 45), 13, Muted, TextAnchor.MiddleLeft);
            }
            Label("Geliştirmeler bu koşu boyunca geçerli.", upgrade.transform, new Vector2(.5f, 0), new Vector2(0, 49), new Vector2(460, 32), 12, Muted, TextAnchor.MiddleCenter);
        }

        private void BuildGameOver()
        {
            gameOver = Overlay("Game over", .9f);
            Label("KUŞATMA SONA ERDİ", gameOver.transform, new Vector2(.5f, .77f), Vector2.zero, new Vector2(460, 30), 12, Coral, TextAnchor.MiddleCenter, true);
            Label("Bir tur daha?", gameOver.transform, new Vector2(.5f, .77f), new Vector2(0, -51), new Vector2(470, 70), 41, Cream, TextAnchor.MiddleCenter, true);
            resultScore = Label("0", gameOver.transform, new Vector2(.5f, .52f), Vector2.zero, new Vector2(480, 100), 70, Cyan, TextAnchor.MiddleCenter, true);
            Label("TOPLAM SKOR", gameOver.transform, new Vector2(.5f, .52f), new Vector2(0, -66), new Vector2(440, 30), 12, Muted, TextAnchor.MiddleCenter, true);
            resultDetail = Label("", gameOver.transform, new Vector2(.5f, .37f), Vector2.zero, new Vector2(470, 70), 16, Cream, TextAnchor.MiddleCenter);
            retryButton = Button("Tekrar oyna", gameOver.transform, new Vector2(.5f, .19f), Vector2.zero, new Vector2(460, 64), Cyan, "TEKRAR SAVUN  ›", 17, Ink, game.StartRun);
            record = Label("", gameOver.transform, new Vector2(.5f, .19f), new Vector2(0, -64), new Vector2(460, 32), 13, Cream, TextAnchor.MiddleCenter, true);
        }

        public void FitSafeArea(Rect cameraRect)
        {
            Rect safeArea = Screen.safeArea;
            Vector2 dimensions = new Vector2(Screen.width, Screen.height);
            if (safeArea == lastSafe && dimensions == lastScreen && cameraRect == lastCamera) return;
            lastSafe = safeArea; lastScreen = dimensions; lastCamera = cameraRect;
            // A constant reference width keeps phone controls large, even in a letterboxed desktop window.
            canvas.scaleFactor = Mathf.Max(.1f, cameraRect.width * Screen.width / 540f);
            viewport.anchorMin = cameraRect.min; viewport.anchorMax = cameraRect.max;
            viewport.offsetMin = Vector2.zero; viewport.offsetMax = Vector2.zero;
            var pixels = new Rect(cameraRect.x * Screen.width, cameraRect.y * Screen.height, cameraRect.width * Screen.width, cameraRect.height * Screen.height);
            float left = Mathf.Clamp01((safeArea.xMin - pixels.xMin) / pixels.width);
            float bottom = Mathf.Clamp01((safeArea.yMin - pixels.yMin) / pixels.height);
            float right = Mathf.Clamp01((safeArea.xMax - pixels.xMin) / pixels.width);
            float top = Mathf.Clamp01((safeArea.yMax - pixels.yMin) / pixels.height);
            safe.anchorMin = new Vector2(left, bottom); safe.anchorMax = new Vector2(right, top);
            safe.offsetMin = Vector2.zero; safe.offsetMax = Vector2.zero;
        }

        public bool BlocksPointer(Vector2 position, int pointerId)
        {
            if (EventSystem.current == null) return false;
            var pointer = new PointerEventData(EventSystem.current) { position = position, pointerId = pointerId };
            pointerHits.Clear();
            EventSystem.current.RaycastAll(pointer, pointerHits);
            return pointerHits.Count > 0;
        }

        public void Render()
        {
            GameSession session = game.Session;
            bool changed = renderedState != session.State;
            live.SetActive(session.State != RunState.Title);
            title.SetActive(session.State == RunState.Title);
            pause.SetActive(session.State == RunState.Paused);
            upgrade.SetActive(session.State == RunState.Upgrade);
            gameOver.SetActive(session.State == RunState.GameOver);
            wave.text = "DALGA " + session.Wave.ToString("00"); score.text = session.Score.ToString("N0");
            health.text = "SAVUNMA  " + session.Health + " / " + session.MaxHealth + (session.Shield > 0 ? "   +" + session.Shield : "");
            health.color = session.Health <= 2 ? Coral : Cream;
            stats.text = "GÜÇ " + session.Damage.ToString("0.0") + "     ATIŞ " + session.FireRate.ToString("0.0") + "/sn     BİRLİK " + session.MultiShot;
            hint.gameObject.SetActive(session.State == RunState.Playing && session.Elapsed < 10);
            progress.fillAmount = session.WaveProgress;
            titleBest.text = "REKOR  " + game.BestScore.ToString("N0");
            titleSound.text = pauseSound.text = game.SoundEnabled ? "SES AÇIK" : "SES KAPALI";
            Enemy boss = null;
            foreach (Enemy enemy in session.Enemies) if (enemy.Kind == EnemyKind.Boss) { boss = enemy; break; }
            bossPanel.SetActive(boss != null);
            if (boss != null)
            { bossHealth.text = "TİTAN  ·  " + Mathf.CeilToInt(boss.Health) + " / " + Mathf.CeilToInt(boss.MaxHealth); bossFill.fillAmount = boss.Health / boss.MaxHealth; }
            if (changed)
            {
                renderedState = session.State;
                if (session.State == RunState.Upgrade)
                {
                    upgradeSubtitle.text = session.Wave + ". dalga tamamlandı. Bir geliştirme seç.";
                    for (int i = 0; i < 3; i++)
                    { choiceTitle[i].text = UpgradeName(session.Choices[i]); choiceDetail[i].text = UpgradeDescription(session.Choices[i]); }
                }
                if (session.State == RunState.GameOver)
                {
                    resultScore.text = session.Score.ToString("N0");
                    resultDetail.text = session.Wave + ". dalgaya ulaştın\n" + session.Kills + " düşman durdurdun";
                    record.text = "REKOR  " + game.BestScore.ToString("N0");
                }
                if (EventSystem.current != null)
                {
                    var selected = session.State == RunState.Title ? startButton : session.State == RunState.Paused ? resumeButton
                        : session.State == RunState.GameOver ? retryButton : session.State == RunState.Upgrade ? choiceButtons[0] : null;
                    EventSystem.current.SetSelectedGameObject(selected == null ? null : selected.gameObject);
                }
            }
        }

        private static string UpgradeName(UpgradeKind kind)
        {
            switch (kind)
            {
                case UpgradeKind.FireRate: return "Hızlı namlu";
                case UpgradeKind.Damage: return "Ağır birlikler";
                case UpgradeKind.MultiShot: return "Birlik takviyesi";
                case UpgradeKind.Repair: return "Saha onarımı";
                case UpgradeKind.Shield: return "Enerji kalkanı";
                default: return "Hızlı sevkiyat";
            }
        }

        private static string UpgradeDescription(UpgradeKind kind)
        {
            switch (kind)
            {
                case UpgradeKind.FireRate: return "%20 daha hızlı otomatik ateş.";
                case UpgradeKind.Damage: return "Her birliğe %30 daha fazla güç.";
                case UpgradeKind.MultiShot: return "Her atışta fazladan bir birlik.";
                case UpgradeKind.Repair: return "+2 savunma canı ve +0,2 güç.";
                case UpgradeKind.Shield: return "Gelen iki hasarı engelle.";
                default: return "%15 birlik hızı ve %10 daha fazla güç.";
            }
        }

        private GameObject Overlay(string name, float alpha)
        {
            RectTransform panel = Rect(name, safe, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var image = panel.gameObject.AddComponent<Image>(); image.color = new Color(Ink.r, Ink.g, Ink.b, alpha);
            return panel.gameObject;
        }

        private RectTransform Box(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            RectTransform rect = Rect(name, parent, anchor, anchor, position, size);
            var image = rect.gameObject.AddComponent<Image>(); image.sprite = rounded; image.type = Image.Type.Sliced;
            image.color = color; image.raycastTarget = false; return rect;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = min; rect.anchorMax = max;
            rect.anchoredPosition = position; rect.sizeDelta = size;
            return rect;
        }

        private Text Label(string value, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, int fontSize, Color color, TextAnchor alignment, bool bold = false)
        {
            RectTransform rect = Rect(value.Length > 20 ? "Text" : value, parent, anchor, anchor, position, size);
            // Edge-anchored labels grow inward; centered labels use their natural center.
            rect.pivot = new Vector2(anchor.x, .5f);
            var label = rect.gameObject.AddComponent<Text>(); label.font = font; label.text = value;
            label.fontSize = fontSize; label.color = color; label.alignment = alignment;
            label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal; label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }

        private Button Button(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Color color, string text, int fontSize, Color textColor, UnityEngine.Events.UnityAction action)
        {
            RectTransform rect = Box(name, parent, anchor, position, size, color);
            var image = rect.GetComponent<Image>(); image.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            ColorBlock colors = button.colors; colors.normalColor = Color.white;
            colors.highlightedColor = new Color(.9f, .98f, 1); colors.pressedColor = new Color(.7f, .85f, .9f);
            colors.selectedColor = new Color(.85f, 1, .98f); colors.fadeDuration = .1f; button.colors = colors;
            button.onClick.AddListener(action);
            Label(text, rect, new Vector2(.5f, .5f), Vector2.zero, size - new Vector2(20, 8), fontSize, textColor, TextAnchor.MiddleCenter, true);
            return button;
        }

        private Image Fill(string name, Transform parent, Color color)
        {
            RectTransform rect = Rect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var fill = rect.gameObject.AddComponent<Image>(); fill.sprite = rounded; fill.color = color;
            fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.raycastTarget = false;
            return fill;
        }

        private Sprite RoundedSprite()
        {
            const int size = 64, radius = 14;
            roundTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            roundTexture.name = "Procedural rounded panel"; roundTexture.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(radius - x, x - (size - radius - 1));
                    float dy = Mathf.Max(radius - y, y - (size - radius - 1));
                    float distance = Mathf.Sqrt(Mathf.Max(0, dx) * Mathf.Max(0, dx) + Mathf.Max(0, dy) * Mathf.Max(0, dy));
                    roundTexture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(radius - distance + .5f)));
                }
            roundTexture.Apply();
            return Sprite.Create(roundTexture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }

        private void OnDestroy()
        {
            if (rounded != null) Destroy(rounded);
            if (roundTexture != null) Destroy(roundTexture);
        }
    }
}
