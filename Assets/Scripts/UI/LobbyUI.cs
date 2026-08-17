using UnityEngine;
using UnityEngine.UI;

namespace LABANAN
{
    /// <summary>
    /// Online lobby UI - Create Room / Join Room.
    /// </summary>
    public class LobbyUI : MonoBehaviour
    {
        [Header("Panels")]
        public GameObject createRoomPanel;
        public GameObject joinRoomPanel;
        public GameObject waitingPanel;

        [Header("Create Room")]
        public Text yourIpText;
        public Button copyIpButton;
        public Button backButton1;

        [Header("Join Room")]
        public InputField ipInputField;
        public Button connectButton;
        public Button backButton2;

        [Header("Waiting")]
        public Text statusText;
        public Button cancelButton;

        [Header("Buttons")]
        public Button createRoomButton;
        public Button joinRoomButton;

        private void Start()
        {
            ShowMainMenu();

            // Main buttons
            if (createRoomButton != null)
                createRoomButton.onClick.AddListener(OnCreateRoom);
            if (joinRoomButton != null)
                joinRoomButton.onClick.AddListener(OnJoinRoom);

            // Create room
            if (copyIpButton != null)
                copyIpButton.onClick.AddListener(OnCopyIP);
            if (backButton1 != null)
                backButton1.onClick.AddListener(ShowMainMenu);

            // Join room
            if (connectButton != null)
                connectButton.onClick.AddListener(OnConnect);
            if (backButton2 != null)
                backButton2.onClick.AddListener(ShowMainMenu);

            // Waiting
            if (cancelButton != null)
                cancelButton.onClick.AddListener(OnCancel);

            // Setup lobby callbacks
            LobbyManager.Instance.OnLobbyCreated += OnLobbyCreated;
            LobbyManager.Instance.OnLobbyJoined += OnLobbyJoined;
            LobbyManager.Instance.OnLobbyError += OnLobbyError;
        }

        private void ShowMainMenu()
        {
            SetPanelActive(createRoomPanel, false);
            SetPanelActive(joinRoomPanel, false);
            SetPanelActive(waitingPanel, false);
        }

        private void OnCreateRoom()
        {
            SetPanelActive(createRoomPanel, true);
            SetPanelActive(joinRoomPanel, false);
            SetPanelActive(waitingPanel, false);

            if (yourIpText != null)
                yourIpText.text = $"Your IP:\n{LobbyManager.Instance.LocalIPAddress}\n\nShare this with your opponent";
        }

        private void OnCopyIP()
        {
            GUIUtility.systemCopyBuffer = LobbyManager.Instance.LocalIPAddress;
            if (statusText != null)
                statusText.text = "IP copied to clipboard!";
        }

        private void OnJoinRoom()
        {
            SetPanelActive(createRoomPanel, false);
            SetPanelActive(joinRoomPanel, true);
            SetPanelActive(waitingPanel, false);
        }

        private void OnConnect()
        {
            string ip = ipInputField?.text?.Trim();
            if (string.IsNullOrEmpty(ip))
            {
                OnLobbyError("Please enter an IP address");
                return;
            }

            SetPanelActive(createRoomPanel, false);
            SetPanelActive(joinRoomPanel, false);
            SetPanelActive(waitingPanel, true);

            if (statusText != null)
                statusText.text = "Connecting...";

            LobbyManager.Instance.JoinLobby(ip);
        }

        private void OnCancel()
        {
            LobbyManager.Instance.LeaveLobby();
            ShowMainMenu();
        }

        private void OnLobbyCreated()
        {
            SetPanelActive(waitingPanel, true);
            if (statusText != null)
                statusText.text = "Waiting for opponent to connect...";
        }

        private void OnLobbyJoined()
        {
            // Start the game
            UnityEngine.SceneManagement.SceneManager.LoadScene("Game");
        }

        private void OnLobbyError(string error)
        {
            if (statusText != null)
                statusText.text = $"Error: {error}";
        }

        private void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null)
                panel.SetActive(active);
        }

        private void OnDestroy()
        {
            if (LobbyManager.Instance != null)
            {
                LobbyManager.Instance.OnLobbyCreated -= OnLobbyCreated;
                LobbyManager.Instance.OnLobbyJoined -= OnLobbyJoined;
                LobbyManager.Instance.OnLobbyError -= OnLobbyError;
            }
        }
    }
}
