using UnityEngine;
using UnityEngine.Audio;
using Shredsquatch.Core;

namespace Shredsquatch.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Mixer")]
        [SerializeField] private AudioMixer _masterMixer;

        [Header("Music Sources")]
        [SerializeField] private AudioSource _menuMusic;
        [SerializeField] private AudioSource _gameplayMusic;
        [SerializeField] private AudioSource _chaseMusic;

        [Header("Ambient Sources")]
        [SerializeField] private AudioSource _windAmbient;
        [SerializeField] private AudioSource _snowAmbient;

        [Header("SFX Sources")]
        [SerializeField] private AudioSource _sfxSource;

        [Header("Clips")]
        [SerializeField] private AudioClip _trickCompleteClip;
        [SerializeField] private AudioClip _comboClip;
        [SerializeField] private AudioClip _crashClip;
        [SerializeField] private AudioClip _coinCollectClip;
        [SerializeField] private AudioClip _powerupCollectClip;
        [SerializeField] private AudioClip _sasquatchRoarClip;
        [SerializeField] private AudioClip _sasquatchNearClip;

        [Header("Settings")]
        [SerializeField] private float _musicCrossfadeTime = 1f;
        [SerializeField] private float _chaseMusicThreshold = 300f;

        private float _targetMusicVolume = 1f;
        private bool _isChaseMusicPlaying;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged += HandleStateChanged;
            }

            LoadAudioSettings();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
            }
        }

        private void HandleStateChanged(GameState state)
        {
            switch (state)
            {
                case GameState.MainMenu:
                    PlayMenuMusic();
                    break;
                case GameState.Playing:
                    PlayGameplayMusic();
                    break;
                case GameState.Paused:
                    // Optionally duck music
                    break;
                case GameState.GameOver:
                    StopChaseMusic();
                    break;
            }
        }

        public void PlayMenuMusic()
        {
            CrossfadeToSource(_menuMusic);
            StopAmbient();
        }

        public void PlayGameplayMusic()
        {
            CrossfadeToSource(_gameplayMusic);
            StartAmbient();
            _isChaseMusicPlaying = false;
        }

        public void PlayChaseMusic()
        {
            if (_isChaseMusicPlaying) return;

            _isChaseMusicPlaying = true;
            CrossfadeToSource(_chaseMusic);
        }

        public void StopChaseMusic()
        {
            if (!_isChaseMusicPlaying) return;

            _isChaseMusicPlaying = false;
            CrossfadeToSource(_gameplayMusic);
        }

        private void CrossfadeToSource(AudioSource target)
        {
            // Simple crossfade implementation
            if (_menuMusic != null && _menuMusic != target)
            {
                StartCoroutine(FadeOut(_menuMusic, _musicCrossfadeTime));
            }
            if (_gameplayMusic != null && _gameplayMusic != target)
            {
                StartCoroutine(FadeOut(_gameplayMusic, _musicCrossfadeTime));
            }
            if (_chaseMusic != null && _chaseMusic != target)
            {
                StartCoroutine(FadeOut(_chaseMusic, _musicCrossfadeTime));
            }

            if (target != null)
            {
                StartCoroutine(FadeIn(target, _musicCrossfadeTime));
            }
        }

        private System.Collections.IEnumerator FadeOut(AudioSource source, float duration)
        {
            float startVolume = source.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                yield return null;
            }

            source.Stop();
            source.volume = startVolume;
        }

        private System.Collections.IEnumerator FadeIn(AudioSource source, float duration)
        {
            source.volume = 0f;
            source.Play();

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(0f, _targetMusicVolume, elapsed / duration);
                yield return null;
            }

            source.volume = _targetMusicVolume;
        }

        private void StartAmbient()
        {
            if (_windAmbient != null && !_windAmbient.isPlaying)
            {
                _windAmbient.Play();
            }
            if (_snowAmbient != null && !_snowAmbient.isPlaying)
            {
                _snowAmbient.Play();
            }
        }

        private void StopAmbient()
        {
            if (_windAmbient != null) _windAmbient.Stop();
            if (_snowAmbient != null) _snowAmbient.Stop();
        }

        // SFX Methods
        public void PlayTrickComplete()
        {
            PlaySFX(_trickCompleteClip);
        }

        public void PlayCombo()
        {
            PlaySFX(_comboClip);
        }

        public void PlayCrash()
        {
            PlaySFX(_crashClip);
        }

        public void PlayCoinCollect()
        {
            PlaySFX(_coinCollectClip, 0.5f);
        }

        public void PlayPowerupCollect()
        {
            PlaySFX(_powerupCollectClip);
        }

        public void PlaySasquatchRoar()
        {
            PlaySFX(_sasquatchRoarClip, 1f, 1f);
        }

        public void PlaySasquatchNear()
        {
            PlaySFX(_sasquatchNearClip, 0.8f);
        }

        private void PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip == null || _sfxSource == null) return;

            _sfxSource.pitch = pitch;
            _sfxSource.PlayOneShot(clip, volume);
        }

        // Volume controls
        public void SetMasterVolume(float volume)
        {
            if (_masterMixer != null)
            {
                _masterMixer.SetFloat("MasterVolume", LinearToDecibel(volume));
            }
            PlayerPrefs.SetFloat("MasterVolume", volume);
        }

        public void SetMusicVolume(float volume)
        {
            _targetMusicVolume = volume;
            if (_masterMixer != null)
            {
                _masterMixer.SetFloat("MusicVolume", LinearToDecibel(volume));
            }
            PlayerPrefs.SetFloat("MusicVolume", volume);
        }

        public void SetSFXVolume(float volume)
        {
            if (_masterMixer != null)
            {
                _masterMixer.SetFloat("SFXVolume", LinearToDecibel(volume));
            }
            PlayerPrefs.SetFloat("SFXVolume", volume);
        }

        private float LinearToDecibel(float linear)
        {
            return linear > 0.0001f ? 20f * Mathf.Log10(linear) : -80f;
        }

        private void LoadAudioSettings()
        {
            float master = PlayerPrefs.GetFloat("MasterVolume", 1f);
            float music = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
            float sfx = PlayerPrefs.GetFloat("SFXVolume", 1f);

            SetMasterVolume(master);
            SetMusicVolume(music);
            SetSFXVolume(sfx);
        }

        // Update ambient based on speed/distance
        public void UpdateAmbientIntensity(float speedRatio, float sasquatchDistance)
        {
            if (_windAmbient != null)
            {
                _windAmbient.volume = Mathf.Lerp(0.2f, 1f, speedRatio);
                _windAmbient.pitch = 0.8f + speedRatio * 0.4f;
            }

            // Trigger chase music when Sasquatch is close
            if (sasquatchDistance < _chaseMusicThreshold && sasquatchDistance > 0)
            {
                PlayChaseMusic();
            }
            else if (sasquatchDistance > _chaseMusicThreshold * 1.5f)
            {
                StopChaseMusic();
            }
        }
    }
}
