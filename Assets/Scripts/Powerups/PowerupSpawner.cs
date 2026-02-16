using UnityEngine;
using System.Collections.Generic;
using Shredsquatch.Core;

namespace Shredsquatch.Powerups
{
    public class PowerupSpawner : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject _goldenBoardPrefab;
        [SerializeField] private GameObject _nitroPrefab;
        [SerializeField] private GameObject _repellentPrefab;
        [SerializeField] private GameObject _coinPrefab;

        [Header("Spawn Settings")]
        [SerializeField] private float _spawnHeight = 1.5f;
        [SerializeField] private float _coinLineLength = 50f;
        [SerializeField] private int _coinsPerLine = 10;

        [Header("References")]
        [SerializeField] private Transform _player;

        // Tracking
        private float _lastPowerupDistance;
        private float _lastCoinSpawnZ;
        private List<GameObject> _spawnedPowerups = new List<GameObject>();

        // Seeded random for deterministic powerup spawning (leaderboard fairness)
        private System.Random _seededRandom;

        private void Start()
        {
            // Use a fixed seed for deterministic powerup spawning (leaderboard fairness).
            // All players see the same powerups for the same run.
            _seededRandom = new System.Random(42);
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRunStarted += Reset;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRunStarted -= Reset;
            }
        }

        private void Update()
        {
            if (GameManager.Instance?.CurrentState != GameState.Playing)
                return;

            CheckPowerupSpawn();
            CheckCoinSpawn();
            CleanupDistantPowerups();
        }

        private void CheckPowerupSpawn()
        {
            if (_player == null) return;

            float currentDistance = GameManager.Instance.CurrentRun.Distance;
            float spawnInterval = GetPowerupSpawnInterval(currentDistance);

            if (currentDistance - _lastPowerupDistance >= spawnInterval)
            {
                SpawnRandomPowerup();
                _lastPowerupDistance = currentDistance;
            }
        }

        private void CheckCoinSpawn()
        {
            if (_player == null) return;

            float playerZ = _player.position.z;

            // Spawn coins ahead of player
            if (playerZ > _lastCoinSpawnZ - 200f)
            {
                SpawnCoinLine(_lastCoinSpawnZ + 100f);
                _lastCoinSpawnZ += 100f;
            }
        }

        private float GetPowerupSpawnInterval(float distanceKm)
        {
            // Powerups spawn less frequently as distance increases
            if (distanceKm < 2f) return 0.2f;  // Every 200m
            if (distanceKm < 5f) return 0.3f;  // Every 300m
            return 0.5f;                        // Every 500m
        }

        private float SeededRandomRange(float min, float max)
        {
            return (float)(_seededRandom.NextDouble() * (max - min) + min);
        }

        private void SpawnRandomPowerup()
        {
            if (_player == null) return;

            // Determine powerup type (weighted) using seeded random for leaderboard fairness
            float roll = (float)_seededRandom.NextDouble();
            GameObject prefab;

            if (roll < 0.4f)
            {
                prefab = _goldenBoardPrefab;
            }
            else if (roll < 0.7f)
            {
                prefab = _nitroPrefab;
            }
            else
            {
                // Repellent only spawns after Sasquatch
                if (GameManager.Instance.CurrentRun.Distance >= Constants.Sasquatch.SpawnDistance)
                {
                    prefab = _repellentPrefab;
                }
                else
                {
                    prefab = _goldenBoardPrefab;
                }
            }

            if (prefab == null) return;

            // Spawn position: ahead of player, seeded random X offset
            Vector3 spawnPos = _player.position;
            spawnPos.z += SeededRandomRange(100f, 200f);
            spawnPos.x += SeededRandomRange(-30f, 30f);
            spawnPos.y += _spawnHeight;

            GameObject powerup = Instantiate(prefab, spawnPos, Quaternion.identity);
            _spawnedPowerups.Add(powerup);
        }

        private void SpawnCoinLine(float zPosition)
        {
            if (_coinPrefab == null || _player == null) return;

            // Seeded random X position for the line
            float baseX = _player.position.x + SeededRandomRange(-20f, 20f);

            // Slight curve to the line
            float curveAmount = SeededRandomRange(-0.5f, 0.5f);

            for (int i = 0; i < _coinsPerLine; i++)
            {
                float t = i / (float)(_coinsPerLine - 1);
                float x = baseX + Mathf.Sin(t * Mathf.PI) * curveAmount * 20f;
                float z = zPosition + t * _coinLineLength;

                Vector3 spawnPos = new Vector3(x, _spawnHeight, z);

                // Raycast to find ground
                if (Physics.Raycast(spawnPos + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f))
                {
                    spawnPos.y = hit.point.y + _spawnHeight;
                }

                GameObject coin = Instantiate(_coinPrefab, spawnPos, Quaternion.identity);
                _spawnedPowerups.Add(coin);
            }
        }

        private void CleanupDistantPowerups()
        {
            if (_player == null) return;

            float cleanupDistance = 100f; // Behind player

            for (int i = _spawnedPowerups.Count - 1; i >= 0; i--)
            {
                GameObject powerup = _spawnedPowerups[i];

                if (powerup == null)
                {
                    _spawnedPowerups.RemoveAt(i);
                    continue;
                }

                // Remove if too far behind player
                if (powerup.transform.position.z < _player.position.z - cleanupDistance)
                {
                    Destroy(powerup);
                    _spawnedPowerups.RemoveAt(i);
                }
            }
        }

        private void Reset()
        {
            // Clear all spawned powerups
            foreach (var powerup in _spawnedPowerups)
            {
                if (powerup != null)
                {
                    Destroy(powerup);
                }
            }
            _spawnedPowerups.Clear();

            _lastPowerupDistance = 0f;
            _lastCoinSpawnZ = 0f;
            // Re-seed random on run reset for deterministic replay
            _seededRandom = new System.Random(42);
        }

        public void SetPlayerReference(Transform player)
        {
            _player = player;
        }
    }
}
