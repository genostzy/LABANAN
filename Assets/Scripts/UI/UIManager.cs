using UnityEngine;
using UnityEngine.UI;

namespace LABANAN
{
    /// <summary>
    /// Manages all in-game UI: health bars, timer, round info, pause, game over.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("References")]
        public Image healthBarRed;
        public Image healthBarBlue;
        public Text timerText;
        public Text roundText;
        public Text scoreRedText;
        public Text scoreBlueText;

        [Header("Overlays")]
        public GameObject labanOverlay;
        public GameObject pauseOverlay;
        public GameObject gameOverOverlay;
        public GameObject redWinOverlay;
        public GameObject blueWinOverlay;

        [Header("Buttons")]
        public Button playAgainButton;
        public Button exitButton;
        public Button resumeButton;
        public Button pauseExitButton;

        [Header("Health Bar Settings")]
        public float maxBarWidth = 650f;
        public int maxHealth = 500;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            // Setup button listeners
            if (playAgainButton != null)
                playAgainButton.onClick.AddListener(OnPlayAgain);
            if (exitButton != null)
                exitButton.onClick.AddListener(OnExit);
            if (resumeButton != null)
                resumeButton.onClick.AddListener(OnResume);
            if (pauseExitButton != null)
                pauseExitButton.onClick.AddListener(OnPauseExit);
        }

        private void Update()
        {
            if (GameManager.Instance == null) return;

            GameState state = GameManager.Instance.currentState;

            UpdateHealthBars(state);
            UpdateTimer(state);
            UpdateRoundInfo(state);
            UpdateOverlays(state);

            // Handle ESC for pause
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (!state.isGameOver && !state.showLaban)
                {
                    GameManager.Instance.TogglePause();
                }
            }
        }

        private void UpdateHealthBars(GameState state)
        {
            if (healthBarRed != null)
            {
                float healthPercent = (float)state.player1.health / maxHealth;
                healthBarRed.fillAmount = healthPercent;
            }

            if (healthBarBlue != null)
            {
                float healthPercent = (float)state.player2.health / maxHealth;
                healthBarBlue.fillAmount = healthPercent;
            }
        }

        private void UpdateTimer(GameState state)
        {
            if (timerText != null && !state.showLaban)
            {
                int seconds = Mathf.Max(0, state.timer);
                timerText.text = string.Format("{0:D2}", seconds);
            }
        }

        private void UpdateRoundInfo(GameState state)
        {
            if (roundText != null)
                roundText.text = $"Round: {state.round}/{GameManager.Instance.maxRounds}";

            if (scoreRedText != null)
                scoreRedText.text = $"Red: {state.player1Wins}";

            if (scoreBlueText != null)
                scoreBlueText.text = $"{state.player2Wins} :Blue";
        }

        private void UpdateOverlays(GameState state)
        {
            SetActive(labanOverlay, state.showLaban);
            SetActive(pauseOverlay, state.isPaused);
            SetActive(gameOverOverlay, state.isGameOver);
            SetActive(redWinOverlay, state.showRedWin && !state.isGameOver);
            SetActive(blueWinOverlay, state.showBlueWin && !state.isGameOver);
        }

        private void SetActive(GameObject obj, bool active)
        {
            if (obj != null && obj.activeSelf != active)
                obj.SetActive(active);
        }

        private void OnPlayAgain()
        {
            GameManager.Instance.ResetGame();
            GameManager.Instance.currentState.showLaban = true;
            GameManager.Instance.currentState.labanTimerFrames = 120;
            AudioManager.Instance?.PlayLevel1();
        }

        private void OnExit()
        {
            NetworkManager.Instance?.Disconnect();
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }

        private void OnResume()
        {
            GameManager.Instance.TogglePause();
        }

        private void OnPauseExit()
        {
            NetworkManager.Instance?.Disconnect();
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }
}
