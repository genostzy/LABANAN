using UnityEngine;
using UnityEngine.UI;

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

        // ── UI references ──
        private GameObject canvasObj;
        private Text timerText;
        private Text roundText;
        private Text labanText;
        private Image p1HealthBar;
        private Image p2HealthBar;
        private Image p1StaminaBar;
        private Image p2StaminaBar;
        private Text p1NameTag;
        private Text p2NameTag;
        private Text p1CooldownText;
        private Text p2CooldownText;
        private Image[] p1WinDots = new Image[3];
        private Image[] p2WinDots = new Image[3];
        private static Sprite whiteSprite;

        private void Start()
        {
            tickInterval = 1f / targetTickRate;
            mainCam = Camera.main;
            LoadSprites();
            SetupBackground();
            SetupPlatforms();
            SetupUI();
            Debug.Log("GameLoop.Start() complete");
        }

        // ──────────────────────────────────────────────
        //  SPRITES
        // ──────────────────────────────────────────────

        private void LoadSprites()
        {
            redSprites = LoadSpritesheet("Sprites/Red/RED_SPRITESHEET", out redRows);
            blueSprites = LoadSpritesheet("Sprites/Blue/BLUE_SPRITESHEET", out blueRows);
            Debug.Log($"Loaded sprites: Red={redRows} rows, Blue={blueRows} rows");
        }

        private Sprite[][] LoadSpritesheet(string resourcePath, out int rowCount)
        {
            var tex = Resources.Load<Texture2D>(resourcePath);
            if (tex == null) { rowCount = 0; return null; }

            int cols = tex.width / 64;
            rowCount = tex.height / 64;

            var sprites = new Sprite[rowCount][];
            for (int row = 0; row < rowCount; row++)
            {
                sprites[row] = new Sprite[cols];
                for (int col = 0; col < cols; col++)
                {
                    var rect = new Rect(col * 64, (rowCount - 1 - row) * 64, 64, 64);
                    sprites[row][col] = Sprite.Create(tex, rect, new Vector2(0.5f, 0f), 64);
                }
            }
            return sprites;
        }

        private Sprite GetSprite(Sprite[][] sheet, int rows, int animState, int animIndex)
        {
            if (sheet == null || animState < 0 || animState >= rows) return null;
            var row = sheet[animState];
            if (row == null || animIndex < 0 || animIndex >= row.Length) return null;
            return row[animIndex];
        }

        // ──────────────────────────────────────────────
        //  BACKGROUND
        // ──────────────────────────────────────────────

        private void SetupBackground()
        {
            background = new GameObject("Background");
            bgSR = background.AddComponent<SpriteRenderer>();
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

        // ──────────────────────────────────────────────
        //  PLATFORMS
        // ──────────────────────────────────────────────

        private void SetupPlatforms()
        {
            CreateSolidPlatform("PlatformVisuals", new Vector3(9f, 0.25f, 3), new Vector3(12f, 0.5f, 1f), new Color(0.3f, 0.35f, 0.3f));
            CreateSolidPlatform("LeftPlatformVisual", new Vector3(3f, 3.25f, 3), new Vector3(3f, 0.5f, 1f), new Color(0.35f, 0.3f, 0.3f));
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

        // ──────────────────────────────────────────────
        //  UI  (created at runtime, direct references)
        // ──────────────────────────────────────────────

        private void SetupUI()
        {
            canvasObj = new GameObject("HUD_Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();

            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            // Preload white sprite for all UI images
            if (whiteSprite == null)
            {
                var tex = new Texture2D(4, 4);
                var px = new Color[16];
                for (int i = 0; i < 16; i++) px[i] = Color.white;
                tex.SetPixels(px);
                tex.Apply();
                whiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
            }

            // ── Nametags ──
            p1NameTag = CreateText(canvasObj.transform, "P1Name", "RED", 22, TextAnchor.MiddleCenter,
                new Vector2(0.02f, 0.97f), new Vector2(0.15f, 1f), Color.red);
            p2NameTag = CreateText(canvasObj.transform, "P2Name", "BLUE", 22, TextAnchor.MiddleCenter,
                new Vector2(0.85f, 0.97f), new Vector2(0.98f, 1f), new Color(0.3f, 0.6f, 1f));

            // ── Health bars ──
            p1HealthBar = CreateBar(canvasObj.transform, "P1HealthBar", Color.red,
                new Vector2(0.02f, 0.935f), new Vector2(0.38f, 0.965f));
            p2HealthBar = CreateBar(canvasObj.transform, "P2HealthBar", new Color(0, 0.5f, 1f),
                new Vector2(0.62f, 0.935f), new Vector2(0.98f, 0.965f));

            // ── Stamina bars (below health) ──
            p1StaminaBar = CreateBar(canvasObj.transform, "P1StaminaBar", new Color(0.2f, 0.8f, 0.2f),
                new Vector2(0.02f, 0.915f), new Vector2(0.22f, 0.932f));
            p2StaminaBar = CreateBar(canvasObj.transform, "P2StaminaBar", new Color(0.2f, 0.8f, 0.2f),
                new Vector2(0.78f, 0.915f), new Vector2(0.98f, 0.932f));

            // ── Win dots (3 per player for race to 3) ──
            for (int i = 0; i < 3; i++)
            {
                float xMin = 0.02f + i * 0.04f;
                p1WinDots[i] = CreateBar(canvasObj.transform, $"P1Dot{i}", new Color(0.3f, 0.3f, 0.3f),
                    new Vector2(xMin, 0.895f), new Vector2(xMin + 0.03f, 0.912f));

                float xMin2 = 0.88f + i * 0.04f;
                p2WinDots[i] = CreateBar(canvasObj.transform, $"P2Dot{i}", new Color(0.3f, 0.3f, 0.3f),
                    new Vector2(xMin2, 0.895f), new Vector2(xMin2 + 0.03f, 0.912f));
            }

            // ── Center: Timer + Round ──
            timerText = CreateText(canvasObj.transform, "TimerText", "60", 48, TextAnchor.MiddleCenter,
                new Vector2(0.45f, 0.94f), new Vector2(0.55f, 0.98f), Color.white);
            roundText = CreateText(canvasObj.transform, "RoundText", "ROUND 1", 20, TextAnchor.MiddleCenter,
                new Vector2(0.42f, 0.91f), new Vector2(0.58f, 0.935f), Color.white);

            // ── Cooldown indicators ──
            p1CooldownText = CreateText(canvasObj.transform, "P1Cooldown", "J:READY  K:READY  L:READY", 16, TextAnchor.MiddleLeft,
                new Vector2(0.02f, 0.87f), new Vector2(0.30f, 0.89f), new Color(0.8f, 0.8f, 0.8f));
            p2CooldownText = CreateText(canvasObj.transform, "P2Cooldown", "J:READY  K:READY  L:READY", 16, TextAnchor.MiddleRight,
                new Vector2(0.70f, 0.87f), new Vector2(0.98f, 0.89f), new Color(0.8f, 0.8f, 0.8f));

            // ── Laban text ──
            labanText = CreateText(canvasObj.transform, "LabanText", "LABAN!", 120, TextAnchor.MiddleCenter,
                new Vector2(0.15f, 0.35f), new Vector2(0.85f, 0.65f), Color.yellow);

            // ── Win overlay texts ──
            CreateText(canvasObj.transform, "P1WinsText", "", 28, TextAnchor.MiddleLeft,
                new Vector2(0.02f, 0.85f), new Vector2(0.15f, 0.88f), Color.red);
            CreateText(canvasObj.transform, "P2WinsText", "", 28, TextAnchor.MiddleRight,
                new Vector2(0.85f, 0.85f), new Vector2(0.98f, 0.88f), new Color(0, 0.5f, 1f));
        }

        private static Image CreateBar(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = obj.AddComponent<Image>();
            img.sprite = whiteSprite;
            img.color = color;
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillAmount = 1f;
            return img;
        }

        private static Text CreateText(Transform parent, string name, string text, int fontSize,
            TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var t = obj.AddComponent<Text>();
            t.text = text;
            t.fontSize = fontSize;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", fontSize);
            t.font = font;
            t.alignment = alignment;
            t.color = color;
            t.raycastTarget = false;
            return t;
        }

        // ──────────────────────────────────────────────
        //  LOOPS
        // ──────────────────────────────────────────────

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
                debugTogglePressed = false;

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

            if (Input.GetKeyDown(KeyCode.F2) && GameManager.Instance != null)
            {
                GameManager.Instance.ResetRound();
                Debug.Log("Round reset!");
            }

            if (Input.GetKeyDown(KeyCode.F3))
            {
                debugDummyMode = !debugDummyMode;
                if (debugDummyMode) debugAttackSpam = false;
                Debug.Log($"Blue dummy: {(debugDummyMode ? "ON" : "OFF")}");
            }

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
            float scaleX = orthoW / bgSR.sprite.bounds.size.x;
            float scaleY = orthoH / bgSR.sprite.bounds.size.y;
            float scale = Mathf.Max(scaleX, scaleY);

            background.transform.localScale = new Vector3(scale, scale, 1f);
            background.transform.position = new Vector3(mainCam.transform.position.x, mainCam.transform.position.y, 5f);
        }

        private void FixedUpdate()
        {
            if (!gameStarted) return;

            CollectLocalInput();
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
                    remoteInput = InputData.Create(currentFrame);
                else if (debugAttackSpam)
                {
                    remoteInput = InputData.Create(currentFrame);
                    remoteInput.buttons |= InputData.ATTACK;
                }
                else
                    remoteInput = InputData.Create(currentFrame);
            }

            tickAccumulator += Time.fixedDeltaTime;

            while (tickAccumulator >= tickInterval)
            {
                tickAccumulator -= tickInterval;

                if (NetworkManager.Instance != null &&
                    NetworkManager.Instance.State == NetworkManager.ConnectionState.Connected)
                    NetworkManager.Instance.GetRemoteInput(currentFrame, out remoteInput);

                GameManager.Instance?.Tick(localInput, remoteInput);
                currentFrame++;
            }
        }

        // ──────────────────────────────────────────────
        //  VISUALS
        // ──────────────────────────────────────────────

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
                if (p2 != null) player2Sprite = p2.GetComponent<SpriteRenderer>();
            }
            if (mainCam == null) mainCam = Camera.main;

            bool showP2 = debugDummyMode || debugAttackSpam ||
                (NetworkManager.Instance != null && NetworkManager.Instance.State == NetworkManager.ConnectionState.Connected);

            if (player2Sprite != null)
            {
                player2Sprite.enabled = showP2;
                if (showP2)
                {
                    player2Sprite.transform.position = new Vector3(
                        FixedMath.ToFloat(state.player2.x),
                        FixedMath.ToFloat(state.player2.y), 0);
                    player2Sprite.flipX = state.player2.facingLeft;
                    var sprite = GetSprite(blueSprites, blueRows, state.player2.animState, state.player2.animIndex);
                    if (sprite != null) player2Sprite.sprite = sprite;
                }
            }

            if (player1Sprite != null)
            {
                player1Sprite.transform.position = new Vector3(
                    FixedMath.ToFloat(state.player1.x),
                    FixedMath.ToFloat(state.player1.y), 0);
                player1Sprite.flipX = false;
                var sprite = GetSprite(redSprites, redRows, state.player1.animState, state.player1.animIndex);
                if (sprite != null) player1Sprite.sprite = sprite;
            }

            if (player1Sprite != null)
            {
                float playerY = player1Sprite.transform.position.y;
                float baseCamY = 1.5f;
                float camY = baseCamY + playerY * 0.3f;
                float baseOrtho = 3.5f;
                float orthoSize = baseOrtho + Mathf.Abs(playerY - baseCamY) * 0.5f;
                orthoSize = Mathf.Clamp(orthoSize, 3f, 8f);

                mainCam.transform.position = new Vector3(player1Sprite.transform.position.x, camY, -10f);
                mainCam.orthographicSize = orthoSize;
            }

            UpdateUI(state);
        }

        // ──────────────────────────────────────────────
        //  UI UPDATE (direct references, no Find)
        // ──────────────────────────────────────────────

        private void UpdateUI(GameState state)
        {
            // ── Laban splash ──
            if (labanText != null)
                labanText.gameObject.SetActive(state.showLaban);

            // ── Timer ──
            if (timerText != null)
                timerText.text = state.timer.ToString("D2");

            // ── Round ──
            if (roundText != null)
                roundText.text = $"ROUND {state.round}";

            // ── Health bars ──
            if (p1HealthBar != null) p1HealthBar.fillAmount = (float)state.player1.health / PlayerController.MAX_HEALTH;
            if (p2HealthBar != null) p2HealthBar.fillAmount = (float)state.player2.health / PlayerController.MAX_HEALTH;

            // ── Stamina bars ──
            if (p1StaminaBar != null)
            {
                p1StaminaBar.fillAmount = (float)state.player1.stamina / PlayerController.MAX_STAMINA;
                p1StaminaBar.color = state.player1.slowTimer > 0 ? new Color(0.5f, 0.3f, 1f) : new Color(0.2f, 0.8f, 0.2f);
            }
            if (p2StaminaBar != null)
            {
                p2StaminaBar.fillAmount = (float)state.player2.stamina / PlayerController.MAX_STAMINA;
                p2StaminaBar.color = state.player2.slowTimer > 0 ? new Color(0.5f, 0.3f, 1f) : new Color(0.2f, 0.8f, 0.2f);
            }

            // ── Win dots ──
            for (int i = 0; i < 3; i++)
            {
                if (p1WinDots[i] != null)
                    p1WinDots[i].color = i < state.player1Wins ? Color.red : new Color(0.3f, 0.3f, 0.3f);
                if (p2WinDots[i] != null)
                    p2WinDots[i].color = i < state.player2Wins ? new Color(0.3f, 0.6f, 1f) : new Color(0.3f, 0.3f, 0.3f);
            }

            // ── Cooldown indicators ──
            if (p1CooldownText != null)
                p1CooldownText.text = $"J:{CdStr(state.player1.attackCooldownLeft)}  K:{CdStr(state.player1.sungkitCooldownLeft)}  L:{CdStr(state.player1.launchCooldownLeft)}";
            if (p2CooldownText != null)
                p2CooldownText.text = $"J:{CdStr(state.player2.attackCooldownLeft)}  K:{CdStr(state.player2.sungkitCooldownLeft)}  L:{CdStr(state.player2.launchCooldownLeft)}";
        }

        private static string CdStr(int frames)
        {
            if (frames <= 0) return "READY";
            return $"{frames / 60f:F1}s";
        }

        // ──────────────────────────────────────────────
        //  INPUT
        // ──────────────────────────────────────────────

        private void CollectLocalInput()
        {
            localInput = InputData.Create(currentFrame);
            if (Input.GetKey(KeyCode.A)) localInput.buttons |= InputData.LEFT;
            if (Input.GetKey(KeyCode.D)) localInput.buttons |= InputData.RIGHT;
            if (Input.GetKey(KeyCode.W)) localInput.buttons |= InputData.UP;
            if (Input.GetKey(KeyCode.S)) localInput.buttons |= InputData.DOWN;
            if (Input.GetKey(KeyCode.Space)) localInput.buttons |= InputData.BLOCK;
        }

        // ──────────────────────────────────────────────
        //  ROLLBACK
        // ──────────────────────────────────────────────

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

        // ──────────────────────────────────────────────
        //  DEBUG OVERLAY
        // ──────────────────────────────────────────────

        private void OnGUI()
        {
            if (!showDebugInfo) return;

            int y = 10;
            int lh = 20;

            GUI.Label(new Rect(10, y, 500, lh), $"Frame: {currentFrame}  Focused: {Application.isFocused}");
            y += lh;

            if (GameManager.Instance != null)
            {
                GameState state = GameManager.Instance.currentState;
                var p1 = state.player1;
                GUI.Label(new Rect(10, y, 900, lh),
                    $"P1: hp={p1.health} stm={p1.stamina} slow={p1.slowTimer} blkTmr={p1.blockTimer} gnd={p1.isOnGround} anim={p1.animState}[{p1.animIndex}]");
                y += lh;
                GUI.Label(new Rect(10, y, 500, lh), $"Round: {state.round}  Timer: {state.timer}  Wins: P1={state.player1Wins} P2={state.player2Wins}  First to {GameManager.Instance.winsNeeded}");
                y += lh;

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
            DrawRect(PlayerController.GetPlayerHitbox(player), color, 0.3f);
        }

        private void DrawAttackHitbox(PlayerState player, Color color)
        {
            if (!player.attacking && !player.sungkit && !player.launch) return;
            Rect hitbox = PlayerController.GetAttackHitbox(player);
            if (hitbox.width > 0 && hitbox.height > 0) DrawRect(hitbox, color, 0.5f);
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
