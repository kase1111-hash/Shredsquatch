using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Shredsquatch.Core;
using Shredsquatch.Player;
using Shredsquatch.Tricks;

namespace Shredsquatch.UI
{
    public class TutorialController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject _tutorialPanel;
        [SerializeField] private TextMeshProUGUI _promptText;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Settings")]
        [SerializeField] private float _tutorialEndDistance = 2f; // km
        [SerializeField] private float _promptDisplayTime = 4f;
        [SerializeField] private float _fadeTime = 0.5f;
        [SerializeField] private float _delayBetweenPrompts = 2f;

        [Header("References")]
        [SerializeField] private PlayerController _player;
        [SerializeField] private TrickController _trickController;

        // Tutorial state
        private bool _tutorialActive = true;
        private bool _isShowingPrompt;
        private float _promptTimer;

        // Tracking what player has done
        private bool _hasSteered;
        private bool _hasTucked;
        private bool _hasBraked;
        private bool _hasJumped;
        private bool _hasDoneTrick;
        private bool _hasLandedTrick;

        // Distance-based prompts
        private bool _shown500mPrompt;
        private bool _shown1kmPrompt;
        private bool _shown1_5kmPrompt;

        private PlayerInput _input;
        private Coroutine _currentPromptCoroutine;

        private void Start()
        {
            if (_player != null)
            {
                _input = _player.GetComponent<PlayerInput>();
            }

            if (_trickController != null)
            {
                _trickController.OnTrickCompleted += OnTrickCompleted;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRunStarted += OnRunStarted;
                GameManager.Instance.OnDistanceChanged += OnDistanceChanged;
            }

            // Hide panel initially
            if (_tutorialPanel != null)
            {
                _tutorialPanel.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (_trickController != null)
            {
                _trickController.OnTrickCompleted -= OnTrickCompleted;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRunStarted -= OnRunStarted;
                GameManager.Instance.OnDistanceChanged -= OnDistanceChanged;
            }
        }

        private void Update()
        {
            if (!_tutorialActive || GameManager.Instance?.CurrentState != GameState.Playing)
                return;

            TrackPlayerActions();
            UpdateContextualPrompts();
        }

        private void OnRunStarted()
        {
            ResetTutorial();

            // Show initial prompt after a short delay
            StartCoroutine(ShowInitialPrompt());
        }

        private void ResetTutorial()
        {
            _tutorialActive = true;
            _hasSteered = false;
            _hasTucked = false;
            _hasBraked = false;
            _hasJumped = false;
            _hasDoneTrick = false;
            _hasLandedTrick = false;
            _shown500mPrompt = false;
            _shown1kmPrompt = false;
            _shown1_5kmPrompt = false;
        }

        private IEnumerator ShowInitialPrompt()
        {
            yield return new WaitForSeconds(1f);

            if (_tutorialActive)
            {
                ShowPrompt("Use A/D or Left Stick to steer\nHold W or Right Trigger to tuck and go faster");
            }
        }

        private void TrackPlayerActions()
        {
            if (_input == null) return;

            // Track steering
            if (!_hasSteered && Mathf.Abs(_input.SteerInput) > 0.3f)
            {
                _hasSteered = true;
            }

            // Track tucking
            if (!_hasTucked && _input.IsTucking)
            {
                _hasTucked = true;
            }

            // Track braking
            if (!_hasBraking && _input.IsBraking)
            {
                _hasBraked = true;
            }

            // Track jumping
            if (!_hasJumped && _input.JumpPressed)
            {
                _hasJumped = true;
            }

            // Track trick attempts
            if (!_hasDoneTrick && (_input.SpinInput != 0 || _input.GrabInput != 0))
            {
                _hasDoneTrick = true;
            }
        }

        private bool _hasBraking; // Track if we've already detected braking

        private void UpdateContextualPrompts()
        {
            if (_isShowingPrompt) return;

            float distance = GameManager.Instance?.CurrentRun.Distance ?? 0f;

            // Early game prompts (0-0.5km)
            if (distance < 0.5f)
            {
                if (_hasSteered && !_hasTucked && !_isShowingPrompt)
                {
                    ShowPrompt("Hold W or Right Trigger to tuck\nBuild speed to outrun what's coming...");
                    return;
                }

                if (_hasTucked && !_hasJumped && distance > 0.2f && !_isShowingPrompt)
                {
                    ShowPrompt("Press SPACE or A Button to jump\nHit ramps for bigger air!");
                    return;
                }
            }
        }

        private void OnDistanceChanged(float distanceKm)
        {
            // End tutorial at 2km
            if (distanceKm >= _tutorialEndDistance)
            {
                EndTutorial();
                return;
            }

            if (_isShowingPrompt) return;

            // 500m prompt - tricks
            if (!_shown500mPrompt && distanceKm >= 0.5f)
            {
                _shown500mPrompt = true;
                if (!_hasLandedTrick)
                {
                    ShowPrompt("In the air: Q/E to spin, 1-4 for grabs\nLand clean for bonus points!");
                }
            }

            // 1km prompt - obstacles
            if (!_shown1kmPrompt && distanceKm >= 1f)
            {
                _shown1kmPrompt = true;
                ShowPrompt("Watch for trees and rocks!\nHit them too fast and you'll crash");
            }

            // 1.5km prompt - rails
            if (!_shown1_5kmPrompt && distanceKm >= 1.5f)
            {
                _shown1_5kmPrompt = true;
                ShowPrompt("Jump onto rails to grind\nBalance with A/D for bonus points");
            }
        }

        private void OnTrickCompleted(string trickName, int points)
        {
            _hasLandedTrick = true;

            // If early in tutorial, congratulate first trick
            if (!_shown500mPrompt && !_isShowingPrompt)
            {
                ShowPrompt("Nice trick! Chain tricks for combo multipliers");
            }
        }

        private void ShowPrompt(string message)
        {
            if (_currentPromptCoroutine != null)
            {
                StopCoroutine(_currentPromptCoroutine);
            }
            _currentPromptCoroutine = StartCoroutine(ShowPromptCoroutine(message));
        }

        private IEnumerator ShowPromptCoroutine(string message)
        {
            _isShowingPrompt = true;

            if (_tutorialPanel != null)
            {
                _tutorialPanel.SetActive(true);
            }

            if (_promptText != null)
            {
                _promptText.text = message;
            }

            // Fade in
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                float elapsed = 0f;
                while (elapsed < _fadeTime)
                {
                    elapsed += Time.deltaTime;
                    _canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / _fadeTime);
                    yield return null;
                }
                _canvasGroup.alpha = 1f;
            }

            // Display time
            yield return new WaitForSeconds(_promptDisplayTime);

            // Fade out
            if (_canvasGroup != null)
            {
                float elapsed = 0f;
                while (elapsed < _fadeTime)
                {
                    elapsed += Time.deltaTime;
                    _canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / _fadeTime);
                    yield return null;
                }
                _canvasGroup.alpha = 0f;
            }

            if (_tutorialPanel != null)
            {
                _tutorialPanel.SetActive(false);
            }

            // Delay before next prompt
            yield return new WaitForSeconds(_delayBetweenPrompts);

            _isShowingPrompt = false;
        }

        private void EndTutorial()
        {
            if (!_tutorialActive) return;

            _tutorialActive = false;

            if (_currentPromptCoroutine != null)
            {
                StopCoroutine(_currentPromptCoroutine);
            }

            // Show final message (store reference so it can be stopped if object is destroyed)
            _currentPromptCoroutine = StartCoroutine(ShowFinalPrompt());
        }

        private IEnumerator ShowFinalPrompt()
        {
            _isShowingPrompt = true;

            if (_tutorialPanel != null)
            {
                _tutorialPanel.SetActive(true);
            }

            if (_promptText != null)
            {
                _promptText.text = "Tutorial complete!\nSomething stirs in the mountain ahead...";
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                float elapsed = 0f;
                while (elapsed < _fadeTime)
                {
                    elapsed += Time.deltaTime;
                    _canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / _fadeTime);
                    yield return null;
                }
            }

            yield return new WaitForSeconds(3f);

            if (_canvasGroup != null)
            {
                float elapsed = 0f;
                while (elapsed < _fadeTime)
                {
                    elapsed += Time.deltaTime;
                    _canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / _fadeTime);
                    yield return null;
                }
            }

            if (_tutorialPanel != null)
            {
                _tutorialPanel.SetActive(false);
            }

            _isShowingPrompt = false;
        }

        public void SkipTutorial()
        {
            EndTutorial();
        }
    }
}
