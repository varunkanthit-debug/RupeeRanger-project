using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using RupeeRanger.Core;

namespace RupeeRanger.UI
{
    public class ScenePlaceholderUI : MonoBehaviour
    {
        [Header("Scene Header")]
        public string sceneTitle = "Expense Setup";
        public TMP_Text titleText;
        public TMP_Text playerSummaryText;
        public Button backButton;

        private void Start()
        {
            if (titleText != null)
            {
                titleText.text = sceneTitle;
            }

            if (playerSummaryText != null && GameManager.Instance != null)
            {
                playerSummaryText.text = $"Player: <b>{GameManager.Instance.playerName}</b> (Age {GameManager.Instance.playerAge}) | Lifestyle: <b>{GameManager.Instance.selectedLifestyle}</b> ({GameManager.FormatMoney(GameManager.Instance.monthlyIncome)}/mo) | Character: <b>{GameManager.Instance.selectedCharacterId}</b>";
            }

            if (backButton != null)
            {
                backButton.onClick.AddListener(() => SceneManager.LoadScene("PlayerSetup"));
            }
        }
    }
}
