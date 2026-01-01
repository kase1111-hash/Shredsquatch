using UnityEngine;

namespace Shredsquatch.Configuration
{
    /// <summary>
    /// Central registry of all game prefabs.
    /// Assign prefabs here and the game systems will use them.
    /// </summary>
    [CreateAssetMenu(fileName = "PrefabRegistry", menuName = "Shredsquatch/Prefab Registry")]
    public class PrefabRegistry : ScriptableObject
    {
        public static PrefabRegistry Instance { get; private set; }

        [Header("Core Prefabs")]
        [Tooltip("Player prefab with all player components")]
        public GameObject PlayerPrefab;

        [Tooltip("Sasquatch enemy prefab")]
        public GameObject SasquatchPrefab;

        [Tooltip("Terrain chunk prefab for procedural generation")]
        public GameObject TerrainChunkPrefab;

        [Header("Tree Prefabs")]
        public GameObject[] PineTrees;
        public GameObject[] BirchTrees;
        public GameObject[] DeadTrees;
        public GameObject[] FallenTrees;

        [Header("Rock Prefabs")]
        public GameObject[] LargeRocks;
        public GameObject[] SmallRocks;

        [Header("Ramp Prefabs")]
        public GameObject SmallRamp;
        public GameObject MediumRamp;
        public GameObject LargeRamp;
        public GameObject CliffRamp;

        [Header("Rail Prefabs")]
        public GameObject FenceRail;
        public GameObject PipeRail;
        public GameObject MetalRail;

        [Header("Collectible Prefabs")]
        public GameObject CoinPrefab;

        [Header("Powerup Prefabs")]
        public GameObject NitroPowerup;
        public GameObject GoldenBoardPowerup;
        public GameObject RepellentPowerup;

        [Header("Environment Prefabs")]
        public GameObject ChairliftTower;
        public GameObject ChairliftChair;

        [Header("Effect Prefabs")]
        public GameObject SnowSprayEffect;
        public GameObject CrashEffect;
        public GameObject CoinCollectEffect;

        private void OnEnable()
        {
            Instance = this;
        }

        /// <summary>
        /// Get all tree prefabs as a single array.
        /// </summary>
        public GameObject[] GetAllTrees()
        {
            var trees = new System.Collections.Generic.List<GameObject>();
            if (PineTrees != null) trees.AddRange(PineTrees);
            if (BirchTrees != null) trees.AddRange(BirchTrees);
            if (DeadTrees != null) trees.AddRange(DeadTrees);
            return trees.ToArray();
        }

        /// <summary>
        /// Get all rock prefabs as a single array.
        /// </summary>
        public GameObject[] GetAllRocks()
        {
            var rocks = new System.Collections.Generic.List<GameObject>();
            if (LargeRocks != null) rocks.AddRange(LargeRocks);
            if (SmallRocks != null) rocks.AddRange(SmallRocks);
            return rocks.ToArray();
        }

        /// <summary>
        /// Get all ramp prefabs as a single array.
        /// </summary>
        public GameObject[] GetAllRamps()
        {
            var ramps = new System.Collections.Generic.List<GameObject>();
            if (SmallRamp != null) ramps.Add(SmallRamp);
            if (MediumRamp != null) ramps.Add(MediumRamp);
            if (LargeRamp != null) ramps.Add(LargeRamp);
            if (CliffRamp != null) ramps.Add(CliffRamp);
            return ramps.ToArray();
        }

        /// <summary>
        /// Get all rail prefabs as a single array.
        /// </summary>
        public GameObject[] GetAllRails()
        {
            var rails = new System.Collections.Generic.List<GameObject>();
            if (FenceRail != null) rails.Add(FenceRail);
            if (PipeRail != null) rails.Add(PipeRail);
            if (MetalRail != null) rails.Add(MetalRail);
            return rails.ToArray();
        }

        /// <summary>
        /// Get all powerup prefabs as a single array.
        /// </summary>
        public GameObject[] GetAllPowerups()
        {
            var powerups = new System.Collections.Generic.List<GameObject>();
            if (NitroPowerup != null) powerups.Add(NitroPowerup);
            if (GoldenBoardPowerup != null) powerups.Add(GoldenBoardPowerup);
            if (RepellentPowerup != null) powerups.Add(RepellentPowerup);
            return powerups.ToArray();
        }

        /// <summary>
        /// Validate that required prefabs are assigned.
        /// </summary>
        public bool Validate(out string[] missingPrefabs)
        {
            var missing = new System.Collections.Generic.List<string>();

            if (PlayerPrefab == null) missing.Add("PlayerPrefab");
            if (SasquatchPrefab == null) missing.Add("SasquatchPrefab");
            if (CoinPrefab == null) missing.Add("CoinPrefab");

            if (PineTrees == null || PineTrees.Length == 0)
                missing.Add("PineTrees (at least 1)");

            if (LargeRocks == null || LargeRocks.Length == 0)
                missing.Add("LargeRocks (at least 1)");

            missingPrefabs = missing.ToArray();
            return missing.Count == 0;
        }
    }
}
