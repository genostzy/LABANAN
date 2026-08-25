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
        public int maxRounds = 10;
        public int winsNeeded = 3;
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
        private const int LABAN_DURATION = 120;
        private const int WIN_DISPLAY_DURATION = 72;
        private const int ROUND_START_DURATION = 90; // 1.5s pwesto lock

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
            currentState.roundStartTimer = ROUND_START_DURATION;
            gameRunning = true;

            if (NetworkManager.Instance != null)
                NetworkManager.Instance.SetGameManager(this);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayBGM();
                AudioManager.Instance.PlayPwesto();
            }
        }

        public void Tick(InputData p1Input, InputData p2Input)
        {
            if (!gameRunning) return;

            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.SaveGameState(currentState.frame, currentState);
            }

            if (currentState.showLaban)
            {
                currentState.labanTimerFrames--;
                if (currentState.labanTimerFrames <= 0)
                {
                    currentState.showLaban = false;
                }
            }

            if (currentState.roundStartTimer > 0)
            {
                currentState.roundStartTimer--;
                currentState.frame++;
                return;
            }

            if (currentState.isPaused)
            {
                currentState.frame++;
                return;
            }

            if (currentState.showBlueWin || currentState.showRedWin)
            {
                currentState.winDisplayTimerFrames--;
                if (currentState.winDisplayTimerFrames <= 0)
                {
                    currentState.showBlueWin = false;
                    currentState.showRedWin = false;
                    if (!currentState.isGameOver)
                        ResetRound();
                }
                currentState.frame++;
                return;
            }

            if (currentState.isGameOver)
            {
                currentState.frame++;
                return;
            }

            UpdateTimer();

            int prevP1YVel = currentState.player1.yVelocity;
            int prevP2YVel = currentState.player2.yVelocity;

            currentState.player1 = PlayerController.Update(currentState.player1, p1Input, platforms);
            currentState.player2 = PlayerController.Update(currentState.player2, p2Input, platforms);

            if (currentState.player1.yVelocity > 0 && prevP1YVel <= 0 && AudioManager.Instance != null)
                AudioManager.Instance.PlayJump();
            if (currentState.player2.yVelocity > 0 && prevP2YVel <= 0 && AudioManager.Instance != null)
                AudioManager.Instance.PlayJump();

            CheckCombat();

            CheckDeathZone();
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
                    if (currentState.player1.health > currentState.player2.health)
                        DeclareWinner(1);
                    else if (currentState.player2.health > currentState.player1.health)
                        DeclareWinner(2);
                    else
                        ResetRound();
                }
            }
        }

        private void CheckCombat()
        {
            // P1 attacks P2
            var p1Hit = PlayerController.CheckAttackCollisions(
                currentState.player1, currentState.player2);

            bool p1Attacking = currentState.player1.attacking || currentState.player1.sungkit || currentState.player1.launch;
            bool p1JustStarted = currentState.player1.attackStartupFrames == PlayerController.ATTACK_STARTUP;

            if (p1Hit.damage > 0)
            {
                currentState.player2.health -= p1Hit.damage;
                if (currentState.player2.health < 0) currentState.player2.health = 0;

                Debug.Log($"[COMBAT] P1 hit P2 for {p1Hit.damage} (type={p1Hit.attackType})");

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayAttack2();
                    AudioManager.Instance.PlayHurt();
                }

                if (p1Hit.isSungkit)
                {
                    currentState.player2.slowTimer = PlayerController.SUNGKIT_SLOW_DURATION;
                    Debug.Log($"[DEBUFF] P2 slowed for {PlayerController.SUNGKIT_SLOW_DURATION} frames");
                }
            }
            else if (p1JustStarted && p1Attacking && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayAttack1();
            }

            if (p1Hit.knockbackDefender)
            {
                currentState.player2.isKnockedBack = true;
                currentState.player2.knockbackDirection = currentState.player1.facingLeft ? -1 : 1;
                currentState.player2.knockbackTimer = PlayerController.KNOCKBACK_DURATION;
            }

            if (p1Hit.knockbackAttacker)
            {
                currentState.player1.isKnockedBack = true;
                currentState.player1.knockbackDirection = currentState.player1.facingLeft ? 1 : -1;
                currentState.player1.knockbackTimer = p1Hit.perfectParry ? PlayerController.KNOCKBACK_DURATION : PlayerController.KNOCKBACK_DURATION;

                if (p1Hit.perfectParry)
                {
                    int heal = currentState.player2.health + PlayerController.PARRY_HEAL;
                    currentState.player2.health = Min(heal, PlayerController.MAX_HEALTH);
                    Debug.Log($"[PARRY] P2 perfect parry! Healed {PlayerController.PARRY_HEAL} HP");
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayBlock();
                }
            }

            // P2 attacks P1
            var p2Hit = PlayerController.CheckAttackCollisions(
                currentState.player2, currentState.player1);

            bool p2Attacking = currentState.player2.attacking || currentState.player2.sungkit || currentState.player2.launch;
            bool p2JustStarted = currentState.player2.attackStartupFrames == PlayerController.ATTACK_STARTUP;

            if (p2Hit.damage > 0)
            {
                currentState.player1.health -= p2Hit.damage;
                if (currentState.player1.health < 0) currentState.player1.health = 0;

                Debug.Log($"[COMBAT] P2 hit P1 for {p2Hit.damage} (type={p2Hit.attackType})");

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayAttack2();
                    AudioManager.Instance.PlayHurt();
                }

                if (p2Hit.isSungkit)
                {
                    currentState.player1.slowTimer = PlayerController.SUNGKIT_SLOW_DURATION;
                    Debug.Log($"[DEBUFF] P1 slowed for {PlayerController.SUNGKIT_SLOW_DURATION} frames");
                }
            }
            else if (p2JustStarted && p2Attacking && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayAttack1();
            }

            if (p2Hit.knockbackDefender)
            {
                currentState.player1.isKnockedBack = true;
                currentState.player1.knockbackDirection = currentState.player2.facingLeft ? -1 : 1;
                currentState.player1.knockbackTimer = PlayerController.KNOCKBACK_DURATION;
            }

            if (p2Hit.knockbackAttacker)
            {
                currentState.player2.isKnockedBack = true;
                currentState.player2.knockbackDirection = currentState.player2.facingLeft ? 1 : -1;
                currentState.player2.knockbackTimer = PlayerController.KNOCKBACK_DURATION;

                if (p2Hit.perfectParry)
                {
                    int heal = currentState.player1.health + PlayerController.PARRY_HEAL;
                    currentState.player1.health = Min(heal, PlayerController.MAX_HEALTH);
                    Debug.Log($"[PARRY] P1 perfect parry! Healed {PlayerController.PARRY_HEAL} HP");
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayBlock();
                }
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
                currentState.showRedWin = true;
                currentState.showBlueWin = true;
                currentState.winDisplayTimerFrames = WIN_DISPLAY_DURATION;
                if (AudioManager.Instance != null) AudioManager.Instance.PlayDraw();
            }
            else if (p1Dead)
            {
                DeclareWinner(2);
            }
            else if (p2Dead)
            {
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

            if (currentState.player1Wins >= winsNeeded || currentState.player2Wins >= winsNeeded)
            {
                currentState.isGameOver = true;
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
            currentState.showLaban = true;
            currentState.labanTimerFrames = LABAN_DURATION;
            currentState.roundStartTimer = ROUND_START_DURATION;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPwesto();
                if (currentState.round >= 3)
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

        private static int Min(int a, int b) => a < b ? a : b;

        public PlatformManager Platforms => platforms;
    }
}
