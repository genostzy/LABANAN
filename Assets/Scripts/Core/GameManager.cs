using UnityEngine;

namespace LABANAN
{
    /// <summary>
    /// Core game manager handling rounds, timer, and game state.
    /// Designed for rollback netcode - all logic is deterministic.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game Settings")]
        public int maxRounds = 4;
        public int winsNeeded = 2;
        public int roundTimerSeconds = 60;

        // Current game state
        public GameState currentState;
        private PlatformManager platforms = new PlatformManager();
        private bool gameRunning;

        // Spawn positions (fixed-point)
        private const int P1_SPAWN_X = 6000;
        private const int P1_SPAWN_Y = 500;
        private const int P2_SPAWN_X = 12000;
        private const int P2_SPAWN_Y = 500;

        // Frame timing
        private const int FRAMES_PER_SECOND = 60;
        private const int LABAN_DURATION = 120; // 2 seconds
        private const int WIN_DISPLAY_DURATION = 72; // 1.2 seconds

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void StartGame()
        {
            currentState = GameState.CreateDefault();
            gameRunning = true;

            // Register with network manager
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.SetGameManager(this);
            }
        }

        /// <summary>
        /// Main game loop tick. Called at fixed 60fps.
        /// </summary>
        public void Tick(InputData p1Input, InputData p2Input)
        {
            if (!gameRunning) return;

            // Save state for rollback
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.SaveGameState(currentState.frame, currentState);
            }

            // Handle LABAN splash screen
            if (currentState.showLaban)
            {
                currentState.labanTimerFrames--;
                if (currentState.labanTimerFrames <= 0)
                {
                    currentState.showLaban = false;
                }
                currentState.frame++;
                return;
            }

            // Handle pause
            if (currentState.isPaused)
            {
                currentState.frame++;
                return;
            }

            // Handle win display
            if (currentState.showBlueWin || currentState.showRedWin)
            {
                currentState.winDisplayTimerFrames--;
                if (currentState.winDisplayTimerFrames <= 0)
                {
                    currentState.showBlueWin = false;
                    currentState.showRedWin = false;
                }
                currentState.frame++;
                return;
            }

            // Handle game over
            if (currentState.isGameOver)
            {
                currentState.frame++;
                return;
            }

            // Update timer
            UpdateTimer();

            // Update players
            currentState.player1 = PlayerController.Update(currentState.player1, p1Input, platforms);
            currentState.player2 = PlayerController.Update(currentState.player2, p2Input, platforms);

            // Check attack collisions
            CheckCombat();

            // Check death zone
            CheckDeathZone();

            // Check round end
            CheckRoundEnd();

            currentState.frame++;
        }

        private void UpdateTimer()
        {
            currentState.timerFrameCounter++;
            if (currentState.timerFrameCounter >= FRAMES_PER_SECOND)
            {
                currentState.timerFrameCounter = 0;
                currentState.timer--;

                if (currentState.timer <= 0)
                {
                    // Time's up - whoever has more HP wins
                    if (currentState.player1.health > currentState.player2.health)
                        DeclareWinner(1);
                    else if (currentState.player2.health > currentState.player1.health)
                        DeclareWinner(2);
                    else
                        ResetRound(); // Draw
                }
            }
        }

        private void CheckCombat()
        {
            // Player 1 attacks Player 2
            var (p1Damage, p2Damage, p1KB, p2KB) = PlayerController.CheckAttackCollisions(
                currentState.player1, currentState.player2);

            if (p2Damage > 0)
            {
                currentState.player2.health -= p2Damage;
                if (currentState.player2.health < 0) currentState.player2.health = 0;

            if (p2KB)
            {
                currentState.player2.isKnockedBack = true;
                currentState.player2.knockbackDirection = currentState.player1.facingLeft ? -1 : 1;
                currentState.player2.knockbackTimer = PlayerController.KNOCKBACK_DURATION;
                Debug.Log($"[COMBAT] P1 hit P2! P2 knockback from x={currentState.player2.x}");
            }
            }

            if (p1KB)
            {
                currentState.player1.isKnockedBack = true;
                currentState.player1.knockbackDirection = currentState.player1.facingLeft ? 1 : -1;
                currentState.player1.knockbackTimer = PlayerController.KNOCKBACK_DURATION;
            }

            // Player 2 attacks Player 1
            var (p2Damage2, p1Damage2, p2KB2, p1KB2) = PlayerController.CheckAttackCollisions(
                currentState.player2, currentState.player1);

            if (p1Damage2 > 0)
            {
                currentState.player1.health -= p1Damage2;
                if (currentState.player1.health < 0) currentState.player1.health = 0;

            if (p1KB2)
            {
                currentState.player1.isKnockedBack = true;
                currentState.player1.knockbackDirection = currentState.player2.facingLeft ? -1 : 1;
                currentState.player1.knockbackTimer = PlayerController.KNOCKBACK_DURATION;
                Debug.Log($"[COMBAT] P2 hit P1! P1 knockback from x={currentState.player1.x}");
            }
            }

            if (p2KB2)
            {
                currentState.player2.isKnockedBack = true;
                currentState.player2.knockbackDirection = currentState.player2.facingLeft ? 1 : -1;
                currentState.player2.knockbackTimer = PlayerController.KNOCKBACK_DURATION;
            }
        }

        private void CheckDeathZone()
        {
            if (platforms.IsOnDeathZone(currentState.player1))
            {
                Debug.Log($"[DEATH] P1 fell off stage at y={currentState.player1.y}");
                currentState.player1.health = 0;
            }

            if (platforms.IsOnDeathZone(currentState.player2))
            {
                Debug.Log($"[DEATH] P2 fell off stage at y={currentState.player2.y}");
                currentState.player2.health = 0;
            }
        }

        private void CheckRoundEnd()
        {
            bool p1Dead = currentState.player1.health <= 0;
            bool p2Dead = currentState.player2.health <= 0;

            if (p1Dead && p2Dead)
            {
                // Draw - no one wins
                ResetRound();
            }
            else if (p1Dead)
            {
                // Player 2 wins
                DeclareWinner(2);
            }
            else if (p2Dead)
            {
                // Player 1 wins
                DeclareWinner(1);
            }
        }

        private void DeclareWinner(int player)
        {
            if (player == 1)
            {
                currentState.player1Wins++;
                currentState.showRedWin = true;
            }
            else
            {
                currentState.player2Wins++;
                currentState.showBlueWin = true;
            }
            currentState.winDisplayTimerFrames = WIN_DISPLAY_DURATION;

            // Check for game over
            if (currentState.player1Wins >= winsNeeded || currentState.player2Wins >= winsNeeded)
            {
                currentState.isGameOver = true;
            }
            else
            {
                ResetRound();
            }
        }

        public void ResetRound()
        {
            if (currentState.round < maxRounds)
            {
                currentState.round++;
            }
            else
            {
                currentState.round = 1;
                currentState.player1Wins = 0;
                currentState.player2Wins = 0;
            }

            currentState.player1 = PlayerState.CreateDefault(P1_SPAWN_X, P1_SPAWN_Y, false);
            currentState.player2 = PlayerState.CreateDefault(P2_SPAWN_X, P2_SPAWN_Y, true);
            currentState.timer = roundTimerSeconds;
            currentState.timerFrameCounter = 0;
            currentState.isGameOver = false;
            currentState.showBlueWin = false;
            currentState.showRedWin = false;
            currentState.winDisplayTimerFrames = 0;
            currentState.showLaban = false;
            currentState.labanTimerFrames = 0;

            if (currentState.round == 3 && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayLevel2();
            }
        }

        public void ResetGame()
        {
            currentState.round = 1;
            currentState.player1Wins = 0;
            currentState.player2Wins = 0;
            currentState.timer = roundTimerSeconds;
            currentState.timerFrameCounter = 0;
            currentState.player1 = PlayerState.CreateDefault(P1_SPAWN_X, P1_SPAWN_Y, false);
            currentState.player2 = PlayerState.CreateDefault(P2_SPAWN_X, P2_SPAWN_Y, true);
        }

        /// <summary>
        /// Load a game state for rollback re-simulation.
        /// </summary>
        public void LoadState(GameState state)
        {
            currentState = state;
        }

        public void SetPaused(bool paused)
        {
            currentState.isPaused = paused;
        }

        public void TogglePause()
        {
            currentState.isPaused = !currentState.isPaused;
        }

        public PlatformManager Platforms => platforms;
    }
}
