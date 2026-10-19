using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RupeeRanger.Core;

namespace RupeeRanger.UI
{
    public class ThemeManager : MonoBehaviour
    {
        [Header("Background Elements")]
        public Image mainBackgroundImage;
        public Image bottomWaveImage;

        [Header("Color Schemes")]
        public Color darkBgColor = new Color32(0x13, 0x0E, 0x2B, 0xFF); // Deep purple
        public Color darkWaveColor = new Color32(0x52, 0x24, 0xC4, 0xFF); // Vibrant royal violet
        public Color lightBgColor = new Color32(0xEE, 0xEB, 0xF8, 0xFF); // Clean lilac
        public Color lightWaveColor = new Color32(0xC9, 0xBF, 0xF5, 0xFF); // Soft purple

        [Header("Card Elements")]
        public Image[] cardBackgrounds;
        public Color darkCardColor = new Color32(0x27, 0x1E, 0x54, 0xB0);
        public Color lightCardColor = new Color32(0xFF, 0xFF, 0xFF, 0xE6);

        [Header("Text Elements")]
        public TMP_Text[] titleTexts;
        public Color darkTitleColor = Color.white;
        public Color lightTitleColor = new Color32(0x1B, 0x12, 0x38, 0xFF);

        public TMP_Text[] subtitleTexts;
        public Color darkSubtitleColor = new Color32(0xC7, 0xC3, 0xE8, 0xFF);
        public Color lightSubtitleColor = new Color32(0x56, 0x4E, 0x77, 0xFF);

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnThemeChanged += ApplyTheme;
                ApplyTheme(GameManager.Instance.isDarkMode);
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnThemeChanged -= ApplyTheme;
            }
        }

        public void ApplyTheme(bool isDark)
        {
            if (mainBackgroundImage != null)
            {
                mainBackgroundImage.color = isDark ? darkBgColor : lightBgColor;
            }

            if (bottomWaveImage != null)
            {
                bottomWaveImage.color = isDark ? darkWaveColor : lightWaveColor;
            }

            if (cardBackgrounds != null)
            {
                foreach (var img in cardBackgrounds)
                {
                    if (img != null) img.color = isDark ? darkCardColor : lightCardColor;
                }
            }

            if (titleTexts != null)
            {
                foreach (var txt in titleTexts)
                {
                    if (txt != null) txt.color = isDark ? darkTitleColor : lightTitleColor;
                }
            }

            if (subtitleTexts != null)
            {
                foreach (var txt in subtitleTexts)
                {
                    if (txt != null) txt.color = isDark ? darkSubtitleColor : lightSubtitleColor;
                }
            }
        }
    }
}
