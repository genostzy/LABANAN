using UnityEngine;
using UnityEngine.UI;

namespace LABANAN
{
    /// <summary>
    /// Main menu UI - Start, Online, Exit buttons.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Buttons")]
        public Button onlineButton;
        public Button exitButton;

        [Header("Panels")]
        public GameObject mainPanel;
        public GameObject lobbyPanel;

        private void Start()
        {
            if (onlineButton != null)
                onlineButton.onClick.AddListener(OnOnline);
            if (exitButton != null)
                exitButton.onClick.AddListener(OnExit);
        }

        private void OnOnline()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("OnlineLobby");
        }

        private void OnExit()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
