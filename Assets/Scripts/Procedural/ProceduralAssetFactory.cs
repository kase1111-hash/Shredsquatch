using UnityEngine;
using Shredsquatch.Configuration;
using Shredsquatch.Player;
using Shredsquatch.Tricks;
using Shredsquatch.Sasquatch;

namespace Shredsquatch.Procedural
{
    /// <summary>
    /// Factory for creating game objects with procedural meshes.
    /// Use this to spawn placeholder assets until real 3D models are available.
    /// </summary>
    public class ProceduralAssetFactory : MonoBehaviour
    {
        public static ProceduralAssetFactory Instance { get; private set; }

        [Header("Materials")]
        [SerializeField] private Material _treeTrunkMaterial;
        [SerializeField] private Material _treeFoliageMaterial;
        [SerializeField] private Material _rockMaterial;
        [SerializeField] private Material _snowMaterial;
        [SerializeField] private Material _metalMaterial;
        [SerializeField] private Material _woodMaterial;
        [SerializeField] private Material _coinMaterial;

        // Cached meshes
        private Mesh _pineTreeMesh;
        private Mesh _deadTreeMesh;
        private Mesh _fallenLogMesh;
        private Mesh _boulderMesh;
        private Mesh _rockOutcropMesh;
        private Mesh _rampMesh;
        private Mesh _fenceRailMesh;
        private Mesh _pipeRailMesh;
        private Mesh _coinMesh;
        private Mesh _chairliftTowerMesh;
        private Mesh _chairliftChairMesh;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            CreateDefaultMaterials();
            GenerateMeshes();
        }

        private void CreateDefaultMaterials()
        {
            // Create simple colored materials if not assigned
            if (_treeTrunkMaterial == null)
            {
                _treeTrunkMaterial = CreateMaterial(new Color(0.4f, 0.25f, 0.1f)); // Brown
            }
            if (_treeFoliageMaterial == null)
            {
                _treeFoliageMaterial = CreateMaterial(new Color(0.1f, 0.4f, 0.15f)); // Dark green
            }
            if (_rockMaterial == null)
            {
                _rockMaterial = CreateMaterial(new Color(0.5f, 0.5f, 0.5f)); // Gray
            }
            if (_snowMaterial == null)
            {
                _snowMaterial = CreateMaterial(new Color(0.95f, 0.97f, 1f)); // White-ish
            }
            if (_metalMaterial == null)
            {
                _metalMaterial = CreateMaterial(new Color(0.6f, 0.6f, 0.65f)); // Metal gray
            }
            if (_woodMaterial == null)
            {
                _woodMaterial = CreateMaterial(new Color(0.55f, 0.35f, 0.15f)); // Wood brown
            }
            if (_coinMaterial == null)
            {
                _coinMaterial = CreateMaterial(new Color(1f, 0.85f, 0.1f)); // Gold
            }
        }

        private Material CreateMaterial(Color color)
        {
            // Try to use URP Lit shader, fall back to Standard
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            var mat = new Material(shader);
            mat.color = color;
            return mat;
        }

        private void GenerateMeshes()
        {
            _pineTreeMesh = ProceduralMeshGenerator.GeneratePineTree();
            _deadTreeMesh = ProceduralMeshGenerator.GenerateDeadTree();
            _fallenLogMesh = ProceduralMeshGenerator.GenerateFallenLog();
            _boulderMesh = ProceduralMeshGenerator.GenerateBoulder();
            _rockOutcropMesh = ProceduralMeshGenerator.GenerateRockOutcrop();
            _rampMesh = ProceduralMeshGenerator.GenerateRamp();
            _fenceRailMesh = ProceduralMeshGenerator.GenerateFenceRail();
            _pipeRailMesh = ProceduralMeshGenerator.GeneratePipeRail();
            _coinMesh = ProceduralMeshGenerator.GenerateCoin();
            _chairliftTowerMesh = ProceduralMeshGenerator.GenerateChairliftTower();
            _chairliftChairMesh = ProceduralMeshGenerator.GenerateChairliftChair();
        }

        #region Tree Spawning

        /// <summary>
        /// Create a pine tree at the given position.
        /// </summary>
        public GameObject CreatePineTree(Vector3 position, Transform parent = null)
        {
            var go = new GameObject("PineTree_Procedural");
            go.transform.position = position;
            go.transform.parent = parent;

            var mf = go.AddComponent<MeshFilter>();
            mf.mesh = _pineTreeMesh;

            var mr = go.AddComponent<MeshRenderer>();
            mr.materials = new Material[] { _treeTrunkMaterial, _treeFoliageMaterial };

            // Add collider for collision detection
            var capsule = go.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0, 3f, 0);
            capsule.radius = 0.3f;
            capsule.height = 6f;

            go.tag = "Tree";

            return go;
        }

        /// <summary>
        /// Create a dead tree at the given position.
        /// </summary>
        public GameObject CreateDeadTree(Vector3 position, Transform parent = null)
        {
            var go = new GameObject("DeadTree_Procedural");
            go.transform.position = position;
            go.transform.parent = parent;

            var mf = go.AddComponent<MeshFilter>();
            mf.mesh = _deadTreeMesh;

            var mr = go.AddComponent<MeshRenderer>();
            mr.material = _treeTrunkMaterial;

            var capsule = go.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0, 2.5f, 0);
            capsule.radius = 0.2f;
            capsule.height = 5f;

            go.tag = "Tree";

            return go;
        }

        /// <summary>
        /// Create a fallen log at the given position.
        /// </summary>
        public GameObject CreateFallenLog(Vector3 position, float rotation = 0f, Transform parent = null)
        {
            var go = new GameObject("FallenLog_Procedural");
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0, rotation, 0);
            go.transform.parent = parent;

            var mf = go.AddComponent<MeshFilter>();
            mf.mesh = _fallenLogMesh;

            var mr = go.AddComponent<MeshRenderer>();
            mr.material = _treeTrunkMaterial;

            var box = go.AddComponent<BoxCollider>();
            box.center = new Vector3(0, 0.3f, 4f);
            box.size = new Vector3(0.6f, 0.6f, 8f);
            box.isTrigger = true;

            go.tag = "Rail";
            go.name = "FallenPine_Procedural";

            return go;
        }

        #endregion

        #region Rock Spawning

        /// <summary>
        /// Create a boulder at the given position.
        /// </summary>
        public GameObject CreateBoulder(Vector3 position, float scale = 1f, Transform parent = null)
        {
            var go = new GameObject("Boulder_Procedural");
            go.transform.position = position;
            go.transform.localScale = Vector3.one * scale;
            go.transform.parent = parent;

            var mf = go.AddComponent<MeshFilter>();
            mf.mesh = _boulderMesh;

            var mr = go.AddComponent<MeshRenderer>();
            mr.material = _rockMaterial;

            var sphere = go.AddComponent<SphereCollider>();
            sphere.radius = scale;

            go.tag = "Rock";

            return go;
        }

        /// <summary>
        /// Create a rock outcrop at the given position.
        /// </summary>
        public GameObject CreateRockOutcrop(Vector3 position, float scale = 1f, Transform parent = null)
        {
            var go = new GameObject("RockOutcrop_Procedural");
            go.transform.position = position;
            go.transform.localScale = Vector3.one * scale;
            go.transform.parent = parent;

            var mf = go.AddComponent<MeshFilter>();
            mf.mesh = _rockOutcropMesh;

            var mr = go.AddComponent<MeshRenderer>();
            mr.material = _rockMaterial;

            var box = go.AddComponent<BoxCollider>();
            box.size = new Vector3(2f, 1f, 1.5f) * scale;

            go.tag = "Rock";

            return go;
        }

        #endregion

        #region Ramp Spawning

        /// <summary>
        /// Create a snow ramp at the given position.
        /// </summary>
        public GameObject CreateRamp(Vector3 position, float rotation = 0f, Transform parent = null)
        {
            var go = new GameObject("Ramp_Procedural");
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0, rotation, 0);
            go.transform.parent = parent;

            var mf = go.AddComponent<MeshFilter>();
            mf.mesh = _rampMesh;

            var mr = go.AddComponent<MeshRenderer>();
            mr.material = _snowMaterial;

            var mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = _rampMesh;

            go.tag = "Ramp";

            return go;
        }

        #endregion

        #region Rail Spawning

        /// <summary>
        /// Create a fence rail at the given position.
        /// </summary>
        public GameObject CreateFenceRail(Vector3 position, float rotation = 0f, Transform parent = null)
        {
            var go = new GameObject("FenceRail_Procedural");
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0, rotation, 0);
            go.transform.parent = parent;

            var mf = go.AddComponent<MeshFilter>();
            mf.mesh = _fenceRailMesh;

            var mr = go.AddComponent<MeshRenderer>();
            mr.material = _woodMaterial;

            var box = go.AddComponent<BoxCollider>();
            box.center = new Vector3(0, 0.4f, 5f);
            box.size = new Vector3(0.2f, 0.2f, 10f);
            box.isTrigger = true;

            go.tag = "Rail";
            go.name = "Fence_Procedural";

            return go;
        }

        /// <summary>
        /// Create a metal pipe rail at the given position.
        /// </summary>
        public GameObject CreatePipeRail(Vector3 position, float rotation = 0f, Transform parent = null)
        {
            var go = new GameObject("PipeRail_Procedural");
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0, rotation, 0);
            go.transform.parent = parent;

            var mf = go.AddComponent<MeshFilter>();
            mf.mesh = _pipeRailMesh;

            var mr = go.AddComponent<MeshRenderer>();
            mr.material = _metalMaterial;

            var box = go.AddComponent<BoxCollider>();
            box.center = new Vector3(0, 0.5f, 7.5f);
            box.size = new Vector3(0.2f, 0.2f, 15f);
            box.isTrigger = true;

            go.tag = "Rail";
            go.name = "Pipe_Procedural";

            return go;
        }

        #endregion

        #region Collectible Spawning

        /// <summary>
        /// Create a coin at the given position.
        /// </summary>
        public GameObject CreateCoin(Vector3 position, Transform parent = null)
        {
            var go = new GameObject("Coin_Procedural");
            go.transform.position = position;
            go.transform.parent = parent;

            var mf = go.AddComponent<MeshFilter>();
            mf.mesh = _coinMesh;

            var mr = go.AddComponent<MeshRenderer>();
            mr.material = _coinMaterial;

            var sphere = go.AddComponent<SphereCollider>();
            sphere.radius = 0.5f;
            sphere.isTrigger = true;

            // Add spin animation
            var spinner = go.AddComponent<CoinSpinner>();

            go.tag = "Coin";

            return go;
        }

        #endregion

        #region Chairlift Spawning

        /// <summary>
        /// Create a chairlift tower at the given position.
        /// </summary>
        public GameObject CreateChairliftTower(Vector3 position, Transform parent = null)
        {
            var go = new GameObject("ChairliftTower_Procedural");
            go.transform.position = position;
            go.transform.parent = parent;

            var mf = go.AddComponent<MeshFilter>();
            mf.mesh = _chairliftTowerMesh;

            var mr = go.AddComponent<MeshRenderer>();
            mr.material = _metalMaterial;

            var capsule = go.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0, 7.5f, 0);
            capsule.radius = 0.5f;
            capsule.height = 15f;

            return go;
        }

        /// <summary>
        /// Create a chairlift chair at the given position.
        /// </summary>
        public GameObject CreateChairliftChair(Vector3 position, Transform parent = null)
        {
            var go = new GameObject("ChairliftChair_Procedural");
            go.transform.position = position;
            go.transform.parent = parent;

            var mf = go.AddComponent<MeshFilter>();
            mf.mesh = _chairliftChairMesh;

            var mr = go.AddComponent<MeshRenderer>();
            mr.material = _metalMaterial;

            var box = go.AddComponent<BoxCollider>();
            box.center = new Vector3(0, 0.3f, 0.3f);
            box.size = new Vector3(1.5f, 0.8f, 0.6f);

            return go;
        }

        #endregion

        #region Prefab Registry Population

        /// <summary>
        /// Populate a PrefabRegistry with procedural template GameObjects.
        /// Creates deactivated templates that can be Instantiated by TerrainGenerator and SceneInitializer.
        /// </summary>
        public void PopulatePrefabRegistry(PrefabRegistry registry)
        {
            if (registry == null) return;

            var container = new GameObject("_ProceduralTemplates");
            container.SetActive(false);

            // Trees
            if (registry.PineTrees == null || registry.PineTrees.Length == 0)
            {
                registry.PineTrees = new GameObject[]
                {
                    CreatePineTree(Vector3.zero, container.transform)
                };
            }

            if (registry.DeadTrees == null || registry.DeadTrees.Length == 0)
            {
                registry.DeadTrees = new GameObject[]
                {
                    CreateDeadTree(Vector3.zero, container.transform)
                };
            }

            // Rocks
            if (registry.LargeRocks == null || registry.LargeRocks.Length == 0)
            {
                registry.LargeRocks = new GameObject[]
                {
                    CreateBoulder(Vector3.zero, 1f, container.transform)
                };
            }

            if (registry.SmallRocks == null || registry.SmallRocks.Length == 0)
            {
                registry.SmallRocks = new GameObject[]
                {
                    CreateRockOutcrop(Vector3.zero, 1f, container.transform)
                };
            }

            // Ramps
            if (registry.SmallRamp == null)
            {
                registry.SmallRamp = CreateRamp(Vector3.zero, 0f, container.transform);
            }
            if (registry.MediumRamp == null)
            {
                registry.MediumRamp = CreateRamp(Vector3.zero, 0f, container.transform);
                registry.MediumRamp.name = "MediumRamp_Procedural";
            }

            // Rails
            if (registry.FenceRail == null)
            {
                registry.FenceRail = CreateFenceRail(Vector3.zero, 0f, container.transform);
            }
            if (registry.PipeRail == null)
            {
                registry.PipeRail = CreatePipeRail(Vector3.zero, 0f, container.transform);
            }

            // Coins
            if (registry.CoinPrefab == null)
            {
                registry.CoinPrefab = CreateCoin(Vector3.zero, container.transform);
            }

            // Player
            if (registry.PlayerPrefab == null)
            {
                registry.PlayerPrefab = CreatePlayerTemplate(container.transform);
            }

            // Sasquatch
            if (registry.SasquatchPrefab == null)
            {
                registry.SasquatchPrefab = CreateSasquatchTemplate(container.transform);
            }

            Debug.Log("[ProceduralAssetFactory] PrefabRegistry populated with procedural templates");
        }

        private GameObject CreatePlayerTemplate(Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Player_Procedural";
            go.transform.parent = parent;
            go.tag = "Player";

            // Color it blue
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = CreateMaterial(new Color(0.2f, 0.4f, 0.8f));
            }

            // Replace primitive collider with CharacterController
            var primitiveCollider = go.GetComponent<Collider>();
            if (primitiveCollider != null) Destroy(primitiveCollider);

            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0, 0.9f, 0);

            // Core player components (PlayerController's RequireComponent will auto-add
            // PlayerInput, SnowboardPhysics, JumpController, CrashHandler)
            go.AddComponent<PlayerController>();

            // Trick components
            go.AddComponent<TrickController>();
            go.AddComponent<RailGrindController>();

            // Camera as child with first-person controller
            var camObj = new GameObject("PlayerCamera");
            camObj.transform.parent = go.transform;
            camObj.transform.localPosition = new Vector3(0, 1.6f, 0);
            camObj.transform.localRotation = Quaternion.identity;
            camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
            camObj.AddComponent<FirstPersonCamera>();

            return go;
        }

        private GameObject CreateSasquatchTemplate(Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Sasquatch_Procedural";
            go.transform.parent = parent;
            go.transform.localScale = new Vector3(2f, 3f, 2f);

            // Color it dark brown
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = CreateMaterial(new Color(0.3f, 0.15f, 0.05f));
            }

            go.AddComponent<SasquatchAI>();

            // Audio source for roar
            go.AddComponent<AudioSource>();

            return go;
        }

        #endregion
    }

    /// <summary>
    /// Simple component to spin coins.
    /// </summary>
    public class CoinSpinner : MonoBehaviour
    {
        [SerializeField] private float _spinSpeed = 180f;

        private void Update()
        {
            transform.Rotate(0, _spinSpeed * Time.deltaTime, 0);
        }
    }
}
