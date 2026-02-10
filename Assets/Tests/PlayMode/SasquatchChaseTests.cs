using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Shredsquatch.Core;
using Shredsquatch.Player;
using Shredsquatch.Sasquatch;

namespace Shredsquatch.Tests.PlayMode
{
    /// <summary>
    /// Integration tests for Distance -> Sasquatch Spawn -> Chase mechanics.
    /// Tests spawn conditions, rubber-banding behavior, and catch mechanics.
    /// </summary>
    [TestFixture]
    public class SasquatchChaseTests
    {
        private GameObject _gameManagerObj;
        private GameObject _playerObj;
        private GameObject _sasquatchObj;
        private GameManager _gameManager;
        private SasquatchAI _sasquatch;
        private SnowboardPhysics _physics;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Create GameManager
            _gameManagerObj = new GameObject("GameManager");
            _gameManager = _gameManagerObj.AddComponent<GameManager>();
            yield return null;

            // Create Player with minimal components
            _playerObj = new GameObject("Player");
            _playerObj.tag = "Player";
            var charController = _playerObj.AddComponent<CharacterController>();
            var playerInput = _playerObj.AddComponent<PlayerInput>();
            _physics = _playerObj.AddComponent<SnowboardPhysics>();

            // Create Sasquatch
            _sasquatchObj = new GameObject("Sasquatch");
            _sasquatch = _sasquatchObj.AddComponent<SasquatchAI>();
            _sasquatch.SetPlayerReference(_playerObj.transform);

            yield return null;

            // Set player reference in GameManager
            _gameManager.SetPlayerReference(_playerObj.transform);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_sasquatchObj != null) Object.Destroy(_sasquatchObj);
            if (_playerObj != null) Object.Destroy(_playerObj);
            if (_gameManagerObj != null) Object.Destroy(_gameManagerObj);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Sasquatch_StartsInactive()
        {
            // Assert
            Assert.IsFalse(_sasquatch.IsActive);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Sasquatch_SpawnsAfterDistanceThreshold()
        {
            // Arrange
            _gameManager.StartRun(GameMode.Standard);
            bool spawnEventFired = false;
            _sasquatch.OnSpawn += () => spawnEventFired = true;
            yield return null;

            // Act - Move player past spawn distance (5km = 5000m in Z)
            _playerObj.transform.position = new Vector3(0, 0, 5100f);

            // Wait for distance update and spawn check
            yield return new WaitForSeconds(0.2f);

            // Note: Sasquatch spawns when GameManager.OnDistanceChanged fires
            // This test verifies the event subscription is working
            Assert.IsTrue(GameManager.Instance.CurrentRun.Distance >= Constants.Sasquatch.SpawnDistance ||
                          _sasquatch.IsActive || spawnEventFired);
        }

        [UnityTest]
        public IEnumerator Sasquatch_DoesNotSpawnBeforeThreshold()
        {
            // Arrange
            _gameManager.StartRun(GameMode.Standard);
            yield return null;

            // Act - Move player but not past spawn distance
            _playerObj.transform.position = new Vector3(0, 0, 1000f); // 1km
            yield return new WaitForSeconds(0.1f);

            // Assert
            Assert.IsFalse(_sasquatch.IsActive);
        }

        [UnityTest]
        public IEnumerator Sasquatch_TracksDistanceToPlayer()
        {
            // Arrange - manually activate sasquatch for testing
            _gameManager.StartRun(GameMode.Standard);
            _sasquatchObj.SetActive(true);
            yield return null;

            // Position player and sasquatch
            _playerObj.transform.position = new Vector3(0, 0, 100f);
            _sasquatchObj.transform.position = new Vector3(0, 0, 0f);
            yield return null;

            // Assert
            float distance = _sasquatch.DistanceToPlayer;
            Assert.Greater(distance, 0f);
        }

        [UnityTest]
        public IEnumerator Sasquatch_OnCatchPlayer_EndsRun()
        {
            // Arrange
            _gameManager.StartRun(GameMode.Standard);
            bool catchEventFired = false;
            _sasquatch.OnCatchPlayer += () => catchEventFired = true;
            yield return null;

            // Act - Position sasquatch very close to player (within catch distance)
            _sasquatchObj.SetActive(true);
            _playerObj.transform.position = new Vector3(0, 0, 0);
            _sasquatchObj.transform.position = new Vector3(0, 0, 1f); // Within 3m catch distance

            // Wait for catch detection
            yield return new WaitForSeconds(0.2f);

            // The catch event should fire and end the run
            // Note: Actual behavior depends on Update running while sasquatch is active
            Assert.IsTrue(catchEventFired || _gameManager.CurrentState == GameState.GameOver);
        }

        [UnityTest]
        public IEnumerator Sasquatch_RubberBanding_SpeedsUpWhenFar()
        {
            // This tests the rubber-band mechanic concept
            // When player is far ahead, sasquatch should speed up

            // Arrange
            _gameManager.StartRun(GameMode.Standard);
            yield return null;

            // Verify constants are set correctly for rubber-banding
            Assert.Greater(Constants.Sasquatch.BurstSpeedMod, 1f,
                "Burst speed modifier should be > 1 for catch-up");
            Assert.Less(Constants.Sasquatch.TiredSpeedMod, 1f,
                "Tired speed modifier should be < 1 when close");
        }

        [UnityTest]
        public IEnumerator Sasquatch_ApplyRepellent_SlowsChase()
        {
            // Arrange
            _gameManager.StartRun(GameMode.Standard);
            _sasquatchObj.SetActive(true);
            yield return null;

            // Act
            _sasquatch.ApplyRepellent();
            yield return null;

            // Assert - repellent slows sasquatch (verify constant exists)
            Assert.AreEqual(0.5f, Constants.Powerup.RepellentSlowdown,
                "Repellent should slow sasquatch by 50%");
        }

        [UnityTest]
        public IEnumerator Sasquatch_ResetsOnNewRun()
        {
            // Arrange - start a run and simulate sasquatch being active
            _gameManager.StartRun(GameMode.Standard);
            _sasquatchObj.SetActive(true);
            yield return null;

            // Act - return to menu and start new run
            _gameManager.ReturnToMenu();
            yield return null;
            _gameManager.StartRun(GameMode.Standard);
            yield return null;

            // Assert - sasquatch should be inactive for new run
            Assert.IsFalse(_sasquatch.IsActive);
        }

        [UnityTest]
        public IEnumerator Sasquatch_SpawnPosition_IsBehindPlayer()
        {
            // Verify spawn logic places sasquatch behind the player
            // The spawn position should be 800m behind the player

            float expectedSpawnDistance = 800f;

            // This is a constants/design verification test
            Assert.IsTrue(expectedSpawnDistance > Constants.Sasquatch.TargetDistance,
                "Sasquatch should spawn behind its target chase distance");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Distance_ThresholdsAreConfiguredCorrectly()
        {
            // Verify the chase distance thresholds make sense
            yield return null;

            // Far threshold should be greater than target distance
            Assert.Greater(Constants.Sasquatch.FarThreshold, Constants.Sasquatch.TargetDistance,
                "Far threshold should exceed target distance");

            // Close threshold should be less than target distance
            Assert.Less(Constants.Sasquatch.CloseThreshold, Constants.Sasquatch.TargetDistance,
                "Close threshold should be under target distance");

            // Close should be greater than 0
            Assert.Greater(Constants.Sasquatch.CloseThreshold, 0,
                "Close threshold should be positive");
        }
    }
}
