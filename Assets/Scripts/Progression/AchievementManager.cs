using UnityEngine;
using System;
using System.Collections.Generic;
using Shredsquatch.Core;
using Shredsquatch.Player;
using Shredsquatch.Tricks;
using Shredsquatch.Sasquatch;

namespace Shredsquatch.Progression
{
    /// <summary>
    /// Manages achievement tracking, unlocking, and persistence.
    /// </summary>
    public class AchievementManager : MonoBehaviour
    {
        public static AchievementManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private bool _showNotifications = true;

        // Achievement data
        private Dictionary<AchievementId, Achievement> _achievements;
        private AchievementSaveData _saveData;

        // Tracking stats for current session
        private int _totalTricksLanded;
        private int _totalRailMeters;
        private float _maxSpeedReached;
        private bool _escapedPulsingRed;

        // Wired references (for unsubscription)
        private TrickController _trickController;
        private Tricks.RailGrindController _railController;
        private SasquatchAI _sasquatch;

        // Events
        public event Action<Achievement> OnAchievementUnlocked;
        public event Action<AchievementId, float> OnAchievementProgress;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeAchievements();
            LoadProgress();
        }

        private void Start()
        {
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void InitializeAchievements()
        {
            _achievements = new Dictionary<AchievementId, Achievement>
            {
                // Distance achievements
                [AchievementId.FirstRun] = new Achievement(
                    AchievementId.FirstRun,
                    "First Run",
                    "Complete your first run",
                    AchievementType.Single
                ),
                [AchievementId.Distance5K] = new Achievement(
                    AchievementId.Distance5K,
                    "5K Club",
                    "Reach 5 kilometers",
                    AchievementType.Single
                ),
                [AchievementId.Distance10K] = new Achievement(
                    AchievementId.Distance10K,
                    "10K Legend",
                    "Reach 10 kilometers",
                    AchievementType.Single
                ),
                [AchievementId.Distance20K] = new Achievement(
                    AchievementId.Distance20K,
                    "20K Master",
                    "Reach 20 kilometers",
                    AchievementType.Single
                ),
                [AchievementId.Distance30K] = new Achievement(
                    AchievementId.Distance30K,
                    "30K Immortal",
                    "Reach 30 kilometers",
                    AchievementType.Single
                ),

                // Trick achievements
                [AchievementId.TrickNovice] = new Achievement(
                    AchievementId.TrickNovice,
                    "Trick Novice",
                    "Land 100 tricks",
                    AchievementType.Cumulative,
                    100
                ),
                [AchievementId.TrickMaster] = new Achievement(
                    AchievementId.TrickMaster,
                    "Trick Master",
                    "Land 1,000 tricks",
                    AchievementType.Cumulative,
                    1000
                ),
                [AchievementId.ComboKing] = new Achievement(
                    AchievementId.ComboKing,
                    "Combo King",
                    "Achieve a 10-trick combo",
                    AchievementType.Single
                ),

                // Rail achievements
                [AchievementId.RailRider] = new Achievement(
                    AchievementId.RailRider,
                    "Rail Rider",
                    "Grind for 100 total meters",
                    AchievementType.Cumulative,
                    100
                ),

                // Game mode achievements
                [AchievementId.NightOwl] = new Achievement(
                    AchievementId.NightOwl,
                    "Night Owl",
                    "Survive past 10km in Night Mode",
                    AchievementType.Single
                ),
                [AchievementId.AvalancheSurvivor] = new Achievement(
                    AchievementId.AvalancheSurvivor,
                    "Avalanche Survivor",
                    "Survive 5km in Avalanche Mode",
                    AchievementType.Single
                ),
                [AchievementId.StormChaser] = new Achievement(
                    AchievementId.StormChaser,
                    "Storm Chaser",
                    "Survive 5km in Storm Mode",
                    AchievementType.Single
                ),

                // Skill achievements
                [AchievementId.SpeedDemon] = new Achievement(
                    AchievementId.SpeedDemon,
                    "Speed Demon",
                    "Reach 120 km/h",
                    AchievementType.Single
                ),
                [AchievementId.CloseCall] = new Achievement(
                    AchievementId.CloseCall,
                    "Close Call",
                    "Escape when proximity bar is pulsing red",
                    AchievementType.Single
                ),

                // Unlock achievements
                [AchievementId.OldFriend] = new Achievement(
                    AchievementId.OldFriend,
                    "Old Friend",
                    "Unlock the Classic Yeti skin",
                    AchievementType.Single
                ),
                [AchievementId.GoldenLegend] = new Achievement(
                    AchievementId.GoldenLegend,
                    "Golden Legend",
                    "Unlock the Golden Sasquatch",
                    AchievementType.Single
                ),

                // Easter egg
                [AchievementId.GetMeOff] = new Achievement(
                    AchievementId.GetMeOff,
                    "Get Me Off This Mountain!",
                    "A nod to the classics",
                    AchievementType.Hidden
                )
            };
        }

        private void SubscribeToEvents()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameOver += OnGameOver;
                GameManager.Instance.OnDistanceChanged += OnDistanceChanged;
            }
        }

        /// <summary>
        /// Wire trick controller events. Call from SceneInitializer after player is spawned.
        /// </summary>
        public void SetTrickController(TrickController trickController)
        {
            // Unsubscribe from previous controller if any
            if (_trickController != null)
            {
                _trickController.OnTrickCompleted -= OnTrickCompleted;
                _trickController.OnComboUpdated -= OnComboUpdated;
            }

            _trickController = trickController;

            if (trickController != null)
            {
                trickController.OnTrickCompleted += OnTrickCompleted;
                trickController.OnComboUpdated += OnComboUpdated;
            }
        }

        /// <summary>
        /// Wire rail grind controller events. Call from SceneInitializer after player is spawned.
        /// Without this wiring, the RailRider achievement can never be unlocked.
        /// </summary>
        public void SetRailGrindController(Tricks.RailGrindController railController)
        {
            if (_railController != null)
            {
                _railController.OnGrindDistanceComplete -= OnGrindComplete;
            }

            _railController = railController;

            if (railController != null)
            {
                railController.OnGrindDistanceComplete += OnGrindComplete;
            }
        }

        /// <summary>
        /// Wire sasquatch events. Call from SceneInitializer after sasquatch is spawned.
        /// </summary>
        public void SetSasquatch(SasquatchAI sasquatch)
        {
            if (_sasquatch != null)
            {
                _sasquatch.OnDistanceChanged -= OnSasquatchDistanceChanged;
            }

            _sasquatch = sasquatch;

            if (sasquatch != null)
            {
                sasquatch.OnDistanceChanged += OnSasquatchDistanceChanged;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameOver -= OnGameOver;
                GameManager.Instance.OnDistanceChanged -= OnDistanceChanged;
            }

            if (_trickController != null)
            {
                _trickController.OnTrickCompleted -= OnTrickCompleted;
                _trickController.OnComboUpdated -= OnComboUpdated;
            }

            if (_railController != null)
            {
                _railController.OnGrindDistanceComplete -= OnGrindComplete;
            }

            if (_sasquatch != null)
            {
                _sasquatch.OnDistanceChanged -= OnSasquatchDistanceChanged;
            }
        }

        private void OnGameOver()
        {
            // First run achievement
            TryUnlock(AchievementId.FirstRun);

            // Check distance achievements
            float distance = GameManager.Instance.CurrentRun.Distance;
            CheckDistanceAchievements(distance);

            // Check game mode achievements
            CheckGameModeAchievements(distance);

            // Save progress
            SaveProgress();
        }

        private void OnDistanceChanged(float distance)
        {
            // Check speed
            if (GameManager.Instance != null)
            {
                float speed = GameManager.Instance.CurrentRun.MaxSpeed;
                if (speed >= 120f)
                {
                    TryUnlock(AchievementId.SpeedDemon);
                }
            }
        }

        private void OnTrickCompleted(string trickName, int points)
        {
            _totalTricksLanded++;
            _saveData.TotalTricksLanded++;

            // Check cumulative trick achievements
            UpdateProgress(AchievementId.TrickNovice, _saveData.TotalTricksLanded);
            UpdateProgress(AchievementId.TrickMaster, _saveData.TotalTricksLanded);
        }

        private void OnComboUpdated(int comboCount)
        {
            if (comboCount >= 10)
            {
                TryUnlock(AchievementId.ComboKing);
            }
        }

        private void OnGrindComplete(float meters)
        {
            _saveData.TotalRailMeters += meters;
            UpdateProgress(AchievementId.RailRider, _saveData.TotalRailMeters);
        }

        private bool _wasInPulsingRed;

        private void OnSasquatchDistanceChanged(float distance)
        {
            // Track if we were in pulsing red (<150m) and escaped to safe (>300m)
            if (distance < 150f)
            {
                _wasInPulsingRed = true;
            }
            else if (_wasInPulsingRed && distance > 300f)
            {
                TryUnlock(AchievementId.CloseCall);
                _wasInPulsingRed = false;
            }
        }

        private void CheckDistanceAchievements(float distance)
        {
            if (distance >= 5f) TryUnlock(AchievementId.Distance5K);
            if (distance >= 10f) TryUnlock(AchievementId.Distance10K);
            if (distance >= 20f) TryUnlock(AchievementId.Distance20K);
            if (distance >= 30f) TryUnlock(AchievementId.Distance30K);

            // Skin unlock achievements
            if (distance >= 5f) TryUnlock(AchievementId.OldFriend);
            if (distance >= 30f) TryUnlock(AchievementId.GoldenLegend);
        }

        private void CheckGameModeAchievements(float distance)
        {
            if (GameManager.Instance == null) return;

            GameMode mode = GameManager.Instance.CurrentMode;

            // Night mode requires checking if night was reached (10km+)
            if (GameManager.Instance.Progress.NightModeUnlocked && distance >= 10f)
            {
                TryUnlock(AchievementId.NightOwl);
            }

            // Game mode specific
            switch (mode)
            {
                case GameMode.Avalanche:
                    if (distance >= 5f) TryUnlock(AchievementId.AvalancheSurvivor);
                    break;
                case GameMode.Storm:
                    if (distance >= 5f) TryUnlock(AchievementId.StormChaser);
                    break;
            }
        }

        /// <summary>
        /// Try to unlock an achievement. Returns true if newly unlocked.
        /// </summary>
        public bool TryUnlock(AchievementId id)
        {
            if (!_achievements.TryGetValue(id, out var achievement))
                return false;

            if (achievement.IsUnlocked)
                return false;

            achievement.Unlock();
            _saveData.Add(id);

            Debug.Log($"[Achievement] Unlocked: {achievement.Name}");
            OnAchievementUnlocked?.Invoke(achievement);

            // Report to platform
            LeaderboardManager.Instance?.ReportAchievement(id.ToString());

            SaveProgress();
            return true;
        }

        /// <summary>
        /// Update progress on a cumulative achievement.
        /// </summary>
        public void UpdateProgress(AchievementId id, float currentValue)
        {
            if (!_achievements.TryGetValue(id, out var achievement))
                return;

            if (achievement.IsUnlocked)
                return;

            if (achievement.Type != AchievementType.Cumulative)
                return;

            achievement.CurrentProgress = currentValue;
            OnAchievementProgress?.Invoke(id, currentValue / achievement.TargetValue);

            if (currentValue >= achievement.TargetValue)
            {
                TryUnlock(id);
            }
        }

        /// <summary>
        /// Trigger the Easter egg achievement.
        /// </summary>
        public void TriggerEasterEgg()
        {
            TryUnlock(AchievementId.GetMeOff);
        }

        /// <summary>
        /// Get achievement by ID.
        /// </summary>
        public Achievement GetAchievement(AchievementId id)
        {
            return _achievements.TryGetValue(id, out var achievement) ? achievement : null;
        }

        /// <summary>
        /// Get all achievements.
        /// </summary>
        public IEnumerable<Achievement> GetAllAchievements()
        {
            return _achievements.Values;
        }

        /// <summary>
        /// Get unlock percentage.
        /// </summary>
        public float GetCompletionPercentage()
        {
            int unlocked = 0;
            int total = 0;

            foreach (var achievement in _achievements.Values)
            {
                if (achievement.Type != AchievementType.Hidden || achievement.IsUnlocked)
                {
                    total++;
                    if (achievement.IsUnlocked) unlocked++;
                }
            }

            return total > 0 ? (float)unlocked / total : 0f;
        }

        private void SaveProgress()
        {
            string json = JsonUtility.ToJson(_saveData);
            PlayerPrefs.SetString("Achievements", json);
            PlayerPrefs.Save();
        }

        private void LoadProgress()
        {
            if (PlayerPrefs.HasKey("Achievements"))
            {
                try
                {
                    string json = PlayerPrefs.GetString("Achievements");
                    _saveData = JsonUtility.FromJson<AchievementSaveData>(json) ?? new AchievementSaveData();

                    // Restore unlocked state
                    foreach (var id in _saveData.UnlockedIds)
                    {
                        if (_achievements.TryGetValue(id, out var achievement))
                        {
                            achievement.Unlock();
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[AchievementManager] Corrupt achievement data, resetting: {ex.Message}");
                    _saveData = new AchievementSaveData();
                }
            }
            else
            {
                _saveData = new AchievementSaveData();
            }
        }

        /// <summary>
        /// Reset all achievements (for testing).
        /// </summary>
        public void ResetAllAchievements()
        {
            foreach (var achievement in _achievements.Values)
            {
                achievement.Reset();
            }
            _saveData = new AchievementSaveData();
            SaveProgress();
            Debug.Log("[Achievement] All achievements reset");
        }
    }

    /// <summary>
    /// Achievement identifiers.
    /// </summary>
    public enum AchievementId
    {
        // Distance
        FirstRun,
        Distance5K,
        Distance10K,
        Distance20K,
        Distance30K,

        // Tricks
        TrickNovice,
        TrickMaster,
        ComboKing,

        // Rails
        RailRider,

        // Game Modes
        NightOwl,
        AvalancheSurvivor,
        StormChaser,

        // Skill
        SpeedDemon,
        CloseCall,

        // Unlocks
        OldFriend,
        GoldenLegend,

        // Easter Egg
        GetMeOff
    }

    /// <summary>
    /// Achievement type.
    /// </summary>
    public enum AchievementType
    {
        Single,      // One-time unlock
        Cumulative,  // Progress over multiple runs
        Hidden       // Not shown until unlocked
    }

    /// <summary>
    /// Achievement data.
    /// </summary>
    [Serializable]
    public class Achievement
    {
        public AchievementId Id { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public AchievementType Type { get; private set; }
        public float TargetValue { get; private set; }
        public float CurrentProgress { get; set; }
        public bool IsUnlocked { get; private set; }
        public DateTime UnlockTime { get; private set; }

        public Achievement(AchievementId id, string name, string description,
            AchievementType type, float targetValue = 1f)
        {
            Id = id;
            Name = name;
            Description = description;
            Type = type;
            TargetValue = targetValue;
            CurrentProgress = 0f;
            IsUnlocked = false;
        }

        public void Unlock()
        {
            if (IsUnlocked) return;
            IsUnlocked = true;
            UnlockTime = DateTime.Now;
            CurrentProgress = TargetValue;
        }

        public void Reset()
        {
            IsUnlocked = false;
            CurrentProgress = 0f;
        }
    }

    /// <summary>
    /// Serializable save data for achievements.
    /// JsonUtility cannot serialize List of enums directly, so we store them as ints.
    /// </summary>
    [Serializable]
    public class AchievementSaveData
    {
        public List<int> UnlockedIdValues = new List<int>();
        public int TotalTricksLanded;
        public float TotalRailMeters;

        [NonSerialized]
        private List<AchievementId> _cachedIds;

        public List<AchievementId> UnlockedIds
        {
            get
            {
                if (_cachedIds == null)
                {
                    _cachedIds = new List<AchievementId>();
                    foreach (int val in UnlockedIdValues)
                    {
                        _cachedIds.Add((AchievementId)val);
                    }
                }
                return _cachedIds;
            }
        }

        public void Add(AchievementId id)
        {
            int val = (int)id;
            if (!UnlockedIdValues.Contains(val))
            {
                UnlockedIdValues.Add(val);
                _cachedIds?.Add(id);
            }
        }
    }
}
