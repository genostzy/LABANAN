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
        private InputData bufferedInput;

        private SpriteRenderer player1Sprite;
        private SpriteRenderer player2Sprite;
        private Camera mainCam;
        private GameObject background;
        private SpriteRenderer bgSR;

        private Sprite[][] redSprites;
        private Sprite[][] blueSprites;
        private int redRows;
        private int blueRows;

        private bool debugDummyMode;
        private bool debugAttackSpam;

        private void Start()
        {
            tickInterval = 1f / targetTickRate;
            mainCam = Camera.main;
            LoadSprites();
            SetupBackground();
            SetupPlatforms();
            Debug.Log("GameLoop.Start() complete");
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

        private void SetupBackground()
        {
            background = GameObject.Find("Background");
            if (background == null)
            {
                background = new GameObject("Background");
                background.AddComponent<SpriteRenderer>();
            }
            bgSR = background.GetComponent<SpriteRenderer>();
            if (bgSR == null) bgSR = background.AddComponent<SpriteRenderer>();
            bgSR.sortingOrder = -1;

            var tex = Resources.Load<Texture2D>("Sprites/BG NIGHT");
            if (tex != null)
            {
                bgSR.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width / 20);
                bgSR.color = new Color(2f, 2f, 2f);
            }
            else
            {
                var fallback = new Texture2D(4, 4);
                var px = new Color[16];
                for (int i = 0; i < 16; i++) px[i] = new Color(0.05f, 0.05f, 0.12f);
                fallback.SetPixels(px);
                fallback.Apply();
                bgSR.sprite = Sprite.Create(fallback, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
            }

            background.transform.position = new Vector3(0f, 0f, 5);
            background.transform.localScale = Vector3.one;
        }

        private void SetupPlatforms()
        {
            // Main platform - match collision: X=3.0-15.0, Y=0.0-0.5
            CreateSolidPlatform("PlatformVisuals", new Vector3(9f, 0.25f, 3), new Vector3(12f, 0.5f, 1f), new Color(0.3f, 0.35f, 0.3f));

            // Left platform - match collision: X=1.5-4.5, Y=3.0-3.5
            CreateSolidPlatform("LeftPlatformVisual", new Vector3(3f, 3.25f, 3), new Vector3(3f, 0.5f, 1f), new Color(0.35f, 0.3f, 0.3f));

            // Right platform - match collision: X=13.5-16.5, Y=3.0-3.5
            CreateSolidPlatform("RightPlatformVisual", new Vector3(15f, 3.25f, 3), new Vector3(3f, 0.5f, 1f), new Color(0.3f, 0.3f, 0.35f));

            HidePlatformObject("MainPlatform");
            HidePlatformObject("LeftPlatform");
            HidePlatformObject("RightPlatform");
        }

        private void CreateSolidPlatform(string name, Vector3 pos, Vector3 scale, Color color)
        {
            var obj = new GameObject(name);
            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 1;
            sr.sprite = CreateSquareSprite();
            sr.color = color;
            obj.transform.position = pos;
            obj.transform.localScale = scale;
        }

        private void HidePlatformObject(string name)
        {
            var obj = GameObject.Find(name);
            if (obj != null)
            {
                var sr = obj.GetComponent<SpriteRenderer>();
                if (sr != null) sr.enabled = false;
            }
        }

        private static Sprite CreateSquareSprite()
        {
            var tex = new Texture2D(4, 4);
            var colors = new Color[16];
            for (int i = 0; i < 16; i++) colors[i] = Color.white;
            tex.SetPixels(colors);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
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

            // Buffer one-shot inputs in Update so they aren't missed by FixedUpdate
            if (Input.GetKeyDown(KeyCode.J))
                bufferedInput.buttons |= InputData.ATTACK;
            if (Input.GetKeyDown(KeyCode.K))
                bufferedInput.buttons |= InputData.SUNGKIT;
            if (Input.GetKeyDown(KeyCode.L))
                bufferedInput.buttons |= InputData.LAUNCH;

            if (Input.GetKeyDown(KeyCode.H) && !debugTogglePressed)
            {
                debugTogglePressed = true;
                showDebugInfo = !showDebugInfo;
            }
            if (Input.GetKeyUp(KeyCode.H))
            {
                debugTogglePressed = false;
            }

            // Unstuck - reset player to spawn
            if (Input.GetKeyDown(KeyCode.F1) && GameManager.Instance != null)
            {
                var s = GameManager.Instance.currentState;
                s.player1.x = 6000;
                s.player1.y = 500;
                s.player1.speed = 0;
                s.player1.yVelocity = 0;
                s.player1.isKnockedBack = false;
                s.player1.attacking = false;
                s.player1.sungkit = false;
                s.player1.launch = false;
                s.player1.launchTimer = 0;
                s.player1.blocking = false;
                s.player1.isOnGround = true;
                GameManager.Instance.currentState = s;
                Debug.Log("Unstuck!");
            }

            // Debug: Reset round
            if (Input.GetKeyDown(KeyCode.F2) && GameManager.Instance != null)
            {
                GameManager.Instance.ResetRound();
                Debug.Log("Round reset!");
            }

            // Debug: Toggle blue dummy (no input)
            if (Input.GetKeyDown(KeyCode.F3))
            {
                debugDummyMode = !debugDummyMode;
                if (debugDummyMode) debugAttackSpam = false;
                Debug.Log($"Blue dummy: {(debugDummyMode ? "ON" : "OFF")}");
            }

            // Debug: Toggle blue attack spam
            if (Input.GetKeyDown(KeyCode.F4))
            {
                debugAttackSpam = !debugAttackSpam;
                if (debugAttackSpam) debugDummyMode = false;
                Debug.Log($"Blue attack spam: {(debugAttackSpam ? "ON" : "OFF")}");
            }

            UpdateVisuals();
        }

        private void LateUpdate()
        {
            if (background == null || bgSR == null || bgSR.sprite == null || mainCam == null)
                return;

            float orthoH = mainCam.orthographicSize * 2f;
            float orthoW = orthoH * mainCam.aspect;

            float spriteW = bgSR.sprite.bounds.size.x;
            float spriteH = bgSR.sprite.bounds.size.y;

            float scaleX = orthoW / spriteW;
            float scaleY = orthoH / spriteH;
            float scale = Mathf.Max(scaleX, scaleY);

            background.transform.localScale = new Vector3(scale, scale, 1f);
            background.transform.position = new Vector3(
                mainCam.transform.position.x,
                mainCam.transform.position.y,
                5f);
        }

        private void FixedUpdate()
        {
            if (!gameStarted) return;

            CollectLocalInput();

            // Consume buffered one-shot inputs
            localInput.buttons |= bufferedInput.buttons;
            bufferedInput = InputData.Create(0);

            if (NetworkManager.Instance != null &&
                NetworkManager.Instance.State == NetworkManager.ConnectionState.Connected)
            {
                NetworkManager.Instance.RecordLocalInput(currentFrame, localInput);
                NetworkManager.Instance.SendInputs(currentFrame);
                HandleRollback();
            }
            else
            {
                if (debugDummyMode)
                {
                    remoteInput = InputData.Create(currentFrame);
                }
                else if (debugAttackSpam)
                {
                    remoteInput = InputData.Create(currentFrame);
                    remoteInput.buttons |= InputData.ATTACK;
                }
                else
                {
                    remoteInput = InputData.Create(currentFrame);
                }
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
                }
            }
            if (mainCam == null)
            {
                mainCam = Camera.main;
            }

            bool showP2 = debugDummyMode || debugAttackSpam ||
                (NetworkManager.Instance != null && NetworkManager.Instance.State == NetworkManager.ConnectionState.Connected);

            if (player2Sprite != null)
            {
                player2Sprite.enabled = showP2;

                if (showP2)
                {
                    player2Sprite.transform.position = new Vector3(
                        FixedMath.ToFloat(state.player2.x),
                        FixedMath.ToFloat(state.player2.y),
                        0);

                    player2Sprite.flipX = state.player2.facingLeft;

                    var sprite = GetSprite(blueSprites, blueRows, state.player2.animState, state.player2.animIndex);
                    if (sprite != null)
                        player2Sprite.sprite = sprite;
                }
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
                float playerY = player1Sprite.transform.position.y;
                float baseCamY = 1.5f;
                float camY = baseCamY + playerY * 0.3f;

                float baseOrtho = 3.5f;
                float orthoSize = baseOrtho + Mathf.Abs(playerY - baseCamY) * 0.5f;
                orthoSize = Mathf.Clamp(orthoSize, 3f, 8f);

                mainCam.transform.position = new Vector3(
                    player1Sprite.transform.position.x,
                    camY,
                    -10f);
                mainCam.orthographicSize = orthoSize;
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

            // Health bars
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

            // Stamina bars
            var p1Stam = GameObject.Find("P1StaminaBar");
            if (p1Stam != null)
            {
                var img = p1Stam.GetComponent<UnityEngine.UI.Image>();
                if (img != null)
                {
                    img.fillAmount = (float)state.player1.stamina / PlayerController.MAX_STAMINA;
                    img.color = state.player1.slowTimer > 0 ? new Color(0.5f, 0.3f, 1f) : new Color(0.2f, 0.8f, 0.2f);
                }
            }

            var p2Stam = GameObject.Find("P2StaminaBar");
            if (p2Stam != null)
            {
                var img = p2Stam.GetComponent<UnityEngine.UI.Image>();
                if (img != null)
                {
                    img.fillAmount = (float)state.player2.stamina / PlayerController.MAX_STAMINA;
                    img.color = state.player2.slowTimer > 0 ? new Color(0.5f, 0.3f, 1f) : new Color(0.2f, 0.8f, 0.2f);
                }
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
            if (Input.GetKey(KeyCode.Space))
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

            GUI.Label(new Rect(10, y, 500, lineHeight), $"Frame: {currentFrame}  Focused: {Application.isFocused}");
            y += lineHeight;

            string left = Input.GetKey(KeyCode.A) ? "[A]" : " A ";
            string right = Input.GetKey(KeyCode.D) ? "[D]" : " D ";
            string up = Input.GetKey(KeyCode.W) ? "[W]" : " W ";
            string down = Input.GetKey(KeyCode.S) ? "[S]" : " S ";
            string attack = Input.GetKey(KeyCode.J) ? "[J]" : " J ";
            string sungkit = Input.GetKey(KeyCode.K) ? "[K]" : " K ";
            string launch = Input.GetKey(KeyCode.L) ? "[L]" : " L ";
            string block = Input.GetKey(KeyCode.Space) ? "[SPC]" : " SPC ";
            string unstuck = Input.GetKey(KeyCode.F1) ? "[F1]" : " F1 ";
            string reset = Input.GetKey(KeyCode.F2) ? "[F2]" : " F2 ";
            string dummy = debugDummyMode ? "[F3*]" : " F3 ";
            string atkSpam = debugAttackSpam ? "[F4*]" : " F4 ";
            GUI.Label(new Rect(10, y, 900, lineHeight),
                $"INPUT: {left} {right} {up} {down} {attack} {sungkit} {launch} {block} {unstuck} {reset} {dummy} {atkSpam}");
            y += lineHeight;

            if (GameManager.Instance != null)
            {
                GameState state = GameManager.Instance.currentState;
                var p1 = state.player1;
                GUI.Label(new Rect(10, y, 900, lineHeight),
                    $"P1: x={p1.x} y={p1.y} hp={p1.health} stm={p1.stamina} slow={p1.slowTimer} atk={p1.attacking} sung={p1.sungkit} launch={p1.launch} blk={p1.blocking} blkTmr={p1.blockTimer} gnd={p1.isOnGround} anim={p1.animState}[{p1.animIndex}]");
                y += lineHeight;
                GUI.Label(new Rect(10, y, 300, lineHeight), $"Round: {state.round} Timer: {state.timer} showLaban: {state.showLaban}");
                y += lineHeight;
                string debugModes = "";
                if (debugDummyMode) debugModes += " DUMMY";
                if (debugAttackSpam) debugModes += " ATK_SPAM";
                if (debugModes.Length > 0)
                {
                    GUI.Label(new Rect(10, y, 300, lineHeight), $"DEBUG:{debugModes}");
                    y += lineHeight;
                }

                DrawHitboxes(state);
            }
        }

        private void DrawHitboxes(GameState state)
        {
            if (mainCam == null) return;

            DrawPlayerHitbox(state.player1, Color.green);
            DrawAttackHitbox(state.player1, Color.red);
        }

        private void DrawPlayerHitbox(PlayerState player, Color color)
        {
            Rect hitbox = PlayerController.GetPlayerHitbox(player);
            DrawRect(hitbox, color, 0.3f);
        }

        private void DrawAttackHitbox(PlayerState player, Color color)
        {
            if (!player.attacking && !player.sungkit && !player.launch) return;

            Rect hitbox = PlayerController.GetAttackHitbox(player);
            if (hitbox.width > 0 && hitbox.height > 0)
                DrawRect(hitbox, color, 0.5f);
        }

        private void DrawRect(Rect worldRect, Color color, float alpha)
        {
            Vector3 bl = mainCam.WorldToScreenPoint(new Vector3(worldRect.x, worldRect.y, 0));
            Vector3 tr = mainCam.WorldToScreenPoint(new Vector3(worldRect.x + worldRect.width, worldRect.y + worldRect.height, 0));

            float x = Mathf.Min(bl.x, tr.x);
            float yScreen = Screen.height - Mathf.Max(bl.y, tr.y);
            float w = Mathf.Abs(tr.x - bl.x);
            float h = Mathf.Abs(tr.y - bl.y);

            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, new Color(color.r, color.g, color.b, alpha));
            tex.Apply();

            GUI.DrawTexture(new Rect(x, yScreen, w, h), tex);
        }
    }
}
