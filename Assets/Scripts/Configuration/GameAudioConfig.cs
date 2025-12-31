using UnityEngine;

namespace Shredsquatch.Configuration
{
    /// <summary>
    /// ScriptableObject containing all audio clip references.
    /// Artists can assign real audio files here to replace placeholders.
    /// </summary>
    [CreateAssetMenu(fileName = "GameAudioConfig", menuName = "Shredsquatch/Audio Config")]
    public class GameAudioConfig : ScriptableObject
    {
        [Header("Music")]
        [Tooltip("Main menu background music")]
        public AudioClip MenuMusic;

        [Tooltip("Gameplay music (before Sasquatch spawns)")]
        public AudioClip GameplayMusic;

        [Tooltip("Intense chase music (after Sasquatch spawns)")]
        public AudioClip ChaseMusic;

        [Header("Player SFX")]
        [Tooltip("Sound when player jumps")]
        public AudioClip JumpSound;

        [Tooltip("Sound when player lands")]
        public AudioClip LandSound;

        [Tooltip("Sound when player crashes")]
        public AudioClip CrashSound;

        [Tooltip("Sound when carving/turning")]
        public AudioClip CarvingSound;

        [Tooltip("Sound when grinding rails")]
        public AudioClip GrindingSound;

        [Header("Trick SFX")]
        [Tooltip("Sound when completing a trick")]
        public AudioClip TrickCompleteSound;

        [Tooltip("Sound for combo multiplier increase")]
        public AudioClip ComboSound;

        [Tooltip("Sound when combo ends/breaks")]
        public AudioClip ComboBreakSound;

        [Header("Collectible SFX")]
        [Tooltip("Sound when collecting a coin")]
        public AudioClip CoinSound;

        [Tooltip("Sound when collecting a powerup")]
        public AudioClip PowerupSound;

        [Tooltip("Sound when powerup activates")]
        public AudioClip PowerupActivateSound;

        [Tooltip("Sound when powerup expires")]
        public AudioClip PowerupExpireSound;

        [Header("Sasquatch SFX")]
        [Tooltip("Sasquatch spawn roar")]
        public AudioClip SasquatchRoar;

        [Tooltip("Sasquatch footsteps")]
        public AudioClip SasquatchFootsteps;

        [Tooltip("Sasquatch catch/death sound")]
        public AudioClip SasquatchCatch;

        [Tooltip("Sasquatch proximity growl (when close)")]
        public AudioClip SasquatchGrowl;

        [Header("Ambience")]
        [Tooltip("Wind ambience loop")]
        public AudioClip WindAmbience;

        [Tooltip("Blizzard/storm ambience")]
        public AudioClip BlizzardAmbience;

        [Tooltip("Night ambience")]
        public AudioClip NightAmbience;

        [Header("UI SFX")]
        [Tooltip("Menu button click")]
        public AudioClip UIClick;

        [Tooltip("Menu button hover")]
        public AudioClip UIHover;

        [Tooltip("Achievement unlock fanfare")]
        public AudioClip AchievementSound;

        [Tooltip("Game over sound")]
        public AudioClip GameOverSound;

        [Tooltip("New high score sound")]
        public AudioClip HighScoreSound;

        /// <summary>
        /// Check if all required audio clips are assigned.
        /// </summary>
        public bool ValidateConfig(out string[] missingClips)
        {
            var missing = new System.Collections.Generic.List<string>();

            if (MenuMusic == null) missing.Add("MenuMusic");
            if (GameplayMusic == null) missing.Add("GameplayMusic");
            if (ChaseMusic == null) missing.Add("ChaseMusic");
            if (JumpSound == null) missing.Add("JumpSound");
            if (LandSound == null) missing.Add("LandSound");
            if (CrashSound == null) missing.Add("CrashSound");
            if (CoinSound == null) missing.Add("CoinSound");
            if (SasquatchRoar == null) missing.Add("SasquatchRoar");
            if (WindAmbience == null) missing.Add("WindAmbience");
            if (AchievementSound == null) missing.Add("AchievementSound");

            missingClips = missing.ToArray();
            return missing.Count == 0;
        }
    }
}
