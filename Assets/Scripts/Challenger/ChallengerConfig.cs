using UnityEngine;

namespace Shredsquatch.Challenger
{
    /// <summary>
    /// Configuration for alternate challenger mode.
    /// [PLACEHOLDER] - Asset references pending external approval.
    /// </summary>
    [CreateAssetMenu(fileName = "ChallengerConfig", menuName = "Shredsquatch/Challenger Config")]
    public class ChallengerConfig : ScriptableObject
    {
        [Header("Mode Settings")]
        [Tooltip("Is this mode available? Requires external permission for certain content.")]
        public bool ModeEnabled = false;

        [Tooltip("Unlock code - hidden activation sequence")]
        public string UnlockSequence = ""; // Set externally

        [Header("Character")]
        [Tooltip("Challenger character prefab (placeholder until approved)")]
        public GameObject CharacterPrefab;

        [Tooltip("Character display name")]
        public string CharacterName = "Friendly Yeti";

        [Tooltip("Character description")]
        public string CharacterDescription = "A cheerful mountain dweller";

        [Header("Mount")]
        [Tooltip("Creature mount prefab (replaces snowboard)")]
        public GameObject MountPrefab;

        [Tooltip("Mount display name")]
        public string MountName = "Snow Buddy";

        [Tooltip("Mount type for animations")]
        public MountType MountStyle = MountType.Waddle;

        [Header("Gameplay")]
        [Tooltip("Starting trick energy")]
        public float StartingEnergy = 100f;

        [Tooltip("Energy drain per second")]
        public float EnergyDrainRate = 5f;

        [Tooltip("Energy gained per trick point")]
        public float EnergyPerTrickPoint = 0.1f;

        [Tooltip("Minimum trick score to gain energy")]
        public int MinTrickScore = 100;

        [Tooltip("Grace period at start (seconds)")]
        public float GracePeriod = 10f;

        [Header("Visuals")]
        [Tooltip("Trail effect for mount")]
        public GameObject MountTrailPrefab;

        [Tooltip("Poof effect when energy depletes")]
        public GameObject PoofEffectPrefab;

        [Tooltip("Character color tint")]
        public Color CharacterTint = Color.white;

        [Header("Audio")]
        public AudioClip CharacterVoice;
        public AudioClip MountSound;
        public AudioClip PoofSound;

        [Header("Attribution")]
        [Tooltip("Creator credit - REQUIRED when content is approved")]
        public string CreatorCredit = "";

        [Tooltip("Link to creator")]
        public string CreatorLink = "";

        /// <summary>
        /// Check if mode can be activated.
        /// </summary>
        public bool CanActivate()
        {
            return ModeEnabled && CharacterPrefab != null && MountPrefab != null;
        }

        /// <summary>
        /// Get attribution text for credits.
        /// </summary>
        public string GetAttributionText()
        {
            if (string.IsNullOrEmpty(CreatorCredit))
                return null;

            return $"Character inspired by work of {CreatorCredit}. Used with permission.";
        }
    }

    public enum MountType
    {
        Waddle,     // Penguin-style movement
        Slide,      // Belly slide
        Hop,        // Bouncy movement
        Glide       // Smooth glide
    }
}
