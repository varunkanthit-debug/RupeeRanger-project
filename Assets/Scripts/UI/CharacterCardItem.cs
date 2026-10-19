using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RupeeRanger.Data;
using RupeeRanger.Core;

namespace RupeeRanger.UI
{
    public class CharacterCardItem : MonoBehaviour
    {
        [Header("UI Elements")]
        public Button cardButton;
        public Image cardBackgroundImage;
        public Image outlineHighlightImage;
        public Image characterAvatarImage;
        public TMP_Text characterNameText;

        [Header("Lock Overlay")]
        public GameObject lockOverlay;
        public Image lockIconImage;
        public TMP_Text pointsNeededText;

        [Header("Colors")]
        public Color normalBgColor = new Color32(0x1B, 0x18, 0x42, 0xFF);
        public Color selectedBgColor = new Color32(0x27, 0x22, 0x5E, 0xFF);
        public Color selectedOutlineColor = new Color32(0xFF, 0x40, 0x81, 0xFF); // Coral Pink Glow

        private CharacterData characterData;
        private Action<CharacterCardItem> onSelectedCallback;
        private Action<string> onLockedClickedCallback;
        private bool isUnlocked = false;
        private bool isSelected = false;

        public CharacterData Character => characterData;
        public bool IsUnlocked => isUnlocked;
        public bool IsSelected => isSelected;

        public void Setup(
            CharacterData data,
            bool unlocked,
            Action<CharacterCardItem> onSelect,
            Action<string> onLockedClick)
        {
            characterData = data;
            isUnlocked = unlocked;
            onSelectedCallback = onSelect;
            onLockedClickedCallback = onLockedClick;

            if (characterNameText != null) characterNameText.text = data.characterName;
            if (characterAvatarImage != null && data.sprite != null)
            {
                characterAvatarImage.sprite = data.sprite;
                characterAvatarImage.color = isUnlocked ? Color.white : new Color(0.6f, 0.6f, 0.6f, 0.5f);
            }

            if (lockOverlay != null)
            {
                lockOverlay.SetActive(!isUnlocked);
            }

            if (pointsNeededText != null)
            {
                pointsNeededText.text = $"Needs {data.unlockScore} pts";
            }

            if (cardButton != null)
            {
                cardButton.onClick.RemoveAllListeners();
                cardButton.onClick.AddListener(HandleClicked);
            }

            SetSelected(false);
        }

        private void HandleClicked()
        {
            if (isUnlocked)
            {
                onSelectedCallback?.Invoke(this);
            }
            else
            {
                onLockedClickedCallback?.Invoke($"Locked! Earn {characterData.unlockScore} career points to unlock {characterData.characterName}.");
            }
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
                outlineHighlightImage.enabled = isSelected && isUnlocked;
                outlineHighlightImage.color = selectedOutlineColor;
            }

            transform.localScale = (isSelected && isUnlocked) ? Vector3.one * 1.05f : Vector3.one;
        }
    }
}
