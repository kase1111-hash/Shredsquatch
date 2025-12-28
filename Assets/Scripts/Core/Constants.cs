namespace Shredsquatch.Core
{
    public static class Constants
    {
        // Speed (km/h)
        public static class Speed
        {
            public const float Cruise = 50f;
            public const float TuckMin = 80f;
            public const float TuckMax = 120f;
            public const float CarveBoost = 5f;        // Per sustained turn
            public const float PowderDrag = 0.8f;      // 20% reduction
            public const float WipeoutRecovery = 30f;
            public const float CrashThreshold = 50f;   // Speed for full ragdoll
        }

        // Carving
        public static class Carving
        {
            public const float NormalMaxAngle = 30f;
            public const float CarveMinAngle = 30f;
            public const float CarveMaxAngle = 45f;
            public const float EdgeCatchAngle = 45f;
        }

        // Jumping
        public static class Jump
        {
            public const float BaseHeight = 2f;
            public const float ChargeTimeMax = 1.5f;
            public const float ChargeBonus = 0.5f;     // +50% at full charge
            public const float LandingAngleClean = 15f;
            public const float LandingAngleMax = 30f;
        }

        // Airtime windows (seconds)
        public static class Airtime
        {
            public const float NoTrickMax = 0.5f;
            public const float BasicTrickMax = 1.5f;
            public const float ComboTrickMax = 3.0f;
        }

        // Crash/Recovery
        public static class Crash
        {
            public const float RagdollMinDuration = 2f;
            public const float RagdollMaxDuration = 4f;
            public const float RecoveryTime = 1.5f;
            public const float InvincibilityTime = 2f;
            public const float TumbleDistanceBase = 50f;
            public const float TumbleDistanceMax = 100f;
        }

        // Hitboxes
        public static class Hitbox
        {
            public const float PlayerRadius = 0.5f;
            public const float TreeRadiusMin = 0.3f;
            public const float TreeRadiusMax = 0.8f;
            public const float RockSizeMin = 0.5f;
            public const float RockSizeMax = 2f;
            public const float GrazingThreshold = 0.1f;
            public const float PowerupMagnetRadius = 1.5f;
        }

        // Sasquatch
        public static class Sasquatch
        {
            public const float SpawnDistance = 5f;     // km
            public const float BaseSpeed = 90f;        // km/h
            public const float TargetDistance = 400f;  // m
            public const float FarThreshold = 800f;    // m
            public const float CloseThreshold = 200f;  // m
            public const float BurstSpeedMod = 1.3f;   // +30%
            public const float TiredSpeedMod = 0.8f;   // -20%
        }

        // Scoring
        public static class Score
        {
            public const int BasicJump = 100;
            public const int CleanLandBonus = 500;
            public const int CoinValue = 50;
            public const int RailOllieBonus = 200;
            public const int RailTransferMultiplier = 2;
            public const int RailSpinEntry = 500;
            public const int RailGrabBonus = 300;
        }

        // Combo
        public static class Combo
        {
            public const float ChainWindow = 1f;       // seconds to chain
            public const float MaxMultiplier = 3f;
            public const float RepeatPenalty50 = 0.5f;
            public const float RepeatPenalty25 = 0.25f;
            public const float RepeatPenaltyMin = 0.1f;
        }

        // Terrain chunks
        public static class Terrain
        {
            public const float ChunkSize = 1024f;      // meters
            public const float LoadAhead = 2000f;      // 2km
        }

        // Powerup durations (seconds)
        public static class Powerup
        {
            public const float GoldenBoardDuration = 10f;
            public const float NitroDuration = 5f;
            public const float NitroBoost = 50f;       // km/h
            public const float RepellentDuration = 15f;
            public const float RepellentSlowdown = 0.5f;
            public const float ComboMagnetRadius = 5f;
        }

        // Rail grinding
        public static class Rail
        {
            public const float EntryTolerance = 0.5f;  // meters from center
            public const float BalanceDecayTime = 2f;  // seconds without input
            public const float AccelerationRate = 5f;  // km/h per second
            public const float MaxGrindSpeed = 80f;    // km/h
        }

        // Visibility (meters)
        public static class Visibility
        {
            public const float Dawn = 150f;
            public const float Midday = 200f;
            public const float Dusk = 120f;
            public const float Night = 80f;
            public const float NightNoHeadlamp = 40f;
            public const float HeadlampCone = 40f;
            public const float HeadlampPeripheral = 20f;
            public const float RearFog = 50f;
        }
    }
}
