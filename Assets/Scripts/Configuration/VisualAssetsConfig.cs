using UnityEngine;

namespace Shredsquatch.Configuration
{
    /// <summary>
    /// ScriptableObject containing all visual asset references.
    /// Artists can assign real 3D models, materials, and prefabs here.
    /// </summary>
    [CreateAssetMenu(fileName = "VisualAssetsConfig", menuName = "Shredsquatch/Visual Assets Config")]
    public class VisualAssetsConfig : ScriptableObject
    {
        [Header("Player Assets")]
        [Tooltip("First-person snowboard model (visible in view)")]
        public GameObject SnowboardModel;

        [Tooltip("First-person hands/arms model")]
        public GameObject HandsModel;

        [Tooltip("Board trail particle prefab")]
        public GameObject BoardTrailPrefab;

        [Header("Sasquatch Assets")]
        [Tooltip("Main Sasquatch character prefab")]
        public GameObject SasquatchPrefab;

        [Tooltip("Sasquatch eye glow material")]
        public Material SasquatchEyeGlowMaterial;

        [Tooltip("Sasquatch snow trail particle prefab")]
        public GameObject SasquatchTrailPrefab;

        [Header("Sasquatch Skins")]
        [Tooltip("Default Sasquatch material")]
        public Material SasquatchDefaultSkin;

        [Tooltip("Classic Yeti skin (5km unlock)")]
        public Material SasquatchYetiSkin;

        [Tooltip("Abominable skin (5km unlock)")]
        public Material SasquatchAbominableSkin;

        [Tooltip("Golden Sasquatch skin (30km unlock)")]
        public Material SasquatchGoldenSkin;

        [Header("Tree Prefabs")]
        [Tooltip("Pine tree variants")]
        public GameObject[] PineTreePrefabs;

        [Tooltip("Birch tree variants")]
        public GameObject[] BirchTreePrefabs;

        [Tooltip("Dead tree variants")]
        public GameObject[] DeadTreePrefabs;

        [Tooltip("Fallen log variants")]
        public GameObject[] FallenLogPrefabs;

        [Header("Rock Prefabs")]
        [Tooltip("Boulder variants")]
        public GameObject[] BoulderPrefabs;

        [Tooltip("Rock outcrop variants")]
        public GameObject[] RockOutcropPrefabs;

        [Header("Ramp Prefabs")]
        [Tooltip("Small bump/mound")]
        public GameObject SmallBumpPrefab;

        [Tooltip("Medium snow ramp")]
        public GameObject MediumRampPrefab;

        [Tooltip("Large kicker")]
        public GameObject LargeKickerPrefab;

        [Tooltip("Half-pipe section")]
        public GameObject HalfPipePrefab;

        [Tooltip("Cliff jump")]
        public GameObject CliffPrefab;

        [Header("Rail Prefabs")]
        [Tooltip("Fallen pine rail")]
        public GameObject FallenPineRailPrefab;

        [Tooltip("Fence rail")]
        public GameObject FenceRailPrefab;

        [Tooltip("Log pile")]
        public GameObject LogPilePrefab;

        [Tooltip("Metal barrier")]
        public GameObject MetalBarrierPrefab;

        [Tooltip("Cabin ridge")]
        public GameObject CabinRidgePrefab;

        [Tooltip("Pipe rail")]
        public GameObject PipeRailPrefab;

        [Header("Chairlift Assets")]
        [Tooltip("Chairlift tower prefab")]
        public GameObject ChairliftTowerPrefab;

        [Tooltip("Chairlift chair prefab")]
        public GameObject ChairliftChairPrefab;

        [Tooltip("Chairlift cable material")]
        public Material ChairliftCableMaterial;

        [Header("Collectible Prefabs")]
        [Tooltip("Coin prefab")]
        public GameObject CoinPrefab;

        [Tooltip("Golden board powerup prefab")]
        public GameObject GoldenBoardPrefab;

        [Tooltip("Nitro boost prefab")]
        public GameObject NitroPrefab;

        [Tooltip("Repellent cloud prefab")]
        public GameObject RepellentPrefab;

        [Header("Particle Effects")]
        [Tooltip("Snow spray when carving")]
        public GameObject SnowSprayPrefab;

        [Tooltip("Powder explosion on crash")]
        public GameObject PowderExplosionPrefab;

        [Tooltip("Coin collect effect")]
        public GameObject CoinCollectEffectPrefab;

        [Tooltip("Powerup collect effect")]
        public GameObject PowerupCollectEffectPrefab;

        [Header("Board Trails")]
        [Tooltip("Default trail")]
        public GameObject TrailDefaultPrefab;

        [Tooltip("Fire trail (15km unlock)")]
        public GameObject TrailFirePrefab;

        [Tooltip("Rainbow trail (20km unlock)")]
        public GameObject TrailRainbowPrefab;

        [Tooltip("Lightning trail (25km unlock)")]
        public GameObject TrailLightningPrefab;

        [Header("Environment Materials")]
        [Tooltip("Snow terrain material")]
        public Material SnowMaterial;

        [Tooltip("Snow with tracks material")]
        public Material SnowTracksMaterial;

        [Tooltip("Ice material")]
        public Material IceMaterial;

        [Header("Skybox Materials")]
        [Tooltip("Dawn skybox")]
        public Material SkyboxDawn;

        [Tooltip("Midday skybox")]
        public Material SkyboxMidday;

        [Tooltip("Dusk skybox")]
        public Material SkyboxDusk;

        [Tooltip("Night skybox (with stars)")]
        public Material SkyboxNight;

        [Tooltip("Aurora borealis effect prefab")]
        public GameObject AuroraPrefab;

        /// <summary>
        /// Check if minimum required assets are assigned.
        /// </summary>
        public bool ValidateMinimumAssets(out string[] missingAssets)
        {
            var missing = new System.Collections.Generic.List<string>();

            if (SasquatchPrefab == null) missing.Add("SasquatchPrefab");
            if (CoinPrefab == null) missing.Add("CoinPrefab");
            if (SnowMaterial == null) missing.Add("SnowMaterial");

            if (PineTreePrefabs == null || PineTreePrefabs.Length == 0)
                missing.Add("PineTreePrefabs (at least 1)");

            if (BoulderPrefabs == null || BoulderPrefabs.Length == 0)
                missing.Add("BoulderPrefabs (at least 1)");

            missingAssets = missing.ToArray();
            return missing.Count == 0;
        }

        /// <summary>
        /// Get a random tree prefab.
        /// </summary>
        public GameObject GetRandomTree(System.Random rng = null)
        {
            var allTrees = new System.Collections.Generic.List<GameObject>();

            if (PineTreePrefabs != null) allTrees.AddRange(PineTreePrefabs);
            if (BirchTreePrefabs != null) allTrees.AddRange(BirchTreePrefabs);
            if (DeadTreePrefabs != null) allTrees.AddRange(DeadTreePrefabs);

            if (allTrees.Count == 0) return null;

            int index = rng != null ? rng.Next(allTrees.Count) : Random.Range(0, allTrees.Count);
            return allTrees[index];
        }

        /// <summary>
        /// Get a random rock prefab.
        /// </summary>
        public GameObject GetRandomRock(System.Random rng = null)
        {
            var allRocks = new System.Collections.Generic.List<GameObject>();

            if (BoulderPrefabs != null) allRocks.AddRange(BoulderPrefabs);
            if (RockOutcropPrefabs != null) allRocks.AddRange(RockOutcropPrefabs);

            if (allRocks.Count == 0) return null;

            int index = rng != null ? rng.Next(allRocks.Count) : Random.Range(0, allRocks.Count);
            return allRocks[index];
        }
    }
}
