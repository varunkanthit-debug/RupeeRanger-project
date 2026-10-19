using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RupeeRanger.Data;
using RupeeRanger.Core;

namespace RupeeRanger.UI
{
    public class LifestyleCardItem : MonoBehaviour
    {
        [Header("UI Elements")]
        public Button cardButton;
        public Image cardBackgroundImage;
        public Image outlineHighlightImage;
        public Image iconImage;
        public TMP_Text titleText;
        public TMP_Text subtitleText;
        public TMP_Text incomeText;

        [Header("Colors")]
        public Color normalBgColor = new Color32(0x1B, 0x18, 0x42, 0xFF);
        public Color selectedBgColor = new Color32(0x27, 0x22, 0x5E, 0xFF);
        public Color normalOutlineColor = new Color(1f, 1f, 1f, 0.08f);
        public Color selectedOutlineColor = new Color32(0x00, 0xE5, 0xFF, 0xFF); // Vibrant Cyan glow

        private LifestyleConfig config;
        private Action<LifestyleCardItem> onSelectedCallback;
        private bool isSelected = false;

        public LifestyleConfig Config => config;
        public bool IsSelected => isSelected;

        public void Setup(LifestyleConfig data, Action<LifestyleCardItem> onSelect)
        {
            config = data;
            onSelectedCallback = onSelect;

            if (titleText != null) titleText.text = data.displayName;
            if (subtitleText != null) subtitleText.text = data.subtitle;
            if (incomeText != null) incomeText.text = $"{GameManager.FormatMoney(data.monthlyIncome)}/mo";
            if (iconImage != null && data.icon != null) iconImage.sprite = data.icon;

            if (cardButton != null)
            {
                cardButton.onClick.RemoveAllListeners();
                cardButton.onClick.AddListener(HandleClicked);
            }

            SetSelected(false);
        }

        private void HandleClicked()
        {
            onSelectedCallback?.Invoke(this);
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;

            if (cardBackgroundImage != null)
            {
                cardBackgroundImage.color = isSelected ? selectedBgColor : normalBgColor;
            }

            if (outlineHighlightImage != null)
            {
                outlineHighlightImage.enabled = isSelected;
                outlineHighlightImage.color = selectedOutlineColor;
            }

            transform.localScale = isSelected ? Vector3.one * 1.03f : Vector3.one;
        }
    }
}
