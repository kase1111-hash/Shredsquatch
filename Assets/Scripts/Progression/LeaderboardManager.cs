using UnityEngine;
using System;
using System.Collections.Generic;
using Shredsquatch.Core;

namespace Shredsquatch.Progression
{
    /// <summary>
    /// Manages leaderboard submissions and retrieval.
    /// Provides stubs for Steam, itch.io, and local leaderboards.
    /// </summary>
    public class LeaderboardManager : MonoBehaviour
    {
        public static LeaderboardManager Instance { get; private set; }

        [Header("Platform Settings")]
        [SerializeField] private LeaderboardPlatform _activePlatform = LeaderboardPlatform.Local;
        [SerializeField] private bool _autoSubmitOnGameOver = true;

        [Header("Leaderboard Names")]
        [SerializeField] private string _distanceLeaderboardId = "shredsquatch_distance";
        [SerializeField] private string _trickScoreLeaderboardId = "shredsquatch_tricks";
        [SerializeField] private string _dailyLeaderboardId = "shredsquatch_daily";

        // Local leaderboard storage
        private LocalLeaderboardData _localData;
        private const int MaxLocalEntries = 100;

        // Platform integration status
        public bool IsConnected { get; private set; }
        public LeaderboardPlatform ActivePlatform => _activePlatform;

        // Events
        public event Action<LeaderboardEntry[]> OnLeaderboardLoaded;
        public event Action<int> OnRankReceived; // Player's rank
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
            InitializePlatform();
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

        private void InitializePlatform()
        {
            switch (_activePlatform)
            {
                case LeaderboardPlatform.Steam:
                    InitializeSteam();
                    break;
                case LeaderboardPlatform.ItchIO:
                    InitializeItchIO();
                    break;
                case LeaderboardPlatform.Local:
                default:
                    IsConnected = true;
                    Debug.Log("[Leaderboard] Using local leaderboard");
                    break;
            }
        }

        #region Platform Initialization Stubs

        private void InitializeSteam()
        {
            // STEAM INTEGRATION STUB
            // To implement:
            // 1. Import Steamworks.NET package
            // 2. Initialize SteamClient
            // 3. Get leaderboard handles
            //
            // Example:
            // if (SteamClient.IsValid)
            // {
            //     _distanceLeaderboard = await SteamUserStats.FindOrCreateLeaderboardAsync(
            //         _distanceLeaderboardId,
            //         LeaderboardSort.Descending,
            //         LeaderboardDisplay.Numeric
            //     );
            //     IsConnected = true;
            // }

            Debug.Log("[Leaderboard] Steam integration not yet implemented - using local");
            _activePlatform = LeaderboardPlatform.Local;
            IsConnected = true;
        }

        private void InitializeItchIO()
        {
            // ITCH.IO INTEGRATION STUB
            // To implement:
            // 1. Use itch.io butler API or custom backend
            // 2. Authenticate user
            // 3. Submit/retrieve scores via HTTP
            //
            // Note: itch.io doesn't have a native leaderboard API.
            // Options:
            // - Use a third-party service (PlayFab, GameJolt, etc.)
            // - Host your own simple leaderboard server
            // - Use browser localStorage for local-only

            Debug.Log("[Leaderboard] itch.io integration not yet implemented - using local");
            _activePlatform = LeaderboardPlatform.Local;
            IsConnected = true;
        }

        #endregion

        #region Score Submission

        private void OnGameOver()
        {
            if (GameManager.Instance == null) return;

            var run = GameManager.Instance.CurrentRun;

            // Submit distance
            SubmitScore(LeaderboardType.Distance, run.Distance);

            // Submit trick score
            SubmitScore(LeaderboardType.TrickScore, run.TrickScore);

            // Submit to daily if using daily seed
            // TODO: Implement daily seed tracking
        }

        /// <summary>
        /// Submit a score to the specified leaderboard.
        /// </summary>
        public void SubmitScore(LeaderboardType type, float score)
        {
            string leaderboardId = GetLeaderboardId(type);

            switch (_activePlatform)
            {
                case LeaderboardPlatform.Steam:
                    SubmitScoreSteam(leaderboardId, score);
                    break;
                case LeaderboardPlatform.ItchIO:
                    SubmitScoreItchIO(leaderboardId, score);
                    break;
                case LeaderboardPlatform.Local:
                default:
                    SubmitScoreLocal(type, score);
                    break;
            }
        }

        private void SubmitScoreSteam(string leaderboardId, float score)
        {
            // STEAM SUBMISSION STUB
            // Example:
            // var result = await _distanceLeaderboard.SubmitScoreAsync((int)(score * 1000));
            // if (result.HasValue)
            // {
            //     OnScoreSubmitted?.Invoke();
            //     OnRankReceived?.Invoke(result.Value.NewGlobalRank);
            // }

            Debug.Log($"[Leaderboard] Steam submit stub: {leaderboardId} = {score}");
            OnScoreSubmitted?.Invoke();
        }

        private void SubmitScoreItchIO(string leaderboardId, float score)
        {
            // ITCH.IO SUBMISSION STUB
            // Example with custom backend:
            // StartCoroutine(PostScoreCoroutine(leaderboardId, score));

            Debug.Log($"[Leaderboard] itch.io submit stub: {leaderboardId} = {score}");
            OnScoreSubmitted?.Invoke();
        }

        private void SubmitScoreLocal(LeaderboardType type, float score)
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

        #endregion

        #region Score Retrieval

        /// <summary>
        /// Load top scores from a leaderboard.
        /// </summary>
        public void LoadLeaderboard(LeaderboardType type, int count = 10)
        {
            switch (_activePlatform)
            {
                case LeaderboardPlatform.Steam:
                    LoadLeaderboardSteam(GetLeaderboardId(type), count);
                    break;
                case LeaderboardPlatform.ItchIO:
                    LoadLeaderboardItchIO(GetLeaderboardId(type), count);
                    break;
                case LeaderboardPlatform.Local:
                default:
                    LoadLeaderboardLocal(type, count);
                    break;
            }
        }

        private void LoadLeaderboardSteam(string leaderboardId, int count)
        {
            // STEAM RETRIEVAL STUB
            // Example:
            // var entries = await _distanceLeaderboard.GetScoresAsync(count);
            // var results = entries.Select(e => new LeaderboardEntry {
            //     PlayerName = e.User.Name,
            //     Score = e.Score / 1000f,
            //     Rank = e.GlobalRank
            // }).ToArray();
            // OnLeaderboardLoaded?.Invoke(results);

            Debug.Log($"[Leaderboard] Steam load stub: {leaderboardId}");
            OnLeaderboardLoaded?.Invoke(new LeaderboardEntry[0]);
        }

        private void LoadLeaderboardItchIO(string leaderboardId, int count)
        {
            // ITCH.IO RETRIEVAL STUB
            Debug.Log($"[Leaderboard] itch.io load stub: {leaderboardId}");
            OnLeaderboardLoaded?.Invoke(new LeaderboardEntry[0]);
        }

        private void LoadLeaderboardLocal(LeaderboardType type, int count)
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
            switch (_activePlatform)
            {
                case LeaderboardPlatform.Local:
                    var entries = GetLocalEntries(type);
                    string playerName = GetLocalPlayerName();
                    int rank = entries.FindIndex(e => e.PlayerName == playerName) + 1;
                    OnRankReceived?.Invoke(rank > 0 ? rank : -1);
                    break;
                default:
                    // Platform stubs would query server
                    OnRankReceived?.Invoke(-1);
                    break;
            }
        }

        #endregion

        #region Achievement Reporting

        /// <summary>
        /// Report an achievement unlock to the platform.
        /// </summary>
        public void ReportAchievement(string achievementId)
        {
            switch (_activePlatform)
            {
                case LeaderboardPlatform.Steam:
                    ReportAchievementSteam(achievementId);
                    break;
                default:
                    Debug.Log($"[Leaderboard] Achievement reported: {achievementId}");
                    break;
            }
        }

        private void ReportAchievementSteam(string achievementId)
        {
            // STEAM ACHIEVEMENT STUB
            // Example:
            // var achievement = new Steamworks.Data.Achievement(achievementId);
            // achievement.Trigger();

            Debug.Log($"[Leaderboard] Steam achievement stub: {achievementId}");
        }

        #endregion

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
            // Could be customizable in settings
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
    /// Supported leaderboard platforms.
    /// </summary>
    public enum LeaderboardPlatform
    {
        Local,
        Steam,
        ItchIO
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
        public DateTime Timestamp;
        public GameMode GameMode;
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
