using UnityEngine;

namespace LABANAN
{
    public class GameLoop : MonoBehaviour
    {
        [Header("Settings")]
        public int targetTickRate = 60;
        public bool showDebugInfo = true;

        private int currentFrame;
        private bool gameStarted;
        private float tickAccumulator;
        private float tickInterval;

        private InputData localInput;
        private InputData remoteInput;
        private bool debugTogglePressed;

        private SpriteRenderer player1Sprite;
        private SpriteRenderer player2Sprite;
        private Camera mainCam;

        private Sprite[][] redSprites;
        private Sprite[][] blueSprites;
        private int redRows;
        private int blueRows;

        private void Start()
        {
            tickInterval = 1f / targetTickRate;
            mainCam = Camera.main;
            LoadSprites();
        }

        private void LoadSprites()
        {
            redSprites = LoadSpritesheet("Sprites/Red/RED_SPRITESHEET", out redRows);
            blueSprites = LoadSpritesheet("Sprites/Blue/BLUE_SPRITESHEET", out blueRows);

            Debug.Log($"Loaded sprites: Red={redRows} rows, Blue={blueRows} rows");
        }

        private Sprite[][] LoadSpritesheet(string resourcePath, out int rowCount)
        {
            var tex = Resources.Load<Texture2D>(resourcePath);
            if (tex == null)
            {
                Debug.LogError($"Failed to load spritesheet: {resourcePath}");
                rowCount = 0;
                return null;
            }

            int cols = tex.width / 64;
            rowCount = tex.height / 64;

            var sprites = new Sprite[rowCount][];
            for (int row = 0; row < rowCount; row++)
            {
                sprites[row] = new Sprite[cols];
                for (int col = 0; col < cols; col++)
                {
                    var rect = new Rect(col * 64, (rowCount - 1 - row) * 64, 64, 64);
                    var pivot = new Vector2(0.5f, 0f);
                    sprites[row][col] = Sprite.Create(tex, rect, pivot, 64);
                }
            }

            return sprites;
        }

        private Sprite GetSprite(Sprite[][] sheet, int rows, int animState, int animIndex)
        {
            if (sheet == null || animState < 0 || animState >= rows)
                return null;

            var row = sheet[animState];
            if (row == null || animIndex < 0 || animIndex >= row.Length)
                return null;

            return row[animIndex];
        }

        private void Update()
        {
            UnityMainThread.Update();

            if (!gameStarted)
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.StartGame();
                    gameStarted = true;
                    Debug.Log("Game started!");
                }
                return;
            }

            CollectLocalInput();

            if (Input.GetKeyDown(KeyCode.H) && !debugTogglePressed)
            {
                debugTogglePressed = true;
                showDebugInfo = !showDebugInfo;
            }
            if (Input.GetKeyUp(KeyCode.H))
            {
                debugTogglePressed = false;
            }

            UpdateVisuals();
        }

        private void FixedUpdate()
        {
            if (!gameStarted) return;

            if (NetworkManager.Instance != null &&
                NetworkManager.Instance.State == NetworkManager.ConnectionState.Connected)
            {
                NetworkManager.Instance.RecordLocalInput(currentFrame, localInput);
                NetworkManager.Instance.SendInputs(currentFrame);
                HandleRollback();
            }
            else
            {
                remoteInput = localInput;
            }

            tickAccumulator += Time.fixedDeltaTime;

            while (tickAccumulator >= tickInterval)
            {
                tickAccumulator -= tickInterval;

                if (NetworkManager.Instance != null &&
                    NetworkManager.Instance.State == NetworkManager.ConnectionState.Connected)
                {
                    NetworkManager.Instance.GetRemoteInput(currentFrame, out remoteInput);
                }

                GameManager.Instance?.Tick(localInput, remoteInput);
                currentFrame++;
            }
        }

        private void UpdateVisuals()
        {
            if (GameManager.Instance == null) return;

            GameState state = GameManager.Instance.currentState;

            if (player1Sprite == null)
            {
                var p1 = GameObject.Find("Player1_Red");
                if (p1 != null) player1Sprite = p1.GetComponent<SpriteRenderer>();
            }
            if (player2Sprite == null)
            {
                var p2 = GameObject.Find("Player2_Blue");
                if (p2 != null)
                {
                    player2Sprite = p2.GetComponent<SpriteRenderer>();
                    if (player2Sprite != null) player2Sprite.enabled = false;
                }
            }
            if (mainCam == null)
            {
                mainCam = Camera.main;
            }

            if (player1Sprite != null)
            {
                player1Sprite.transform.position = new Vector3(
                    FixedMath.ToFloat(state.player1.x),
                    FixedMath.ToFloat(state.player1.y),
                    0);

                player1Sprite.flipX = false;

                var sprite = GetSprite(redSprites, redRows, state.player1.animState, state.player1.animIndex);
                if (sprite != null)
                    player1Sprite.sprite = sprite;
            }

            if (player1Sprite != null)
            {
                mainCam.transform.position = new Vector3(
                    player1Sprite.transform.position.x,
                    player1Sprite.transform.position.y + 2f,
                    -10f);
            }

            UpdateUI(state);
        }

        private void UpdateUI(GameState state)
        {
            var timerObj = GameObject.Find("TimerText");
            if (timerObj != null)
            {
                var t = timerObj.GetComponent<UnityEngine.UI.Text>();
                if (t != null) t.text = state.timer.ToString();
            }

            var roundObj = GameObject.Find("RoundText");
            if (roundObj != null)
            {
                var t = roundObj.GetComponent<UnityEngine.UI.Text>();
                if (t != null) t.text = $"Round {state.round}";
            }

            var p1w = GameObject.Find("P1WinsText");
            if (p1w != null)
            {
                var t = p1w.GetComponent<UnityEngine.UI.Text>();
                if (t != null) t.text = $"Wins: {state.player1Wins}";
            }

            var p2w = GameObject.Find("P2WinsText");
            if (p2w != null)
            {
                var t = p2w.GetComponent<UnityEngine.UI.Text>();
                if (t != null) t.text = $"Wins: {state.player2Wins}";
            }

            var p1Bar = GameObject.Find("P1HealthBar");
            if (p1Bar != null)
            {
                var img = p1Bar.GetComponent<UnityEngine.UI.Image>();
                if (img != null) img.fillAmount = (float)state.player1.health / PlayerController.MAX_HEALTH;
            }

            var p2Bar = GameObject.Find("P2HealthBar");
            if (p2Bar != null)
            {
                var img = p2Bar.GetComponent<UnityEngine.UI.Image>();
                if (img != null) img.fillAmount = (float)state.player2.health / PlayerController.MAX_HEALTH;
            }
        }

        private void CollectLocalInput()
        {
            localInput = InputData.Create(currentFrame);

            if (Input.GetKey(KeyCode.A))
                localInput.buttons |= InputData.LEFT;
            if (Input.GetKey(KeyCode.D))
                localInput.buttons |= InputData.RIGHT;
            if (Input.GetKey(KeyCode.W))
                localInput.buttons |= InputData.UP;
            if (Input.GetKey(KeyCode.S))
                localInput.buttons |= InputData.DOWN;
            if (Input.GetKeyDown(KeyCode.J))
                localInput.buttons |= InputData.ATTACK;
            if (Input.GetKeyDown(KeyCode.V))
                localInput.buttons |= InputData.SUNGKIT;
            if (Input.GetKeyDown(KeyCode.E))
                localInput.buttons |= InputData.LAUNCH;
            if (Input.GetKey(KeyCode.Q))
                localInput.buttons |= InputData.BLOCK;
        }

        private void HandleRollback()
        {
            if (NetworkManager.Instance == null) return;

            int rollbackFrame = NetworkManager.Instance.CheckForRollback(currentFrame);

            if (rollbackFrame >= 0)
            {
                if (NetworkManager.Instance.TryLoadGameState(rollbackFrame, out GameState rollbackState))
                {
                    GameManager.Instance.LoadState(rollbackState);

                    for (int f = rollbackFrame; f < currentFrame; f++)
                    {
                        NetworkManager.Instance.GetRemoteInput(f, out InputData p1In);
                        NetworkManager.Instance.GetRemoteInput(f, out InputData p2In);

                        InputData local = (NetworkManager.Instance.IsHost) ? p1In : p2In;
                        InputData remote = (NetworkManager.Instance.IsHost) ? p2In : p1In;

                        GameManager.Instance.Tick(local, remote);
                    }

                    NetworkManager.Instance.NotifyRollback(currentFrame - rollbackFrame);
                }
            }
        }

        private void OnGUI()
        {
            if (!showDebugInfo) return;

            int y = 10;
            int lineHeight = 20;

            GUI.Label(new Rect(10, y, 500, lineHeight), $"Frame: {currentFrame}  Buttons: {localInput.buttons}  Focused: {Application.isFocused}");
            y += lineHeight;

            if (GameManager.Instance != null)
            {
                GameState state = GameManager.Instance.currentState;
                var p1 = state.player1;
                var p2 = state.player2;
                GUI.Label(new Rect(10, y, 600, lineHeight),
                    $"P1: x={p1.x} y={p1.y} spd={p1.speed} hp={p1.health} atk={p1.attacking} kb={p1.isKnockedBack} lock={p1.actionLockFramesLeft} ground={p1.isOnGround} anim={p1.animState}[{p1.animIndex}]");
                y += lineHeight;
                GUI.Label(new Rect(10, y, 600, lineHeight),
                    $"P2: x={p2.x} y={p2.y} spd={p2.speed} hp={p2.health} atk={p2.attacking} kb={p2.isKnockedBack} lock={p2.actionLockFramesLeft} ground={p2.isOnGround} anim={p2.animState}[{p2.animIndex}]");
                y += lineHeight;
                GUI.Label(new Rect(10, y, 300, lineHeight), $"Round: {state.round} Timer: {state.timer} showLaban: {state.showLaban}");
            }
        }
    }
}
