using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using RupeeRanger.Core;

namespace RupeeRanger.UI
{
    public class SplashScreenUI : MonoBehaviour
    {
        [Header("Buttons")]
        public Button playNowButton;
        public Button themeToggleButton;
        public Image themeToggleIcon;
        public Sprite sunIcon;
        public Sprite moonIcon;

        private void Start()
        {
            if (playNowButton != null)
            {
                playNowButton.onClick.AddListener(OnPlayNowClicked);
            }

            if (themeToggleButton != null)
            {
                themeToggleButton.onClick.AddListener(OnThemeToggleClicked);
            }

            UpdateThemeButtonIcon();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnThemeChanged += HandleThemeChanged;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnThemeChanged -= HandleThemeChanged;
            }
        }

        private void HandleThemeChanged(bool isDark)
        {
            UpdateThemeButtonIcon();
        }

        private void UpdateThemeButtonIcon()
        {
            if (themeToggleIcon == null) return;

            bool isDark = GameManager.Instance != null ? GameManager.Instance.isDarkMode : true;
            themeToggleIcon.sprite = isDark ? sunIcon : moonIcon;
        }

        private void OnPlayNowClicked()
        {
            SceneManager.LoadScene("PlayerSetup");
        }

        private void OnThemeToggleClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ToggleTheme();
            }
        }
    }
}
