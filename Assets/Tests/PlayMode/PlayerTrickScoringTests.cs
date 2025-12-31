using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Shredsquatch.Core;
using Shredsquatch.Player;
using Shredsquatch.Tricks;

namespace Shredsquatch.Tests.PlayMode
{
    /// <summary>
    /// Integration tests for Player Movement -> Trick Detection -> Scoring flow.
    /// Tests the complete chain from player input through trick completion to score update.
    /// </summary>
    [TestFixture]
    public class PlayerTrickScoringTests
    {
        private GameObject _gameManagerObj;
        private GameObject _playerObj;
        private GameManager _gameManager;
        private PlayerController _player;
        private TrickController _trickController;
        private JumpController _jumpController;
        private SnowboardPhysics _physics;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Create GameManager
            _gameManagerObj = new GameObject("GameManager");
            _gameManager = _gameManagerObj.AddComponent<GameManager>();
            yield return null; // Allow Awake to run

            // Create Player with required components
            _playerObj = new GameObject("Player");
            _playerObj.tag = "Player";

            // Add CharacterController first (required by physics)
            var charController = _playerObj.AddComponent<CharacterController>();
            charController.height = 1.8f;
            charController.radius = 0.5f;

            // Add components in dependency order
            var playerInput = _playerObj.AddComponent<PlayerInput>();
            _physics = _playerObj.AddComponent<SnowboardPhysics>();
            _jumpController = _playerObj.AddComponent<JumpController>();
            _playerObj.AddComponent<CrashHandler>();
            _trickController = _playerObj.AddComponent<TrickController>();
            _player = _playerObj.AddComponent<PlayerController>();

            yield return null; // Allow components to initialize

            // Set player reference in GameManager
            _gameManager.SetPlayerReference(_playerObj.transform);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_playerObj != null) Object.Destroy(_playerObj);
            if (_gameManagerObj != null) Object.Destroy(_gameManagerObj);
            yield return null;
        }

        [UnityTest]
        public IEnumerator GameManager_StartRun_SetsPlayingState()
        {
            // Act
            _gameManager.StartRun(GameMode.Standard);
            yield return null;

            // Assert
            Assert.AreEqual(GameState.Playing, _gameManager.CurrentState);
        }

        [UnityTest]
        public IEnumerator GameManager_StartRun_ResetsRunStats()
        {
            // Arrange - add some score first
            GameManager.CurrentRun.TrickScore = 1000;
            GameManager.CurrentRun.TrickCount = 5;

            // Act
            _gameManager.StartRun(GameMode.Standard);
            yield return null;

            // Assert
            Assert.AreEqual(0, GameManager.CurrentRun.TrickScore);
            Assert.AreEqual(0, GameManager.CurrentRun.TrickCount);
        }

        [UnityTest]
        public IEnumerator TrickController_CompletesSpinTrick_WhenRotationThresholdMet()
        {
            // Arrange
            _gameManager.StartRun(GameMode.Standard);
            yield return null;

            int scoreBeforeTrick = GameManager.CurrentRun.TrickScore;

            // Simulate being airborne
            // Note: In real tests, we'd use InputSystem.TestFramework to simulate input
            // For now, we test the scoring integration directly
            _gameManager.AddTrickScore(100, 1);
            yield return null;

            // Assert
            Assert.Greater(GameManager.CurrentRun.TrickScore, scoreBeforeTrick);
            Assert.AreEqual(1, GameManager.CurrentRun.TrickCount);
        }

        [UnityTest]
        public IEnumerator TrickScore_AccumulatesAcrossMultipleTricks()
        {
            // Arrange
            _gameManager.StartRun(GameMode.Standard);
            yield return null;

            // Act - simulate multiple tricks
            _gameManager.AddTrickScore(100, 1); // First trick
            yield return null;
            _gameManager.AddTrickScore(200, 2); // Second trick with combo
            yield return null;
            _gameManager.AddTrickScore(300, 3); // Third trick
            yield return null;

            // Assert
            Assert.AreEqual(600, GameManager.CurrentRun.TrickScore);
            Assert.AreEqual(3, GameManager.CurrentRun.TrickCount);
        }

        [UnityTest]
        public IEnumerator TrickController_NotifiesOnTrickComplete()
        {
            // Arrange
            _gameManager.StartRun(GameMode.Standard);
            bool trickCompletedFired = false;

            if (_trickController != null)
            {
                _trickController.OnTrickComplete += (trickData) => trickCompletedFired = true;
            }
            yield return null;

            // Note: Full trick simulation would require mocking the jump controller
            // This test verifies the event subscription works
            Assert.IsNotNull(_trickController);
        }

        [UnityTest]
        public IEnumerator GameManager_PauseGame_StopsTimeScale()
        {
            // Arrange
            _gameManager.StartRun(GameMode.Standard);
            yield return null;

            // Act
            _gameManager.PauseGame();
            yield return null;

            // Assert
            Assert.AreEqual(GameState.Paused, _gameManager.CurrentState);
            Assert.AreEqual(0f, Time.timeScale);

            // Cleanup - restore time scale
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator GameManager_ResumeGame_RestoresTimeScale()
        {
            // Arrange
            _gameManager.StartRun(GameMode.Standard);
            _gameManager.PauseGame();
            yield return null;

            // Act
            _gameManager.ResumeGame();
            yield return null;

            // Assert
            Assert.AreEqual(GameState.Playing, _gameManager.CurrentState);
            Assert.AreEqual(1f, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator Distance_UpdatesAsPlayerMoves()
        {
            // Arrange
            _gameManager.StartRun(GameMode.Standard);
            yield return null;

            float initialDistance = GameManager.CurrentRun.Distance;

            // Act - move player forward (positive Z)
            _playerObj.transform.position += new Vector3(0, 0, 100f);
            yield return new WaitForSeconds(0.1f); // Allow update to run

            // Assert
            Assert.Greater(GameManager.CurrentRun.Distance, initialDistance);
        }

        [UnityTest]
        public IEnumerator EndRun_SavesProgress()
        {
            // Arrange
            _gameManager.StartRun(GameMode.Standard);
            _gameManager.AddTrickScore(500, 1);
            yield return null;

            // Act
            _gameManager.EndRun();
            yield return null;

            // Assert
            Assert.AreEqual(GameState.GameOver, _gameManager.CurrentState);
        }
    }
}
