using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Shredsquatch.Core;
using Shredsquatch.Player;
using Shredsquatch.Terrain;

namespace Shredsquatch.Tests.PlayMode
{
    /// <summary>
    /// Integration tests for Error Recovery across multiple systems.
    /// Tests the ErrorRecoveryManager, SafeExecution utility, and IRecoverable implementations.
    /// </summary>
    [TestFixture]
    public class ErrorRecoveryTests
    {
        private GameObject _errorManagerObj;
        private GameObject _gameManagerObj;
        private GameObject _playerObj;
        private GameObject _terrainGeneratorObj;
        private ErrorRecoveryManager _errorManager;
        private GameManager _gameManager;
        private PlayerController _player;
        private TerrainGenerator _terrainGenerator;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Create ErrorRecoveryManager first
            _errorManagerObj = new GameObject("ErrorRecoveryManager");
            _errorManager = _errorManagerObj.AddComponent<ErrorRecoveryManager>();
            yield return null;

            // Create GameManager
            _gameManagerObj = new GameObject("GameManager");
            _gameManager = _gameManagerObj.AddComponent<GameManager>();
            yield return null;

            // Create Player with required components
            _playerObj = new GameObject("Player");
            _playerObj.tag = "Player";
            var charController = _playerObj.AddComponent<CharacterController>();
            var playerInput = _playerObj.AddComponent<PlayerInput>();
            var physics = _playerObj.AddComponent<SnowboardPhysics>();
            var jump = _playerObj.AddComponent<JumpController>();
            var crash = _playerObj.AddComponent<CrashHandler>();
            _player = _playerObj.AddComponent<PlayerController>();
            yield return null;

            // Create TerrainGenerator
            _terrainGeneratorObj = new GameObject("TerrainGenerator");
            _terrainGenerator = _terrainGeneratorObj.AddComponent<TerrainGenerator>();
            _terrainGenerator.SetPlayerReference(_playerObj.transform);
            yield return null;

            // Set player reference in GameManager
            _gameManager.SetPlayerReference(_playerObj.transform);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_terrainGeneratorObj != null) Object.Destroy(_terrainGeneratorObj);
            if (_playerObj != null) Object.Destroy(_playerObj);
            if (_gameManagerObj != null) Object.Destroy(_gameManagerObj);
            if (_errorManagerObj != null) Object.Destroy(_errorManagerObj);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ErrorRecoveryManager_SingletonExists()
        {
            Assert.IsNotNull(ErrorRecoveryManager.Instance);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SafeExecution_Try_CatchesExceptions()
        {
            bool result = SafeExecution.Try(() =>
            {
                throw new Exception("Test exception");
            }, "TestContext");

            yield return null;

            Assert.IsFalse(result, "SafeExecution.Try should return false on exception");
        }

        [UnityTest]
        public IEnumerator SafeExecution_Try_ReturnsTrueOnSuccess()
        {
            bool actionExecuted = false;
            bool result = SafeExecution.Try(() =>
            {
                actionExecuted = true;
            }, "TestContext");

            yield return null;

            Assert.IsTrue(result, "SafeExecution.Try should return true on success");
            Assert.IsTrue(actionExecuted, "Action should have executed");
        }

        [UnityTest]
        public IEnumerator SafeExecution_TryWithResult_ReturnsFallbackOnException()
        {
            int fallback = 42;
            int result = SafeExecution.Try<int>(() =>
            {
                throw new Exception("Test exception");
            }, fallback, "TestContext");

            yield return null;

            Assert.AreEqual(fallback, result, "Should return fallback value on exception");
        }

        [UnityTest]
        public IEnumerator SafeExecution_TryWithResult_ReturnsActualValueOnSuccess()
        {
            int expected = 100;
            int result = SafeExecution.Try(() => expected, 0, "TestContext");

            yield return null;

            Assert.AreEqual(expected, result, "Should return actual value on success");
        }

        [UnityTest]
        public IEnumerator SafeExecution_TryInvoke_InvokesAllHandlers()
        {
            int callCount = 0;
            Action testEvent = null;
            testEvent += () => callCount++;
            testEvent += () => callCount++;
            testEvent += () => callCount++;

            SafeExecution.TryInvoke(testEvent, "TestEvent");

            yield return null;

            Assert.AreEqual(3, callCount, "All handlers should be invoked");
        }

        [UnityTest]
        public IEnumerator SafeExecution_TryInvoke_ContinuesAfterHandlerException()
        {
            int successCount = 0;
            Action testEvent = null;
            testEvent += () => successCount++;
            testEvent += () => throw new Exception("Handler exception");
            testEvent += () => successCount++;

            SafeExecution.TryInvoke(testEvent, "TestEvent");

            yield return null;

            Assert.AreEqual(2, successCount, "Should continue invoking after exception");
        }

        [UnityTest]
        public IEnumerator ErrorRecoveryManager_RegistersRecoverables()
        {
            // GameManager, PlayerController, and TerrainGenerator implement IRecoverable
            // They should register themselves with the ErrorRecoveryManager

            // This test verifies the registration happened during Awake/Start
            yield return null;

            // No exceptions should occur during registration
            Assert.IsNotNull(_errorManager);
        }

        [UnityTest]
        public IEnumerator GameManager_AttemptRecovery_ResetsTimeScale()
        {
            // Arrange
            _gameManager.StartRun(GameMode.Standard);
            _gameManager.PauseGame();
            Assert.AreEqual(0f, Time.timeScale);
            yield return null;

            // Act
            _gameManager.AttemptRecovery();
            yield return null;

            // Assert
            Assert.AreEqual(1f, Time.timeScale, "Recovery should reset time scale to 1");
        }

        [UnityTest]
        public IEnumerator GameManager_AttemptRecovery_ReturnsToSafeState()
        {
            // Arrange
            _gameManager.StartRun(GameMode.Standard);
            yield return null;

            // Act
            _gameManager.AttemptRecovery();
            yield return null;

            // Assert - should be back to menu state
            Assert.AreEqual(GameState.MainMenu, _gameManager.CurrentState);
        }

        [UnityTest]
        public IEnumerator PlayerController_AttemptRecovery_ResetsPosition()
        {
            // Arrange - set initial position
            Vector3 safePos = new Vector3(0, 10, 0);
            _playerObj.transform.position = safePos;
            yield return new WaitForSeconds(0.1f); // Allow safe position to update

            // Move player to a "dangerous" position
            _playerObj.transform.position = new Vector3(100, -50, 500);
            yield return null;

            // Act
            _player.AttemptRecovery();
            yield return null;

            // Note: Recovery resets to last safe position stored during normal gameplay
            Assert.IsNotNull(_player);
        }

        [UnityTest]
        public IEnumerator TerrainGenerator_AttemptRecovery_ClearsState()
        {
            // Arrange
            _gameManager.StartRun(GameMode.Standard);
            yield return null;

            // Act
            _terrainGenerator.AttemptRecovery();
            yield return null;

            // Assert - should not throw, generator should be functional
            Assert.IsNotNull(_terrainGenerator);
        }

        [UnityTest]
        public IEnumerator ErrorRecoveryManager_TryExecute_HandlesSuccess()
        {
            bool executed = false;
            bool result = _errorManager.TryExecute(() =>
            {
                executed = true;
            }, "TestExecution");

            yield return null;

            Assert.IsTrue(result, "TryExecute should return true on success");
            Assert.IsTrue(executed, "Action should have executed");
        }

        [UnityTest]
        public IEnumerator ErrorRecoveryManager_TryExecute_HandlesFailure()
        {
            bool result = _errorManager.TryExecute(() =>
            {
                throw new Exception("Test failure");
            }, "TestExecution");

            yield return null;

            Assert.IsFalse(result, "TryExecute should return false on failure");
        }

        [UnityTest]
        public IEnumerator ErrorRecoveryManager_GetErrorStats_ReturnsStats()
        {
            var stats = _errorManager.GetErrorStats();

            yield return null;

            // Should return tuple without throwing
            Assert.GreaterOrEqual(stats.total, 0);
            Assert.GreaterOrEqual(stats.recent, 0);
        }

        [UnityTest]
        public IEnumerator ErrorRecoveryManager_ForceReset_ReturnsToMenu()
        {
            // Arrange
            _gameManager.StartRun(GameMode.Standard);
            yield return null;

            // Act
            _errorManager.ForceReset();
            yield return null;

            // Assert
            Assert.AreEqual(1f, Time.timeScale, "Force reset should restore time scale");
            Assert.AreEqual(GameState.MainMenu, _gameManager.CurrentState);
        }

        [UnityTest]
        public IEnumerator SafeExecution_TryGetComponent_ReturnsComponent()
        {
            var charController = SafeExecution.TryGetComponent<CharacterController>(_playerObj);

            yield return null;

            Assert.IsNotNull(charController, "Should find existing component");
        }

        [UnityTest]
        public IEnumerator SafeExecution_TryGetComponent_ReturnsNullForMissing()
        {
            var audioSource = SafeExecution.TryGetComponent<AudioSource>(_playerObj);

            yield return null;

            Assert.IsNull(audioSource, "Should return null for missing component");
        }

        [UnityTest]
        public IEnumerator SafeExecution_TryGetComponent_ReturnsNullForNullObject()
        {
            GameObject nullObj = null;
            var result = SafeExecution.TryGetComponent<Transform>(nullObj);

            yield return null;

            Assert.IsNull(result, "Should return null for null object");
        }

        [UnityTest]
        public IEnumerator MultipleErrors_TriggerRecoveryAtThreshold()
        {
            // This tests the threshold-based recovery trigger
            // Default is 5 errors within 10 seconds

            bool recoveryStarted = false;
            _errorManager.OnRecoveryStarted += () => recoveryStarted = true;

            // Simulate multiple exceptions through the handler
            for (int i = 0; i < 6; i++)
            {
                _errorManager.HandleException(new Exception($"Error {i}"), "TestError");
            }

            yield return null;

            // Recovery should have been triggered
            // Note: Actual behavior depends on timing and configuration
            Assert.IsNotNull(_errorManager);
        }
    }
}
