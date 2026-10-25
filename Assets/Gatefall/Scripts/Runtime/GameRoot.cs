using System;
using Gatefall.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Gatefall
{
    public sealed class GameRoot : MonoBehaviour
    {
        public GameSession Session { get; private set; }
        public ArenaView Arena { get; private set; }
        public GameHud Hud { get; private set; }
        public int BestScore { get; private set; }
        public bool SoundEnabled { get; private set; }
        private GameAudio gameAudio;
        private float accumulator, targetX;
        private bool dragging;
        private int pointerId = -1;
        private RunState previousState;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureGame()
        {
            if (FindFirstObjectByType<GameRoot>() == null) new GameObject("Gatefall").AddComponent<GameRoot>();
        }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Session = new GameSession();
            BestScore = Math.Max(0, PlayerPrefs.GetInt("gatefall.best", 0));
            SoundEnabled = PlayerPrefs.GetInt("gatefall.sound", 1) == 1;
            Arena = new GameObject("Arena").AddComponent<ArenaView>();
            Arena.Build();
            gameAudio = gameObject.AddComponent<GameAudio>();
            Hud = new GameObject("Game UI").AddComponent<GameHud>();
            Hud.Build(this);
            previousState = RunState.Title;
        }

        public void StartRun()
        {
            Session.Start(unchecked(Environment.TickCount));
            accumulator = 0; targetX = 0; dragging = false; pointerId = -1;
            Arena.ResetEffects();
        }

        public void TogglePause()
        {
            Session.SetPaused(Session.State == RunState.Playing);
            dragging = false; pointerId = -1;
        }

        public void ToggleSound()
        {
            SoundEnabled = !SoundEnabled;
            PlayerPrefs.SetInt("gatefall.sound", SoundEnabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void ChooseUpgrade(int index)
        {
            if (Session.ChooseUpgrade(index))
            { accumulator = 0; dragging = false; pointerId = -1; }
        }

        private void Update()
        {
            Arena.FitCamera(); Hud.FitSafeArea(Arena.GameCamera.rect);
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)) TogglePause();
            ReadInput();
            if (Session.State == RunState.Playing)
            {
                float step = 1f / 60f;
                accumulator += Mathf.Min(Time.unscaledDeltaTime, .2f);
                while (accumulator >= step && Session.State == RunState.Playing)
                {
                    Session.Move(Mathf.MoveTowards(Session.CannonX, targetX, 17 * step));
                    Session.Advance(step);
                    foreach (var message in Session.Events)
                    {
                        Arena.React(message);
                        if (SoundEnabled) gameAudio.Play(message);
                    }
                    Session.ClearEvents();
                    accumulator -= step;
                }
            }
            else accumulator = 0;

            if (Session.State != previousState)
            {
                dragging = false; pointerId = -1;
                if (Session.State == RunState.GameOver) SaveRecord();
                previousState = Session.State;
            }
            Arena.Render(Session, Time.unscaledDeltaTime);
            Hud.Render();
        }

        private void ReadInput()
        {
            if (Session.State != RunState.Playing) return;
            float keyboard = 0;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) keyboard -= 1;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) keyboard += 1;
            targetX = Mathf.Clamp(targetX + keyboard * 8 * Time.unscaledDeltaTime, -GameSession.LaneLimit, GameSession.LaneLimit);

            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch touch = Input.GetTouch(i);
                    if (touch.phase == TouchPhase.Began && !dragging && !Hud.BlocksPointer(touch.position, touch.fingerId))
                    { dragging = true; pointerId = touch.fingerId; }
                    if (dragging && touch.fingerId == pointerId)
                    {
                        MovePointer(touch.position);
                        if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                        { dragging = false; pointerId = -1; }
                    }
                }
                return;
            }

            if (Input.GetMouseButtonDown(0) && !Hud.BlocksPointer(Input.mousePosition, -1)) { dragging = true; pointerId = -1; }
            if (dragging && Input.GetMouseButton(0)) MovePointer(Input.mousePosition);
            if (Input.GetMouseButtonUp(0)) dragging = false;
        }

        private void MovePointer(Vector2 screen)
        {
            if (!Arena.GameCamera.pixelRect.Contains(screen)) return;
            // Map the pointer to the cannon's depth, independent of where on the road it is dragged.
            float x = (screen.x - Arena.GameCamera.pixelRect.x) / Arena.GameCamera.pixelRect.width;
            targetX = Mathf.Lerp(-GameSession.LaneLimit, GameSession.LaneLimit, x);
        }

        private void SaveRecord()
        {
            if (Session.Score <= BestScore) return;
            BestScore = Session.Score;
            PlayerPrefs.SetInt("gatefall.best", BestScore);
            PlayerPrefs.Save();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) { Session?.SetPaused(true); SaveRecordIfRunning(); }
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) { Session?.SetPaused(true); dragging = false; pointerId = -1; }
        }

        private void SaveRecordIfRunning() { if (Session != null) SaveRecord(); }
        private void OnApplicationQuit() { SaveRecordIfRunning(); }
    }
}
