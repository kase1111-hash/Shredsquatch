using UnityEngine;
using System;

namespace Shredsquatch.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game State")]
        [SerializeField] private GameState _currentState = GameState.MainMenu;
        [SerializeField] private GameMode _currentMode = GameMode.Standard;

        [Header("References")]
        [SerializeField] private Transform _player;

        public GameState CurrentState => _currentState;
        public GameMode CurrentMode => _currentMode;
        public RunStats CurrentRun { get; private set; } = new RunStats();
        public PlayerProgress Progress { get; private set; } = new PlayerProgress();

        // Events
        public event Action<GameState> OnStateChanged;
        public event Action<float> OnDistanceChanged;
        public event Action OnGameOver;
        public event Action OnRunStarted;

        // Distance tracking
        private Vector3 _startPosition;
        private float _lastDistanceUpdate;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadProgress();
        }

        private void Update()
        {
            if (_currentState == GameState.Playing)
            {
                UpdateRunStats();
            }
        }

        public void StartRun(GameMode mode = GameMode.Standard)
        {
            _currentMode = mode;
            CurrentRun.Reset();

            if (_player != null)
            {
                _startPosition = _player.position;
            }

            SetState(GameState.Playing);
            OnRunStarted?.Invoke();
        }

        public void PauseGame()
        {
            if (_currentState == GameState.Playing)
            {
                SetState(GameState.Paused);
                Time.timeScale = 0f;
            }
        }

        public void ResumeGame()
        {
            if (_currentState == GameState.Paused)
            {
                SetState(GameState.Playing);
                Time.timeScale = 1f;
            }
        }

        public void EndRun()
        {
            Time.timeScale = 1f;
            Progress.CheckUnlocks(CurrentRun.Distance);
            SaveProgress();
            SetState(GameState.GameOver);
            OnGameOver?.Invoke();
        }

        public void ReturnToMenu()
        {
            Time.timeScale = 1f;
            SetState(GameState.MainMenu);
        }

        private void SetState(GameState newState)
        {
            _currentState = newState;
            OnStateChanged?.Invoke(newState);
        }

        private void UpdateRunStats()
        {
            if (_player == null) return;

            CurrentRun.RunTime += Time.deltaTime;

            // Calculate distance in kilometers
            float rawDistance = Vector3.Distance(_startPosition, _player.position);
            CurrentRun.Distance = rawDistance / 1000f; // Convert to km

            // Notify listeners periodically (every 0.1km)
            if (CurrentRun.Distance - _lastDistanceUpdate >= 0.1f)
            {
                _lastDistanceUpdate = CurrentRun.Distance;
                OnDistanceChanged?.Invoke(CurrentRun.Distance);
            }
        }

        public void AddTrickScore(int points, int comboLength)
        {
            CurrentRun.TrickScore += points;
            CurrentRun.TrickCount++;

            if (comboLength > CurrentRun.MaxCombo)
            {
                CurrentRun.MaxCombo = comboLength;
            }
        }

        public void UpdateMaxSpeed(float speed)
        {
            if (speed > CurrentRun.MaxSpeed)
            {
                CurrentRun.MaxSpeed = speed;
            }
        }

        public void CollectCoin()
        {
            CurrentRun.CoinsCollected++;
            CurrentRun.TrickScore += 50; // 50 trick pts each
        }

        public TimeOfDay GetTimeOfDay()
        {
            float dist = CurrentRun.Distance;
            if (dist < 3f) return TimeOfDay.Dawn;
            if (dist < 6f) return TimeOfDay.Midday;
            if (dist < 10f) return TimeOfDay.Dusk;
            return TimeOfDay.Night;
        }

        public float GetVisibilityRange()
        {
            return GetTimeOfDay() switch
            {
                TimeOfDay.Dawn => 150f,
                TimeOfDay.Midday => 200f,
                TimeOfDay.Dusk => 120f,
                TimeOfDay.Night => Progress.NightModeUnlocked ? 80f : 40f,
                _ => 150f
            };
        }

        private void SaveProgress()
        {
            string json = JsonUtility.ToJson(Progress);
            PlayerPrefs.SetString("PlayerProgress", json);
            PlayerPrefs.Save();
        }

        private void LoadProgress()
        {
            if (PlayerPrefs.HasKey("PlayerProgress"))
            {
                string json = PlayerPrefs.GetString("PlayerProgress");
                Progress = JsonUtility.FromJson<PlayerProgress>(json);
            }
            else
            {
                Progress = new PlayerProgress();
            }
        }

        public void SetPlayerReference(Transform player)
        {
            _player = player;
        }
    }
}
