using UnityEngine;

namespace Shredsquatch.Core
{
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        GameOver
    }

    public enum GameMode
    {
        Standard,
        Avalanche,
        Storm
    }

    public enum TimeOfDay
    {
        Dawn,      // 0-3km
        Midday,    // 3-6km
        Dusk,      // 6-10km
        Night      // 10km+
    }

    [System.Serializable]
    public class RunStats
    {
        public float Distance;
        public int TrickCount;
        public int TrickScore;
        public float MaxSpeed;
        public int MaxCombo;
        public int CoinsCollected;
        public float RunTime;

        public void Reset()
        {
            Distance = 0f;
            TrickCount = 0;
            TrickScore = 0;
            MaxSpeed = 0f;
            MaxCombo = 0;
            CoinsCollected = 0;
            RunTime = 0f;
        }

        public float CalculateTotalScore()
        {
            // Total Run Score: Distance x (1 + Tricks/10000)
            return Distance * (1f + TrickScore / 10000f);
        }
    }

    [System.Serializable]
    public class PlayerProgress
    {
        public float BestDistance;
        public int BestTrickScore;
        public bool NightModeUnlocked;      // 10km
        public bool AvalancheModeUnlocked;  // 15km
        public bool StormModeUnlocked;      // 20km
        public bool GoldenSasquatchUnlocked; // 30km
        public string[] UnlockedSkins;
        public string[] UnlockedTrails;

        public void CheckUnlocks(float distance)
        {
            if (distance > BestDistance)
            {
                BestDistance = distance;
            }

            // Check distance-based unlocks
            if (distance >= 10f) NightModeUnlocked = true;
            if (distance >= 15f) AvalancheModeUnlocked = true;
            if (distance >= 20f) StormModeUnlocked = true;
            if (distance >= 30f) GoldenSasquatchUnlocked = true;
        }
    }
}
