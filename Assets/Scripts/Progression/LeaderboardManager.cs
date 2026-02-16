using UnityEngine;
using System;
using System.Collections.Generic;
using Shredsquatch.Core;

namespace Shredsquatch.Progression
{
    /// <summary>
    /// Manages local leaderboard submissions and retrieval.
    /// Platform integration (Steam, itch.io) deferred until post-playable.
    /// </summary>
    public class LeaderboardManager : MonoBehaviour
    {
        public static LeaderboardManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private bool _autoSubmitOnGameOver = true;

        [Header("Leaderboard Names")]
        [SerializeField] private string _distanceLeaderboardId = "shredsquatch_distance";
        [SerializeField] private string _trickScoreLeaderboardId = "shredsquatch_tricks";
        [SerializeField] private string _dailyLeaderboardId = "shredsquatch_daily";

        // Local leaderboard storage
        private LocalLeaderboardData _localData;
        private const int MaxLocalEntries = 100;

        // Status
        public bool IsConnected { get; private set; }

        // Events
        public event Action<LeaderboardEntry[]> OnLeaderboardLoaded;
        public event Action<int> OnRankReceived;
        public event Action<string> OnError;
        public event Action OnScoreSubmitted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadLocalLeaderboard();
            IsConnected = true;
        }

        private void Start()
        {
            if (GameManager.Instance != null && _autoSubmitOnGameOver)
            {
                GameManager.Instance.OnGameOver += OnGameOver;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameOver -= OnGameOver;
            }
        }

        private void OnGameOver()
        {
            if (GameManager.Instance == null) return;

            var run = GameManager.Instance.CurrentRun;
            SubmitScore(LeaderboardType.Distance, run.Distance);
            SubmitScore(LeaderboardType.TrickScore, run.TrickScore);
        }

        /// <summary>
        /// Submit a score to the specified leaderboard.
        /// </summary>
        public void SubmitScore(LeaderboardType type, float score)
        {
            var entry = new LeaderboardEntry
            {
                PlayerName = GetLocalPlayerName(),
                Score = score,
                Timestamp = DateTime.Now,
                GameMode = GameManager.Instance?.CurrentMode ?? GameMode.Standard
            };

            List<LeaderboardEntry> entries = GetLocalEntries(type);
            entries.Add(entry);

            // Sort descending by score
            entries.Sort((a, b) => b.Score.CompareTo(a.Score));

            // Trim to max entries
            if (entries.Count > MaxLocalEntries)
            {
                entries.RemoveRange(MaxLocalEntries, entries.Count - MaxLocalEntries);
            }

            // Find player's rank
            int rank = entries.FindIndex(e =>
                e.PlayerName == entry.PlayerName &&
                Math.Abs(e.Score - entry.Score) < 0.001f &&
                e.Timestamp == entry.Timestamp) + 1;

            SaveLocalLeaderboard();

            Debug.Log($"[Leaderboard] Local submit: {type} = {score:F2}, Rank #{rank}");
            OnScoreSubmitted?.Invoke();
            OnRankReceived?.Invoke(rank);
        }

        /// <summary>
        /// Load top scores from a leaderboard.
        /// </summary>
        public void LoadLeaderboard(LeaderboardType type, int count = 10)
        {
            var entries = GetLocalEntries(type);
            int resultCount = Mathf.Min(count, entries.Count);

            var results = new LeaderboardEntry[resultCount];
            for (int i = 0; i < resultCount; i++)
            {
                results[i] = entries[i];
                results[i].Rank = i + 1;
            }

            OnLeaderboardLoaded?.Invoke(results);
        }

        /// <summary>
        /// Get the player's rank on a leaderboard.
        /// </summary>
        public void GetPlayerRank(LeaderboardType type)
        {
            var entries = GetLocalEntries(type);
            string playerName = GetLocalPlayerName();
            int rank = entries.FindIndex(e => e.PlayerName == playerName) + 1;
            OnRankReceived?.Invoke(rank > 0 ? rank : -1);
        }

        /// <summary>
        /// Report an achievement unlock (stub for future platform integration).
        /// </summary>
        public void ReportAchievement(string achievementId)
        {
            Debug.Log($"[Leaderboard] Achievement reported: {achievementId}");
        }

        #region Helpers

        private string GetLeaderboardId(LeaderboardType type)
        {
            return type switch
            {
                LeaderboardType.Distance => _distanceLeaderboardId,
                LeaderboardType.TrickScore => _trickScoreLeaderboardId,
                LeaderboardType.Daily => _dailyLeaderboardId,
                _ => _distanceLeaderboardId
            };
        }

        private List<LeaderboardEntry> GetLocalEntries(LeaderboardType type)
        {
            return type switch
            {
                LeaderboardType.Distance => _localData.DistanceEntries,
                LeaderboardType.TrickScore => _localData.TrickScoreEntries,
                LeaderboardType.Daily => _localData.DailyEntries,
                _ => _localData.DistanceEntries
            };
        }

        private string GetLocalPlayerName()
        {
            return PlayerPrefs.GetString("PlayerName", "Player");
        }

        /// <summary>
        /// Set the local player name.
        /// </summary>
        public void SetPlayerName(string name)
        {
            PlayerPrefs.SetString("PlayerName", name);
            PlayerPrefs.Save();
        }

        private void SaveLocalLeaderboard()
        {
            string json = JsonUtility.ToJson(_localData);
            PlayerPrefs.SetString("LocalLeaderboard", json);
            PlayerPrefs.Save();
        }

        private void LoadLocalLeaderboard()
        {
            if (PlayerPrefs.HasKey("LocalLeaderboard"))
            {
                string json = PlayerPrefs.GetString("LocalLeaderboard");
                _localData = JsonUtility.FromJson<LocalLeaderboardData>(json);
            }
            else
            {
                _localData = new LocalLeaderboardData();
            }
        }

        /// <summary>
        /// Clear local leaderboard (for testing).
        /// </summary>
        public void ClearLocalLeaderboard()
        {
            _localData = new LocalLeaderboardData();
            SaveLocalLeaderboard();
            Debug.Log("[Leaderboard] Local leaderboard cleared");
        }

        #endregion
    }

    /// <summary>
    /// Leaderboard types.
    /// </summary>
    public enum LeaderboardType
    {
        Distance,
        TrickScore,
        Daily
    }

    /// <summary>
    /// A single leaderboard entry.
    /// </summary>
    [Serializable]
    public class LeaderboardEntry
    {
        public string PlayerName;
        public float Score;
        public int Rank;
        // JsonUtility cannot serialize DateTime. Store as ticks (long) and reconstruct.
        public long TimestampTicks;
        public GameMode GameMode;

        [NonSerialized]
        private DateTime? _timestamp;

        public DateTime Timestamp
        {
            get => _timestamp ?? (_timestamp = new DateTime(TimestampTicks)).Value;
            set
            {
                _timestamp = value;
                TimestampTicks = value.Ticks;
            }
        }
    }

    /// <summary>
    /// Local leaderboard storage.
    /// </summary>
    [Serializable]
    public class LocalLeaderboardData
    {
        public List<LeaderboardEntry> DistanceEntries = new List<LeaderboardEntry>();
        public List<LeaderboardEntry> TrickScoreEntries = new List<LeaderboardEntry>();
        public List<LeaderboardEntry> DailyEntries = new List<LeaderboardEntry>();
    }
}
