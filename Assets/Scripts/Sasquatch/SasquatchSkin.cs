using UnityEngine;

namespace Shredsquatch.Sasquatch
{
    public enum SasquatchSkinType
    {
        Default,
        ClassicYeti,    // 5km unlock - white fur
        Abominable,     // 5km unlock - ice blue
        Golden          // 30km unlock
    }

    [CreateAssetMenu(fileName = "SasquatchSkin", menuName = "Shredsquatch/Sasquatch Skin")]
    public class SasquatchSkin : ScriptableObject
    {
        public SasquatchSkinType Type;
        public string DisplayName;
        public float UnlockDistanceKm;

        [Header("Appearance")]
        public Material FurMaterial;
        public Color EyeGlowColor = Color.red;
        public float EyeGlowIntensity = 2f;

        [Header("Audio")]
        public AudioClip RoarSound;
        public AudioClip FootstepSound;
    }

    public class SasquatchSkinManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Renderer _furRenderer;
        [SerializeField] private Light _leftEyeLight;
        [SerializeField] private Light _rightEyeLight;
        [SerializeField] private AudioSource _roarSource;

        [Header("Skins")]
        [SerializeField] private SasquatchSkin[] _availableSkins;

        private SasquatchSkin _currentSkin;

        public void ApplySkin(SasquatchSkinType type)
        {
            foreach (var skin in _availableSkins)
            {
                if (skin.Type == type)
                {
                    ApplySkin(skin);
                    return;
                }
            }
        }

        public void ApplySkin(SasquatchSkin skin)
        {
            _currentSkin = skin;

            if (_furRenderer != null && skin.FurMaterial != null)
            {
                _furRenderer.material = skin.FurMaterial;
            }

            if (_leftEyeLight != null)
            {
                _leftEyeLight.color = skin.EyeGlowColor;
                _leftEyeLight.intensity = skin.EyeGlowIntensity;
            }

            if (_rightEyeLight != null)
            {
                _rightEyeLight.color = skin.EyeGlowColor;
                _rightEyeLight.intensity = skin.EyeGlowIntensity;
            }

            if (_roarSource != null && skin.RoarSound != null)
            {
                _roarSource.clip = skin.RoarSound;
            }
        }

        public SasquatchSkin GetCurrentSkin() => _currentSkin;
    }
}
