using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Shredsquatch.Progression;

namespace Shredsquatch.UI
{
    /// <summary>
    /// Displays achievement unlock notifications with a popup animation.
    /// </summary>
    public class AchievementNotification : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject _notificationPanel;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _descriptionText;
        [SerializeField] private Image _iconImage;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Animation Settings")]
        [SerializeField] private float _slideInDuration = 0.3f;
        [SerializeField] private float _displayDuration = 3f;
        [SerializeField] private float _slideOutDuration = 0.3f;
        [SerializeField] private float _slideDistance = 100f;

        [Header("Audio")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _unlockSound;

        [Header("Icons")]
        [SerializeField] private Sprite _defaultIcon;
        [SerializeField] private Sprite _hiddenIcon;

        // Notification queue
        private Queue<Achievement> _notificationQueue = new Queue<Achievement>();
        private bool _isShowingNotification;
        private RectTransform _panelRect;
        private Vector2 _hiddenPosition;
        private Vector2 _shownPosition;

        private void Awake()
        {
            if (_notificationPanel != null)
            {
                _panelRect = _notificationPanel.GetComponent<RectTransform>();
                _shownPosition = _panelRect.anchoredPosition;
                _hiddenPosition = _shownPosition + Vector2.up * _slideDistance;
                _notificationPanel.SetActive(false);
            }
        }

        private void Start()
        {
            // Subscribe to achievement events
            if (AchievementManager.Instance != null)
            {
                AchievementManager.Instance.OnAchievementUnlocked += QueueNotification;
            }
        }

        private void OnDestroy()
        {
            if (AchievementManager.Instance != null)
            {
                AchievementManager.Instance.OnAchievementUnlocked -= QueueNotification;
            }
        }

        /// <summary>
        /// Queue an achievement notification.
        /// </summary>
        public void QueueNotification(Achievement achievement)
        {
            _notificationQueue.Enqueue(achievement);

            if (!_isShowingNotification)
            {
                StartCoroutine(ProcessNotificationQueue());
            }
        }

        private IEnumerator ProcessNotificationQueue()
        {
            _isShowingNotification = true;

            while (_notificationQueue.Count > 0)
            {
                var achievement = _notificationQueue.Dequeue();
                yield return StartCoroutine(ShowNotification(achievement));

                // Small delay between notifications if multiple queued
                if (_notificationQueue.Count > 0)
                {
                    yield return new WaitForSeconds(0.5f);
                }
            }

            _isShowingNotification = false;
        }

        private IEnumerator ShowNotification(Achievement achievement)
        {
            // Set up content
            if (_titleText != null)
            {
                _titleText.text = "ACHIEVEMENT UNLOCKED";
            }

            if (_nameText != null)
            {
                _nameText.text = achievement.Name;
            }

            if (_descriptionText != null)
            {
                _descriptionText.text = achievement.Description;
            }

            if (_iconImage != null)
            {
                _iconImage.sprite = achievement.Type == AchievementType.Hidden
                    ? _hiddenIcon ?? _defaultIcon
                    : _defaultIcon;
            }

            // Play sound
            if (_audioSource != null && _unlockSound != null)
            {
                _audioSource.PlayOneShot(_unlockSound);
            }

            // Show panel
            if (_notificationPanel != null)
            {
                _notificationPanel.SetActive(true);
            }

            // Slide in
            yield return StartCoroutine(SlideAnimation(_hiddenPosition, _shownPosition, _slideInDuration, true));

            // Display
            yield return new WaitForSeconds(_displayDuration);

            // Slide out
            yield return StartCoroutine(SlideAnimation(_shownPosition, _hiddenPosition, _slideOutDuration, false));

            // Hide panel
            if (_notificationPanel != null)
            {
                _notificationPanel.SetActive(false);
            }
        }

        private IEnumerator SlideAnimation(Vector2 from, Vector2 to, float duration, bool fadeIn)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;

                // Ease out cubic
                float eased = 1f - Mathf.Pow(1f - t, 3f);

                if (_panelRect != null)
                {
                    _panelRect.anchoredPosition = Vector2.Lerp(from, to, eased);
                }

                if (_canvasGroup != null)
                {
                    _canvasGroup.alpha = fadeIn ? eased : 1f - eased;
                }

                yield return null;
            }

            // Ensure final position
            if (_panelRect != null)
            {
                _panelRect.anchoredPosition = to;
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = fadeIn ? 1f : 0f;
            }
        }

        /// <summary>
        /// Force show a notification immediately (for testing).
        /// </summary>
        public void TestNotification()
        {
            var testAchievement = new Achievement(
                AchievementId.FirstRun,
                "Test Achievement",
                "This is a test notification",
                AchievementType.Single
            );
            QueueNotification(testAchievement);
        }
    }
}
