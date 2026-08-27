using UnityEngine;
using UnityEngine.UI;
using System.Net;
using System.Net.Sockets;

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
        private InputData bufferedInput2; // couch multiplayer: player 2's buffered attack presses
        private bool isLocalGame;

        private SpriteRenderer player1Sprite;
        private SpriteRenderer player2Sprite;
        private Vector3 prevP1VisualPos;
        private Vector3 prevP2VisualPos;
        private Camera mainCam;
        private GameObject background;
        private SpriteRenderer bgSR;

        private Sprite[][] redSprites;
        private Sprite[][] blueSprites;
        private int redRows;
        private int blueRows;

        private static Sprite whiteSprite;
        private static Font cachedFont;

        private GameObject canvasObj;
        private Text timerText;
        private Text roundText;
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

        private Image redWinOverlay;
        private Image blueWinOverlay;
        private Image drawOverlay;
        private Image pauseOverlay;
        private Image p1HealthFrame;
        private Image p2HealthFrame;

        private Image gameOverRedWinsOverlay;
        private Image gameOverBlueWinsOverlay;
        private Image gameOverDrawOverlay;
        private bool prevIsGameOver;
        private GameObject gameOverCanvas;

        private int prevTimer = 60;

        // Connection UI
        private bool showConnectionUI = true;
        private bool showDisconnectedMsg;
        private float disconnectedTimer;
        private string ipInput = "127.0.0.1";
        private GameObject connectionUIRoot;
        private GameObject hudRoot;
        private Text hostIPDisplay;
        private Text statusLabel;

        // Connection pages
        private GameObject mainMenuPage;
        private GameObject hostPage;
        private GameObject joinPage;
        private InputField joinIPField;

        // Loading screen
        private GameObject loadingCanvas;
        private Image loadingBarFill;
        private Text loadingStatusText;
        private Text loadingPercentText;

        private void Start()
        {
            tickInterval = 1f / targetTickRate;
            mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.clearFlags = CameraClearFlags.SolidColor;
                mainCam.backgroundColor = Color.black;
            }

            ShowLoadingScreen();
            StartCoroutine(LoadGameRoutine());
        }

        private System.Collections.IEnumerator LoadGameRoutine()
        {
            // Step 1: Init white sprite
            loadingStatusText.text = "Initializing...";
            loadingBarFill.fillAmount = 0.05f;
            loadingPercentText.text = "5%";
            yield return null;

            if (whiteSprite == null)
            {
                var tex = new Texture2D(4, 4);
                var px = new Color[16];
                for (int i = 0; i < 16; i++) px[i] = Color.white;
                tex.SetPixels(px);
                tex.Apply();
                whiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
            }

            // Step 2: Load sprites
            loadingStatusText.text = "Loading sprites...";
            loadingBarFill.fillAmount = 0.2f;
            loadingPercentText.text = "20%";
            yield return null;

            LoadSprites();

            // Step 3: Setup background
            loadingStatusText.text = "Loading background...";
            loadingBarFill.fillAmount = 0.35f;
            loadingPercentText.text = "35%";
            yield return null;

            SetupBackground();

            // Step 4: Setup platforms
            loadingStatusText.text = "Building stage...";
            loadingBarFill.fillAmount = 0.5f;
            loadingPercentText.text = "50%";
            yield return null;

            SetupPlatforms();

            // Step 5: Setup HUD
            loadingStatusText.text = "Creating HUD...";
            loadingBarFill.fillAmount = 0.65f;
            loadingPercentText.text = "65%";
            yield return null;

            SetupUI();

            // Step 6: Setup connection UI
            loadingStatusText.text = "Setting up menus...";
            loadingBarFill.fillAmount = 0.8f;
            loadingPercentText.text = "80%";
            yield return null;

            SetupConnectionUI();

            // Step 7: Done
            loadingStatusText.text = "Ready!";
            loadingBarFill.fillAmount = 1f;
            loadingPercentText.text = "100%";
            yield return new WaitForSeconds(0.3f);

            HideLoadingScreen();
            Debug.Log("GameLoop.Start() complete");
        }

        private void ShowLoadingScreen()
        {
            var font = GetFont();

            loadingCanvas = new GameObject("LoadingCanvas");
            var canvas = loadingCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            var scaler = loadingCanvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            loadingCanvas.AddComponent<GraphicRaycaster>();

            // Black background
            var bg = new GameObject("BG");
            bg.transform.SetParent(loadingCanvas.transform, false);
            var bgRt = bg.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.05f, 0.05f, 0.08f);

            // Title
            CreateText(loadingCanvas.transform, "Title", "LABANAN", 48, TextAnchor.MiddleCenter,
                new Vector2(0.2f, 0.65f), new Vector2(0.8f, 0.75f), new Color(1f, 0.3f, 0.3f), font);

            // Loading bar background
            var barBg = new GameObject("BarBG");
            barBg.transform.SetParent(loadingCanvas.transform, false);
            var barBgRt = barBg.AddComponent<RectTransform>();
            barBgRt.anchorMin = new Vector2(0.25f, 0.40f);
            barBgRt.anchorMax = new Vector2(0.75f, 0.43f);
            barBgRt.offsetMin = Vector2.zero;
            barBgRt.offsetMax = Vector2.zero;
            var barBgImg = barBg.AddComponent<Image>();
            barBgImg.color = new Color(0.2f, 0.2f, 0.2f);

            // Loading bar fill
            var barFill = new GameObject("BarFill");
            barFill.transform.SetParent(barBg.transform, false);
            var barFillRt = barFill.AddComponent<RectTransform>();
            barFillRt.anchorMin = Vector2.zero;
            barFillRt.anchorMax = Vector2.one;
            barFillRt.offsetMin = Vector2.zero;
            barFillRt.offsetMax = Vector2.zero;
            loadingBarFill = barFill.AddComponent<Image>();
            loadingBarFill.color = new Color(0.9f, 0.2f, 0.2f);
            loadingBarFill.fillAmount = 0f;

            // Status text
            loadingStatusText = CreateText(loadingCanvas.transform, "Status", "Loading...", 22, TextAnchor.MiddleCenter,
                new Vector2(0.2f, 0.32f), new Vector2(0.8f, 0.37f), new Color(0.7f, 0.7f, 0.7f), font);

            // Percent text
            loadingPercentText = CreateText(loadingCanvas.transform, "Percent", "0%", 18, TextAnchor.MiddleCenter,
                new Vector2(0.2f, 0.44f), new Vector2(0.8f, 0.48f), new Color(0.5f, 0.5f, 0.5f), font);
        }

        private void HideLoadingScreen()
        {
            if (loadingCanvas != null)
            {
                Destroy(loadingCanvas);
                loadingCanvas = null;
            }
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

        private void SetupBackground()
        {
            background = new GameObject("Background");
            bgSR = background.AddComponent<SpriteRenderer>();
            bgSR.sortingOrder = -1;

            var tex = Resources.Load<Texture2D>("Sprites/BG NIGHT");
            if (tex != null)
            {
                bgSR.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100);
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
        }

        private void SetupPlatforms()
        {
            var platTex = Resources.Load<Texture2D>("Sprites/PLATFORM");

            CreatePlatformVisual("PlatformVisuals", new Vector3(8.9f, 1.1f, 5), 20f, 3f, platTex, new Color(0.3f, 0.35f, 0.3f));
            CreatePlatformVisual("LeftPlatformVisual", new Vector3(3f, 3.9f, 5), 8.6f, 2f, platTex, new Color(0.35f, 0.3f, 0.3f));
            CreatePlatformVisual("RightPlatformVisual", new Vector3(15f, 3.9f, 5), 8.6f, 2f, platTex, new Color(0.3f, 0.3f, 0.35f));

            HidePlatformObject("MainPlatform");
            HidePlatformObject("LeftPlatform");
            HidePlatformObject("RightPlatform");
        }

        private void CreatePlatformVisual(string name, Vector3 pos, float width, float height, Texture2D tex, Color color)
        {
            var obj = new GameObject(name);
            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 1;
            obj.transform.position = pos;

            if (tex != null)
            {
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 1);
                obj.transform.localScale = new Vector3(width / tex.width, height / tex.height, 1f);
            }
            else
            {
                sr.sprite = whiteSprite;
                obj.transform.localScale = new Vector3(width, height, 1f);
            }

            sr.color = color;
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

        private void SetupUI()
        {
            canvasObj = new GameObject("HUD_Canvas");
            hudRoot = canvasObj;
            hudRoot.SetActive(false);
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();

            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            if (whiteSprite == null)
            {
                var tex = new Texture2D(4, 4);
                var px = new Color[16];
                for (int i = 0; i < 16; i++) px[i] = Color.white;
                tex.SetPixels(px);
                tex.Apply();
                whiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
            }

            Font font = GetFont();

            // ── Nametags ──
            p1NameTag = CreateText(canvasObj.transform, "P1Name", "RED", 22, TextAnchor.MiddleCenter,
                new Vector2(0.02f, 0.97f), new Vector2(0.15f, 1f), Color.red, font);
            p2NameTag = CreateText(canvasObj.transform, "P2Name", "BLUE", 22, TextAnchor.MiddleCenter,
                new Vector2(0.85f, 0.97f), new Vector2(0.98f, 1f), new Color(0.3f, 0.6f, 1f), font);

            // ── Health bar backgrounds (HEALTH BAR.png) ──
            var hbTex = Resources.Load<Texture2D>("Sprites/HEALTH BAR");
            Sprite hbSprite = null;
            if (hbTex != null)
                hbSprite = Sprite.Create(hbTex, new Rect(0, 0, hbTex.width, hbTex.height), new Vector2(0.5f, 0.5f), hbTex.width / 8);

            if (hbSprite != null)
            {
                p1HealthFrame = CreateImage(canvasObj.transform, "P1HealthFrame", hbSprite,
                    new Vector2(0.0f, 0.94f), new Vector2(0.40f, 0.99f));
                p2HealthFrame = CreateImage(canvasObj.transform, "P2HealthFrame", hbSprite,
                    new Vector2(0.60f, 0.94f), new Vector2(1.0f, 0.99f));
                if (p1HealthFrame != null) { p1HealthFrame.type = Image.Type.Simple; p1HealthFrame.preserveAspect = true; }
                if (p2HealthFrame != null) { p2HealthFrame.type = Image.Type.Simple; p2HealthFrame.preserveAspect = true; }
            }

            // ── Health bars (fill) ──
            p1HealthBar = CreateBar(canvasObj.transform, "P1HealthBar", Color.red,
                new Vector2(0.01f, 0.945f), new Vector2(0.39f, 0.985f));
            p2HealthBar = CreateBar(canvasObj.transform, "P2HealthBar", new Color(0, 0.5f, 1f),
                new Vector2(0.61f, 0.945f), new Vector2(0.99f, 0.985f));

            // ── Stamina bars ──
            p1StaminaBar = CreateBar(canvasObj.transform, "P1StaminaBar", new Color(0.2f, 0.8f, 0.2f),
                new Vector2(0.01f, 0.925f), new Vector2(0.20f, 0.942f));
            p2StaminaBar = CreateBar(canvasObj.transform, "P2StaminaBar", new Color(0.2f, 0.8f, 0.2f),
                new Vector2(0.80f, 0.925f), new Vector2(0.99f, 0.942f));

            // ── Win dots (3 per player for race to 3) ──
            for (int i = 0; i < 3; i++)
            {
                float xMin = 0.01f + i * 0.035f;
                p1WinDots[i] = CreateBar(canvasObj.transform, $"P1Dot{i}", new Color(0.3f, 0.3f, 0.3f),
                    new Vector2(xMin, 0.908f), new Vector2(xMin + 0.028f, 0.922f));

                float xMin2 = 0.89f + i * 0.035f;
                p2WinDots[i] = CreateBar(canvasObj.transform, $"P2Dot{i}", new Color(0.3f, 0.3f, 0.3f),
                    new Vector2(xMin2, 0.908f), new Vector2(xMin2 + 0.028f, 0.922f));
            }

            // ── Center: Timer + Round ──
            timerText = CreateText(canvasObj.transform, "TimerText", "60", 48, TextAnchor.MiddleCenter,
                new Vector2(0.43f, 0.945f), new Vector2(0.57f, 0.995f), Color.white, font);
            roundText = CreateText(canvasObj.transform, "RoundText", "ROUND 1", 20, TextAnchor.MiddleCenter,
                new Vector2(0.43f, 0.925f), new Vector2(0.57f, 0.945f), Color.white, font);

            // ── Cooldown indicators ──
            p1CooldownText = CreateText(canvasObj.transform, "P1Cooldown", "J:READY  K:READY  L:READY", 16, TextAnchor.MiddleLeft,
                new Vector2(0.02f, 0.87f), new Vector2(0.30f, 0.89f), new Color(0.8f, 0.8f, 0.8f), font);
            p2CooldownText = CreateText(canvasObj.transform, "P2Cooldown", "J:READY  K:READY  L:READY", 16, TextAnchor.MiddleRight,
                new Vector2(0.70f, 0.87f), new Vector2(0.98f, 0.89f), new Color(0.8f, 0.8f, 0.8f), font);

            // ── Win overlays (hidden) ──
            redWinOverlay = CreateOverlaySprite("RedWin", "Sprites/RED_WIN");
            blueWinOverlay = CreateOverlaySprite("BlueWin", "Sprites/BLUE_WIN");
            drawOverlay = CreateOverlaySprite("Draw", "Sprites/DRAW");

            // ── Pause overlay (hidden) ──
            pauseOverlay = CreateOverlaySprite("PauseOverlay", "Sprites/PAUSE");

            // ── Game over: separate canvas on top ──
            gameOverCanvas = new GameObject("GameOverCanvas");
            var goCanvas = gameOverCanvas.AddComponent<Canvas>();
            goCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            goCanvas.sortingOrder = 100;
            var goScaler = gameOverCanvas.AddComponent<CanvasScaler>();
            goScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            goScaler.referenceResolution = new Vector2(1920, 1080);
            gameOverCanvas.AddComponent<GraphicRaycaster>();

            // ── Game over overlays (hidden) ──
            gameOverRedWinsOverlay = CreateOverlaySpriteOn(gameOverCanvas.transform, "GameOverRedWins", "Sprites/GameOver, again, exit (RED WINS)");
            gameOverBlueWinsOverlay = CreateOverlaySpriteOn(gameOverCanvas.transform, "GameOverBlueWins", "Sprites/GameOver, again, exit (BLUE WINS)");
            gameOverDrawOverlay = CreateOverlaySpriteOn(gameOverCanvas.transform, "GameOverDraw", "Sprites/GameOver, again, exit (DRAW)");

            // ── Game Over buttons (visible, on the GO canvas) ──
            CreateGameOverButton("AgainBtn", new Vector2(0.35f, 0.22f), new Vector2(0.65f, 0.30f), "PLAY AGAIN", font, OnAgainClicked);
            CreateGameOverButton("ExitBtn", new Vector2(0.35f, 0.10f), new Vector2(0.65f, 0.18f), "EXIT", font, OnExitClicked);

            gameOverCanvas.SetActive(false);

            // ── Win overlay texts (hidden) ──
            CreateText(canvasObj.transform, "P1WinsText", "", 28, TextAnchor.MiddleLeft,
                new Vector2(0.02f, 0.85f), new Vector2(0.15f, 0.88f), Color.red, font);
            CreateText(canvasObj.transform, "P2WinsText", "", 28, TextAnchor.MiddleRight,
                new Vector2(0.85f, 0.85f), new Vector2(0.98f, 0.88f), new Color(0, 0.5f, 1f), font);
        }

        private static Font GetFont()
        {
            if (cachedFont != null) return cachedFont;

            cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (cachedFont != null) return cachedFont;

            cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (cachedFont != null) return cachedFont;

            cachedFont = Font.CreateDynamicFontFromOSFont("Arial", 32);
            if (cachedFont != null) return cachedFont;

            cachedFont = Font.CreateDynamicFontFromOSFont("Segoe UI", 32);
            return cachedFont;
        }

        private Image CreateOverlaySprite(string name, string resourcePath)
        {
            return CreateOverlaySpriteOn(canvasObj.transform, name, resourcePath);
        }

        private Image CreateOverlaySpriteOn(Transform parent, string name, string resourcePath)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = obj.AddComponent<Image>();
            img.raycastTarget = false;

            var tex = Resources.Load<Texture2D>(resourcePath);
            if (tex != null)
            {
                img.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f), tex.width / 19);
                img.preserveAspect = false;
            }
            else
            {
                img.sprite = whiteSprite;
                img.color = new Color(0, 0, 0, 0.7f);
            }
            obj.SetActive(false);
            return img;
        }

        private Image CreateImage(Transform parent, string name, Sprite sprite, Vector2 anchorMin, Vector2 anchorMax)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = obj.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            return img;
        }

        private void CreateGameOverButton(string name, Vector2 anchorMin, Vector2 anchorMax, string label, Font font, UnityEngine.Events.UnityAction onClick)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(gameOverCanvas.transform, false);
            var rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = obj.AddComponent<Image>();
            img.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
            var btn = obj.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            var colors = btn.colors;
            colors.normalColor = new Color(0.15f, 0.15f, 0.15f, 0.9f);
            colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f, 0.95f);
            colors.pressedColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            btn.colors = colors;

            CreateText(obj.transform, "Label", label, 24, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Color.white, font);

            obj.SetActive(false);
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
            img.raycastTarget = false;
            return img;
        }

        private static Text CreateText(Transform parent, string name, string text, int fontSize,
            TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax, Color color, Font font = null)
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
            t.font = font != null ? font : GetFont();
            t.alignment = alignment;
            t.color = color;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        // ──────────────────────────────────────────────
        //  CONNECTION UI
        // ──────────────────────────────────────────────

        private void SetupConnectionUI()
        {
            connectionUIRoot = new GameObject("ConnectionUI");
            var uiCanvas = connectionUIRoot.AddComponent<Canvas>();
            uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            uiCanvas.sortingOrder = 100;
            var canvasScaler = connectionUIRoot.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            connectionUIRoot.AddComponent<GraphicRaycaster>();

            var font = GetFont();

            // ── Main Menu Page ──
            mainMenuPage = CreatePage(connectionUIRoot.transform, "MainMenuPage");
            AddMenuBackground(mainMenuPage.transform);

            CreateText(mainMenuPage.transform, "Subtitle", "ONLINE FIGHTING", 24, TextAnchor.MiddleCenter,
                new Vector2(0.2f, 0.55f), new Vector2(0.8f, 0.65f), new Color(0.6f, 0.6f, 0.6f), font);

            CreateButton(mainMenuPage.transform, "HostBtn", "HOST", 28,
                new Vector2(0.30f, 0.38f), new Vector2(0.70f, 0.45f), font, () => ShowPage(hostPage));

            CreateButton(mainMenuPage.transform, "JoinBtn", "JOIN", 28,
                new Vector2(0.30f, 0.28f), new Vector2(0.70f, 0.35f), font, () => ShowPage(joinPage));

            CreateButton(mainMenuPage.transform, "LocalBtn", "LOCAL", 28,
                new Vector2(0.30f, 0.18f), new Vector2(0.70f, 0.25f), font, () =>
                {
                    isLocalGame = true;
                    OnConnectedToGame();
                });

            // ── Host Page ──
            hostPage = CreatePage(connectionUIRoot.transform, "HostPage");
            AddMenuBackground(hostPage.transform);

            CreateText(hostPage.transform, "HostTitle", "HOST GAME", 36, TextAnchor.MiddleCenter,
                new Vector2(0.15f, 0.70f), new Vector2(0.85f, 0.78f), new Color(1f, 0.4f, 0.4f), font);

            CreateText(hostPage.transform, "HostIPLabel", "Your IP (share with opponent):", 20, TextAnchor.MiddleCenter,
                new Vector2(0.15f, 0.58f), new Vector2(0.85f, 0.63f), new Color(0.7f, 0.7f, 0.7f), font);

            hostIPDisplay = CreateText(hostPage.transform, "HostIPValue", GetLocalIPAddress(), 32, TextAnchor.MiddleCenter,
                new Vector2(0.15f, 0.50f), new Vector2(0.85f, 0.57f), new Color(0.4f, 1f, 0.4f), font);

            statusLabel = CreateText(hostPage.transform, "HostStatus", "Press START to begin hosting", 20, TextAnchor.MiddleCenter,
                new Vector2(0.15f, 0.42f), new Vector2(0.85f, 0.47f), Color.yellow, font);

            CreateButton(hostPage.transform, "HostStartBtn", "START", 28,
                new Vector2(0.30f, 0.30f), new Vector2(0.70f, 0.37f), font, () =>
                {
                    if (NetworkManager.Instance != null)
                    {
                        NetworkManager.Instance.Host();
                        if (statusLabel != null) statusLabel.text = "Waiting for opponent...";
                    }
                });

            CreateButton(hostPage.transform, "HostBackBtn", "BACK", 24,
                new Vector2(0.35f, 0.18f), new Vector2(0.65f, 0.25f), font, () =>
                {
                    if (NetworkManager.Instance != null) NetworkManager.Instance.Disconnect();
                    ShowPage(mainMenuPage);
                });

            // ── Join Page ──
            joinPage = CreatePage(connectionUIRoot.transform, "JoinPage");
            AddMenuBackground(joinPage.transform);

            CreateText(joinPage.transform, "JoinTitle", "JOIN GAME", 36, TextAnchor.MiddleCenter,
                new Vector2(0.15f, 0.70f), new Vector2(0.85f, 0.78f), new Color(0.4f, 0.6f, 1f), font);

            CreateText(joinPage.transform, "JoinIPLabel", "Enter host IP:", 22, TextAnchor.MiddleCenter,
                new Vector2(0.15f, 0.58f), new Vector2(0.85f, 0.63f), Color.white, font);

            joinIPField = CreateInputField(joinPage.transform, "JoinIPField", ipInput,
                new Vector2(0.25f, 0.49f), new Vector2(0.75f, 0.55f), font);

            CreateButton(joinPage.transform, "JoinConnectBtn", "CONNECT", 28,
                new Vector2(0.30f, 0.36f), new Vector2(0.70f, 0.43f), font, () =>
                {
                    if (NetworkManager.Instance != null && !string.IsNullOrEmpty(ipInput))
                    {
                        NetworkManager.Instance.Join(ipInput);
                        var st = joinPage.transform.Find("JoinStatus")?.GetComponent<Text>();
                        if (st != null) st.text = "Connecting...";
                    }
                });

            CreateText(joinPage.transform, "JoinStatus", "", 20, TextAnchor.MiddleCenter,
                new Vector2(0.15f, 0.28f), new Vector2(0.85f, 0.33f), Color.yellow, font);

            CreateButton(joinPage.transform, "JoinBackBtn", "BACK", 24,
                new Vector2(0.35f, 0.18f), new Vector2(0.65f, 0.25f), font, () => ShowPage(mainMenuPage));

            // ── Start on main menu ──
            ShowPage(mainMenuPage);

            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnConnected += OnConnectedToGame;
                NetworkManager.Instance.OnDisconnected += OnDisconnectedFromGame;
            }
        }

        private GameObject CreatePage(Transform parent, string name)
        {
            var page = new GameObject(name);
            page.transform.SetParent(parent, false);
            var rt = page.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return page;
        }

        private void AddMenuBackground(Transform parent)
        {
            var menuTex = Resources.Load<Texture2D>("Sprites/menu");
            if (menuTex != null)
            {
                var bgObj = new GameObject("Background");
                bgObj.transform.SetParent(parent, false);
                var bgRt = bgObj.AddComponent<RectTransform>();
                bgRt.anchorMin = Vector2.zero;
                bgRt.anchorMax = Vector2.one;
                bgRt.offsetMin = Vector2.zero;
                bgRt.offsetMax = Vector2.zero;
                var bgImg = bgObj.AddComponent<Image>();
                bgImg.sprite = Sprite.Create(menuTex, new Rect(0, 0, menuTex.width, menuTex.height), new Vector2(0.5f, 0.5f));
                bgImg.preserveAspect = false;
            }

            var overlayObj = new GameObject("DarkOverlay");
            overlayObj.transform.SetParent(parent, false);
            var overlayRt = overlayObj.AddComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.offsetMin = Vector2.zero;
            overlayRt.offsetMax = Vector2.zero;
            var overlayImg = overlayObj.AddComponent<Image>();
            overlayImg.color = new Color(0, 0, 0, 0.6f);
            overlayImg.raycastTarget = false;
        }

        private InputField CreateInputField(Transform parent, string name, string defaultValue,
            Vector2 anchorMin, Vector2 anchorMax, Font font)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var bg = obj.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.15f);
            var field = obj.AddComponent<InputField>();

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(obj.transform, false);
            var textRt = textObj.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(8, 0);
            textRt.offsetMax = new Vector2(-8, 0);
            var text = textObj.AddComponent<Text>();
            text.font = font;
            text.fontSize = 22;
            text.color = Color.white;
            text.supportRichText = false;
            text.alignment = TextAnchor.MiddleLeft;

            var phObj = new GameObject("Placeholder");
            phObj.transform.SetParent(obj.transform, false);
            var phRt = phObj.AddComponent<RectTransform>();
            phRt.anchorMin = Vector2.zero;
            phRt.anchorMax = Vector2.one;
            phRt.offsetMin = new Vector2(8, 0);
            phRt.offsetMax = new Vector2(-8, 0);
            var ph = phObj.AddComponent<Text>();
            ph.font = font;
            ph.fontSize = 22;
            ph.fontStyle = FontStyle.Italic;
            ph.color = new Color(0.5f, 0.5f, 0.5f);
            ph.alignment = TextAnchor.MiddleLeft;
            ph.text = "Enter IP...";

            field.textComponent = text;
            field.placeholder = ph;
            field.text = defaultValue;
            field.onValueChanged.AddListener((val) => ipInput = val);
            return field;
        }

        private void ShowPage(GameObject page)
        {
            mainMenuPage.SetActive(page == mainMenuPage);
            hostPage.SetActive(page == hostPage);
            joinPage.SetActive(page == joinPage);

            if (page == hostPage)
            {
                string localIP = GetLocalIPAddress();
                if (hostIPDisplay != null) hostIPDisplay.text = localIP;
                if (statusLabel != null) statusLabel.text = "Press START to begin hosting";
            }

            if (page == joinPage)
            {
                var st = joinPage.transform.Find("JoinStatus")?.GetComponent<Text>();
                if (st != null) st.text = "";
                if (joinIPField != null) joinIPField.text = ipInput;
            }

            if (page == mainMenuPage)
            {
                if (AudioManager.Instance != null) AudioManager.Instance.StopMusic();
            }
        }

        private void CreateButton(Transform parent, string name, string label, int fontSize,
            Vector2 anchorMin, Vector2 anchorMax, Font font, UnityEngine.Events.UnityAction onClick)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = obj.AddComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.2f);
            var btn = obj.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            var colors = btn.colors;
            colors.normalColor = new Color(0.2f, 0.2f, 0.2f);
            colors.highlightedColor = new Color(0.35f, 0.35f, 0.35f);
            colors.pressedColor = new Color(0.15f, 0.15f, 0.15f);
            btn.colors = colors;

            CreateText(obj.transform, "Label", label, fontSize, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Color.white, font);
        }

        private void OnConnectedToGame()
        {
            showConnectionUI = false;
            if (connectionUIRoot != null) connectionUIRoot.SetActive(false);
            if (hudRoot != null) hudRoot.SetActive(true);
            gameStarted = true;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartGame();
            }
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayBGM();
            Debug.Log("Game started from connection!");
        }

        private bool IsLocalHost()
        {
            return isLocalGame || (NetworkManager.Instance != null && NetworkManager.Instance.IsHost);
        }

        private static string GetLocalIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                        return ip.ToString();
                }
            }
            catch { }
            return "127.0.0.1";
        }

        private void OnDisconnectedFromGame()
        {
            showDisconnectedMsg = true;
            disconnectedTimer = 3f;
            showConnectionUI = true;
            gameStarted = false;
            isLocalGame = false;
            if (connectionUIRoot != null) connectionUIRoot.SetActive(true);
            if (hudRoot != null) hudRoot.SetActive(false);
            if (AudioManager.Instance != null) AudioManager.Instance.StopMusic();
            ShowPage(mainMenuPage);
        }

        private void OnAgainClicked()
        {
            if (gameOverCanvas != null) gameOverCanvas.SetActive(false);
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartGame();
            }
            prevIsGameOver = false;
            prevP1VisualPos = new Vector3(FixedMath.ToFloat(GameManager.P1_SPAWN_X), FixedMath.ToFloat(GameManager.P1_SPAWN_Y), 0);
            prevP2VisualPos = new Vector3(FixedMath.ToFloat(GameManager.P2_SPAWN_X), FixedMath.ToFloat(GameManager.P2_SPAWN_Y), 0);
            if (player1Sprite != null) player1Sprite.transform.position = prevP1VisualPos;
            if (player2Sprite != null) player2Sprite.transform.position = prevP2VisualPos;
            currentFrame = 0;
            Debug.Log("Match restarted!");
        }

        private void OnExitClicked()
        {
            if (gameOverCanvas != null) gameOverCanvas.SetActive(false);
            gameStarted = false;
            isLocalGame = false;
            prevIsGameOver = false;
            showConnectionUI = true;
            if (connectionUIRoot != null) connectionUIRoot.SetActive(true);
            if (hudRoot != null) hudRoot.SetActive(false);
            if (AudioManager.Instance != null)
                AudioManager.Instance.StopMusic();
            if (NetworkManager.Instance != null &&
                NetworkManager.Instance.State == NetworkManager.ConnectionState.Connected)
            {
                NetworkManager.Instance.Disconnect();
            }
            ShowPage(mainMenuPage);
            Debug.Log("Returned to connection screen.");
        }

        // ──────────────────────────────────────────────
        //  LOOPS
        // ──────────────────────────────────────────────

        private void Update()
        {
            UnityMainThread.Update();

            if (showConnectionUI)
            {
                if (showDisconnectedMsg)
                {
                    disconnectedTimer -= Time.deltaTime;
                    if (disconnectedTimer <= 0)
                        showDisconnectedMsg = false;
                }

                if (NetworkManager.Instance != null &&
                    NetworkManager.Instance.State == NetworkManager.ConnectionState.Connecting)
                {
                    if (statusLabel != null)
                        statusLabel.text = "Waiting for opponent...";
                }

                return;
            }

            if (!gameStarted) return;

            if (Input.GetKeyDown(KeyCode.J))
                bufferedInput.buttons |= InputData.ATTACK;
            if (Input.GetKeyDown(KeyCode.K))
                bufferedInput.buttons |= InputData.SUNGKIT;
            if (Input.GetKeyDown(KeyCode.L))
                bufferedInput.buttons |= InputData.LAUNCH;

            // Controller 1 attack buttons (A/B/X on a standard gamepad), optional alongside J/K/L.
            if (Input.GetKeyDown(KeyCode.Joystick1Button0))
                bufferedInput.buttons |= InputData.ATTACK;
            if (Input.GetKeyDown(KeyCode.Joystick1Button1))
                bufferedInput.buttons |= InputData.SUNGKIT;
            if (Input.GetKeyDown(KeyCode.Joystick1Button2))
                bufferedInput.buttons |= InputData.LAUNCH;

            // Couch multiplayer: player 2 shares the keyboard (arrows + numpad) when not
            // networked, so their attack presses need the same edge-detected buffering as P1's.
            if (Input.GetKeyDown(KeyCode.Keypad1))
                bufferedInput2.buttons |= InputData.ATTACK;
            if (Input.GetKeyDown(KeyCode.Keypad2))
                bufferedInput2.buttons |= InputData.SUNGKIT;
            if (Input.GetKeyDown(KeyCode.Keypad3))
                bufferedInput2.buttons |= InputData.LAUNCH;

            // Controller 2 attack buttons, independent of controller 1 (joystick 2 vs joystick 1).
            if (Input.GetKeyDown(KeyCode.Joystick2Button0))
                bufferedInput2.buttons |= InputData.ATTACK;
            if (Input.GetKeyDown(KeyCode.Joystick2Button1))
                bufferedInput2.buttons |= InputData.SUNGKIT;
            if (Input.GetKeyDown(KeyCode.Joystick2Button2))
                bufferedInput2.buttons |= InputData.LAUNCH;

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
                prevP1VisualPos = new Vector3(FixedMath.ToFloat(GameManager.P1_SPAWN_X), FixedMath.ToFloat(GameManager.P1_SPAWN_Y), 0);
                prevP2VisualPos = new Vector3(FixedMath.ToFloat(GameManager.P2_SPAWN_X), FixedMath.ToFloat(GameManager.P2_SPAWN_Y), 0);
                if (player1Sprite != null) player1Sprite.transform.position = prevP1VisualPos;
                if (player2Sprite != null) player2Sprite.transform.position = prevP2VisualPos;
                Debug.Log("Round reset!");
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (NetworkManager.Instance != null &&
                    NetworkManager.Instance.State == NetworkManager.ConnectionState.Connected)
                {
                    NetworkManager.Instance.Disconnect();
                }
                else
                {
                    OnDisconnectedFromGame();
                }
            }

            if (!isLocalGame && NetworkManager.Instance != null &&
                NetworkManager.Instance.State == NetworkManager.ConnectionState.Disconnected &&
                gameStarted)
            {
                OnDisconnectedFromGame();
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
            background.transform.position = new Vector3(mainCam.transform.position.x, mainCam.transform.position.y, 5f);
        }

        private void FixedUpdate()
        {
            if (!gameStarted) return;

            CollectLocalInput();
            localInput.buttons |= bufferedInput.buttons;
            bufferedInput = InputData.Create(0);
            InputData bufferedP2 = bufferedInput2;
            bufferedInput2 = InputData.Create(0);

            if (NetworkManager.Instance != null &&
                NetworkManager.Instance.State == NetworkManager.ConnectionState.Connected)
            {
                NetworkManager.Instance.RecordLocalInput(currentFrame, localInput);
                if (GameManager.Instance != null)
                {
                    uint checksum = GameManager.Instance.currentState.ComputeChecksum();
                    NetworkManager.Instance.RecordLocalChecksum(currentFrame, checksum);
                }
                NetworkManager.Instance.SendInputs(currentFrame);
                HandleRollback();
            }
            else
            {
                // Couch multiplayer: player 2 controls their own character locally instead
                // of an idle dummy, mirroring how player 1's buffered attacks are merged in.
                remoteInput = CollectP2LocalInput();
                remoteInput.buttons |= bufferedP2.buttons;
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

        // ──────────────────────────────────────────────
        //  VISUALS + AUDIO TRIGGERS
        // ──────────────────────────────────────────────

        private bool prevShowRedWin;
        private bool prevShowBlueWin;

        private void UpdateVisuals()
        {
            if (GameManager.Instance == null) return;

            GameState state = GameManager.Instance.currentState;

            if (player1Sprite == null)
            {
                var p1 = GameObject.Find("Player1_Red");
                if (p1 != null)
                {
                    player1Sprite = p1.GetComponent<SpriteRenderer>();
                    prevP1VisualPos = p1.transform.position;
                }
            }
            if (player2Sprite == null)
            {
                var p2 = GameObject.Find("Player2_Blue");
                if (p2 != null)
                {
                    player2Sprite = p2.GetComponent<SpriteRenderer>();
                    prevP2VisualPos = p2.transform.position;
                }
            }
            if (mainCam == null) mainCam = Camera.main;

            if (player2Sprite != null)
            {
                player2Sprite.enabled = true;

                // Online joiner: swap visuals so local player is on the left
                bool swap = !isLocalGame && !IsLocalHost();

                PlayerState visualP2 = swap ? state.player1 : state.player2;
                Sprite[][] blueSpr = swap ? redSprites : blueSprites;
                int blueR = swap ? redRows : blueRows;

                Vector3 targetP2 = new Vector3(
                    FixedMath.ToFloat(visualP2.x),
                    FixedMath.ToFloat(visualP2.y), 0);
                player2Sprite.transform.position = Vector3.Lerp(prevP2VisualPos, targetP2, 0.3f);
                prevP2VisualPos = player2Sprite.transform.position;
                player2Sprite.flipX = false;
                var sprite = GetSprite(blueSpr, blueR, visualP2.animState, visualP2.animIndex);
                if (sprite != null) player2Sprite.sprite = sprite;
            }

            if (player1Sprite != null)
            {
                bool swap = !isLocalGame && !IsLocalHost();

                PlayerState visualP1 = swap ? state.player2 : state.player1;
                Sprite[][] redSpr = swap ? blueSprites : redSprites;
                int redR = swap ? blueRows : redRows;

                Vector3 targetP1 = new Vector3(
                    FixedMath.ToFloat(visualP1.x),
                    FixedMath.ToFloat(visualP1.y), 0);
                player1Sprite.transform.position = Vector3.Lerp(prevP1VisualPos, targetP1, 0.3f);
                prevP1VisualPos = player1Sprite.transform.position;
                player1Sprite.flipX = false;
                var sprite = GetSprite(redSpr, redR, visualP1.animState, visualP1.animIndex);
                if (sprite != null) player1Sprite.sprite = sprite;
            }

            if (player1Sprite != null)
            {
                float p1x = FixedMath.ToFloat(state.player1.x);
                float p2x = FixedMath.ToFloat(state.player2.x);
                float midX = (p1x + p2x) * 0.5f;
                float distBetween = Mathf.Abs(p1x - p2x);

                // Tekken-style: fixed Y, camera only pans horizontally and zooms
                float camY = 3.5f;
                float orthoSize = 5f;

                if (state.roundStartTimer > 0)
                {
                    // Pre-round: zoom in on center
                    orthoSize = Mathf.Lerp(mainCam.orthographicSize, 3f, 0.08f);
                    midX = Mathf.Lerp(mainCam.transform.position.x, midX, 0.08f);
                }
                else
                {
                    // Dynamic zoom based on distance between players
                    float targetOrtho = Mathf.Clamp(distBetween * 0.18f + 3f, 4f, 9f);
                    orthoSize = Mathf.Lerp(mainCam.orthographicSize, targetOrtho, 0.1f);
                    midX = Mathf.Lerp(mainCam.transform.position.x, midX, 0.12f);
                }

                mainCam.transform.position = new Vector3(midX, camY, -10f);
                mainCam.orthographicSize = orthoSize;
            }

            // ── Audio triggers ──
            PlayCombatAudio(state);

            UpdateUI(state);

            prevShowRedWin = state.showRedWin;
            prevShowBlueWin = state.showBlueWin;
        }

        private void PlayCombatAudio(GameState state)
        {
            if (AudioManager.Instance == null) return;

            if (state.showRedWin && !state.showBlueWin && !prevShowRedWin)
                AudioManager.Instance.PlayRedWin();
            if (state.showRedWin && state.showBlueWin && !prevShowRedWin)
                AudioManager.Instance.PlayDraw();

            if (state.timer <= 10 && state.timer > 0)
                AudioManager.Instance.PlayTimerTick(state.timer);

            bool p1Hurt = state.player1.health < 100 && state.player1.health > 0;
            bool p2Hurt = state.player2.health < 100 && state.player2.health > 0;
        }

        private void UpdateUI(GameState state)
        {
            bool showHUD = !state.isGameOver || state.showRedWin || state.showBlueWin;

            if (timerText != null)
            {
                timerText.text = state.timer.ToString("D2");
                timerText.gameObject.SetActive(showHUD);
            }

            if (roundText != null)
            {
                roundText.text = $"ROUND {state.round}";
                roundText.gameObject.SetActive(showHUD);
            }

            // Online joiner: swap HUD elements so local player is always on the left
            bool hudSwap = !isLocalGame && !IsLocalHost();

            var showP1Health = hudSwap ? p2HealthBar : p1HealthBar;
            var showP2Health = hudSwap ? p1HealthBar : p2HealthBar;
            var showP1Frame = hudSwap ? p2HealthFrame : p1HealthFrame;
            var showP2Frame = hudSwap ? p1HealthFrame : p2HealthFrame;
            var showP1Stamina = hudSwap ? p2StaminaBar : p1StaminaBar;
            var showP2Stamina = hudSwap ? p1StaminaBar : p2StaminaBar;
            var showP1Name = hudSwap ? p2NameTag : p1NameTag;
            var showP2Name = hudSwap ? p1NameTag : p2NameTag;
            var showP1Cooldown = hudSwap ? p2CooldownText : p1CooldownText;
            var showP2Cooldown = hudSwap ? p1CooldownText : p2CooldownText;

            // Health
            var p1Health = hudSwap ? state.player2 : state.player1;
            var p2Health = hudSwap ? state.player1 : state.player2;
            if (showP1Health != null) { showP1Health.fillAmount = (float)p1Health.health / PlayerController.MAX_HEALTH; showP1Health.gameObject.SetActive(showHUD); }
            if (showP2Health != null) { showP2Health.fillAmount = (float)p2Health.health / PlayerController.MAX_HEALTH; showP2Health.gameObject.SetActive(showHUD); }

            // Stamina
            var p1Stam = hudSwap ? state.player2 : state.player1;
            var p2Stam = hudSwap ? state.player1 : state.player2;
            if (showP1Stamina != null)
            {
                showP1Stamina.fillAmount = (float)p1Stam.stamina / PlayerController.MAX_STAMINA;
                showP1Stamina.color = p1Stam.slowTimer > 0 ? new Color(0.5f, 0.3f, 1f) : new Color(0.2f, 0.8f, 0.2f);
                showP1Stamina.gameObject.SetActive(showHUD);
            }
            if (showP2Stamina != null)
            {
                showP2Stamina.fillAmount = (float)p2Stam.stamina / PlayerController.MAX_STAMINA;
                showP2Stamina.color = p2Stam.slowTimer > 0 ? new Color(0.5f, 0.3f, 1f) : new Color(0.2f, 0.8f, 0.2f);
                showP2Stamina.gameObject.SetActive(showHUD);
            }

            for (int i = 0; i < 3; i++)
            {
                if (p1WinDots[i] != null)
                {
                    p1WinDots[i].color = i < state.player1Wins ? Color.red : new Color(0.3f, 0.3f, 0.3f);
                    p1WinDots[i].gameObject.SetActive(showHUD);
                }
                if (p2WinDots[i] != null)
                {
                    p2WinDots[i].color = i < state.player2Wins ? new Color(0.3f, 0.6f, 1f) : new Color(0.3f, 0.3f, 0.3f);
                    p2WinDots[i].gameObject.SetActive(showHUD);
                }
            }

            // Cooldowns
            var cdP1 = hudSwap ? state.player2 : state.player1;
            var cdP2 = hudSwap ? state.player1 : state.player2;
            if (showP1Cooldown != null)
            {
                showP1Cooldown.text = $"J:{CdStr(cdP1.attackCooldownLeft)}  K:{CdStr(cdP1.sungkitCooldownLeft)}  L:{CdStr(cdP1.launchCooldownLeft)}";
                showP1Cooldown.gameObject.SetActive(showHUD);
            }
            if (showP2Cooldown != null)
            {
                showP2Cooldown.text = $"J:{CdStr(cdP2.attackCooldownLeft)}  K:{CdStr(cdP2.sungkitCooldownLeft)}  L:{CdStr(cdP2.launchCooldownLeft)}";
                showP2Cooldown.gameObject.SetActive(showHUD);
            }
            if (showP1Name != null) showP1Name.gameObject.SetActive(showHUD);
            if (showP2Name != null) showP2Name.gameObject.SetActive(showHUD);
            if (showP1Frame != null) showP1Frame.gameObject.SetActive(showHUD);
            if (showP2Frame != null) showP2Frame.gameObject.SetActive(showHUD);

            if (redWinOverlay != null) redWinOverlay.gameObject.SetActive(state.showRedWin);
            if (blueWinOverlay != null) blueWinOverlay.gameObject.SetActive(state.showBlueWin);
            if (drawOverlay != null) drawOverlay.gameObject.SetActive(state.showRedWin && state.showBlueWin);
            if (pauseOverlay != null) pauseOverlay.gameObject.SetActive(state.isPaused);

            // Game over overlays
            bool isGameOverNow = state.isGameOver && !state.showRedWin && !state.showBlueWin;
            if (isGameOverNow != prevIsGameOver)
            {
                if (gameOverCanvas != null) gameOverCanvas.SetActive(isGameOverNow);
                var gameOverBtns = new[] { "AgainBtn", "ExitBtn" };
                foreach (var btnName in gameOverBtns)
                {
                    var btnObj = GameObject.Find(btnName);
                    if (btnObj != null) btnObj.SetActive(isGameOverNow);
                }
            }
            if (isGameOverNow)
            {
                bool redWins = state.player1Wins >= GameManager.Instance.winsNeeded;
                bool blueWins = state.player2Wins >= GameManager.Instance.winsNeeded;
                if (gameOverRedWinsOverlay != null) gameOverRedWinsOverlay.gameObject.SetActive(redWins);
                if (gameOverBlueWinsOverlay != null) gameOverBlueWinsOverlay.gameObject.SetActive(blueWins);
                if (gameOverDrawOverlay != null) gameOverDrawOverlay.gameObject.SetActive(!redWins && !blueWins);
            }
            prevIsGameOver = isGameOverNow;
        }

        private static string CdStr(int frames)
        {
            if (frames <= 0) return "READY";
            return $"{frames / 60f:F1}s";
        }

        // ──────────────────────────────────────────────
        //  INPUT
        // ──────────────────────────────────────────────

        // Controller support: analog stick axes are defined per-joystick in
        // ProjectSettings/InputManager.asset (P1_Horizontal/Vertical -> joystick 1,
        // P2_Horizontal/Vertical -> joystick 2), so each player's pad is independent
        // of the other's and of the keyboard.
        private const float STICK_THRESHOLD = 0.5f;

        private void CollectLocalInput()
        {
            localInput = InputData.Create(currentFrame);
            if (Input.GetKey(KeyCode.A)) localInput.buttons |= InputData.LEFT;
            if (Input.GetKey(KeyCode.D)) localInput.buttons |= InputData.RIGHT;
            if (Input.GetKey(KeyCode.W)) localInput.buttons |= InputData.UP;
            if (Input.GetKey(KeyCode.S)) localInput.buttons |= InputData.DOWN;
            if (Input.GetKey(KeyCode.Space)) localInput.buttons |= InputData.BLOCK;

            // Controller 1 (optional, works alongside the keyboard)
            float h1 = Input.GetAxis("P1_Horizontal");
            float v1 = Input.GetAxis("P1_Vertical");
            if (h1 < -STICK_THRESHOLD) localInput.buttons |= InputData.LEFT;
            if (h1 > STICK_THRESHOLD) localInput.buttons |= InputData.RIGHT;
            if (v1 > STICK_THRESHOLD) localInput.buttons |= InputData.UP;
            if (v1 < -STICK_THRESHOLD) localInput.buttons |= InputData.DOWN;
            if (Input.GetKey(KeyCode.Joystick1Button4)) localInput.buttons |= InputData.BLOCK;

            // Online joiner: mirror left/right so your character moves correctly on your screen
            if (!isLocalGame && NetworkManager.Instance != null &&
                NetworkManager.Instance.State == NetworkManager.ConnectionState.Connected &&
                !NetworkManager.Instance.IsHost)
            {
                bool hadLeft = (localInput.buttons & InputData.LEFT) != 0;
                bool hadRight = (localInput.buttons & InputData.RIGHT) != 0;
                localInput.buttons &= unchecked((byte)~(InputData.LEFT | InputData.RIGHT));
                if (hadLeft) localInput.buttons |= InputData.RIGHT;
                if (hadRight) localInput.buttons |= InputData.LEFT;
            }
        }

        // Couch multiplayer: player 2's movement, sharing the same keyboard (arrows + numpad)
        // or their own controller (joystick 2).
        private InputData CollectP2LocalInput()
        {
            InputData input = InputData.Create(currentFrame);
            if (Input.GetKey(KeyCode.LeftArrow)) input.buttons |= InputData.LEFT;
            if (Input.GetKey(KeyCode.RightArrow)) input.buttons |= InputData.RIGHT;
            if (Input.GetKey(KeyCode.UpArrow)) input.buttons |= InputData.UP;
            if (Input.GetKey(KeyCode.DownArrow)) input.buttons |= InputData.DOWN;

            float h2 = Input.GetAxis("P2_Horizontal");
            float v2 = Input.GetAxis("P2_Vertical");
            if (h2 < -STICK_THRESHOLD) input.buttons |= InputData.LEFT;
            if (h2 > STICK_THRESHOLD) input.buttons |= InputData.RIGHT;
            if (v2 > STICK_THRESHOLD) input.buttons |= InputData.UP;
            if (v2 < -STICK_THRESHOLD) input.buttons |= InputData.DOWN;
            if (Input.GetKey(KeyCode.Joystick2Button4)) input.buttons |= InputData.BLOCK;
            if (Input.GetKey(KeyCode.Keypad0)) input.buttons |= InputData.BLOCK;
            return input;
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
                        // Read from NetworkManager's live buffers (not a local cache) so that
                        // a remote input which has since arrived/corrected is actually used.
                        InputData local = NetworkManager.Instance.LocalInputBuffer[f % 256];
                        NetworkManager.Instance.GetRemoteInput(f, out InputData remote);
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
            if (showDisconnectedMsg && showConnectionUI)
            {
                var style = new GUIStyle(GUI.skin.label);
                style.fontSize = 48;
                style.alignment = TextAnchor.MiddleCenter;
                style.normal.textColor = Color.red;
                GUI.Label(new Rect(0, Screen.height * 0.4f, Screen.width, 60),
                    "OPPONENT DISCONNECTED", style);
                return;
            }

            if (showConnectionUI) return;

            if (!showDebugInfo) return;

            int y = 10;
            int lh = 20;

            GUI.Label(new Rect(10, y, 500, lh), $"Frame: {currentFrame}  Focused: {Application.isFocused}");
            y += lh;

            if (NetworkManager.Instance != null &&
                NetworkManager.Instance.State == NetworkManager.ConnectionState.Connected)
            {
                GUI.Label(new Rect(10, y, 500, lh), $"Ping: {NetworkManager.Instance.PingMs}ms  Rollback: {NetworkManager.Instance.Rollback.HistorySize} states  HitStop: {GameManager.Instance?.currentState.hitStopFrames ?? 0}");
                y += lh;
                GUI.Label(new Rect(10, y, 500, lh), $"Local Checksum: {NetworkManager.Instance.LastLocalChecksum:X8}  Remote: {NetworkManager.Instance.LastRemoteChecksum:X8}  Mismatch: {NetworkManager.Instance.ChecksumMismatch}");
                y += lh;
            }

            if (GameManager.Instance != null)
            {
                GameState state = GameManager.Instance.currentState;
                var p1 = state.player1;
                var p2 = state.player2;
                GUI.Label(new Rect(10, y, 900, lh),
                    $"P1: hp={p1.health} stm={p1.stamina} slow={p1.slowTimer} blkTmr={p1.blockTimer} gnd={p1.isOnGround} anim={p1.animState}[{p1.animIndex}] pos=({p1.x},{p1.y})");
                y += lh;
                GUI.Label(new Rect(10, y, 900, lh),
                    $"P2: hp={p2.health} stm={p2.stamina} slow={p2.slowTimer} blkTmr={p2.blockTimer} gnd={p2.isOnGround} anim={p2.animState}[{p2.animIndex}] pos=({p2.x},{p2.y})");
                y += lh;
                GUI.Label(new Rect(10, y, 500, lh), $"Round: {state.round}  Timer: {state.timer}  Wins: P1={state.player1Wins} P2={state.player2Wins}  First to {GameManager.Instance.winsNeeded}");
                y += lh;

                DrawHitboxes(state);
            }
        }

        private void DrawHitboxes(GameState state)
        {
            if (mainCam == null) return;

            // Platform collision rects (cyan, thin outline)
            var plat = GameManager.Instance?.Platforms;
            if (plat != null)
            {
                DrawPlatformCollision(plat.MainX, plat.MainWidth, plat.MainY, new Color(0f, 1f, 1f, 0.25f));
                DrawPlatformCollision(plat.LeftX, plat.LeftWidth, plat.LeftY, new Color(0f, 1f, 1f, 0.25f));
                DrawPlatformCollision(plat.RightX, plat.RightWidth, plat.RightY, new Color(0f, 1f, 1f, 0.25f));
            }

            // Player 1 body hitbox (green)
            DrawPlayerHitbox(state.player1, Color.green);
            // Player 1 attack hitbox (red)
            DrawAttackHitbox(state.player1, new Color(1f, 0.2f, 0.2f));

            // Player 2 body hitbox (blue)
            DrawPlayerHitbox(state.player2, new Color(0.3f, 0.6f, 1f));
            // Player 2 attack hitbox (yellow)
            DrawAttackHitbox(state.player2, Color.yellow);
        }

        private void DrawPlatformCollision(int platX, int platWidth, int platY, Color color)
        {
            float x = FixedMath.ToFloat(platX);
            float w = FixedMath.ToFloat(platWidth);
            float y = FixedMath.ToFloat(platY);

            // Draw a clear horizontal line at the exact collision surface
            Rect surface = new Rect(x, y - 0.05f, w, 0.1f);
            DrawRectFilled(surface, new Color(color.r, color.g, color.b, 0.9f));

            // Thin outline just below the surface to show collision depth
            Rect depth = new Rect(x, y - 0.3f, w, 0.3f);
            DrawRectOutline(depth, new Color(color.r, color.g, color.b, 0.5f), 2f);
        }

        private void DrawPlayerHitbox(PlayerState player, Color color)
        {
            DrawRectOutline(PlayerController.GetPlayerHitbox(player), color);
        }

        private void DrawAttackHitbox(PlayerState player, Color color)
        {
            if (!player.attacking && !player.sungkit && !player.launch) return;
            Rect hitbox = PlayerController.GetAttackHitbox(player);
            if (hitbox.width > 0 && hitbox.height > 0) DrawRectFilled(hitbox, color);
        }

        private void DrawRectFilled(Rect worldRect, Color color)
        {
            Vector3 bl = mainCam.WorldToScreenPoint(new Vector3(worldRect.x, worldRect.y, 0));
            Vector3 tr = mainCam.WorldToScreenPoint(new Vector3(worldRect.x + worldRect.width, worldRect.y + worldRect.height, 0));
            float x = Mathf.Min(bl.x, tr.x);
            float yScreen = Screen.height - Mathf.Max(bl.y, tr.y);
            float w = Mathf.Abs(tr.x - bl.x);
            float h = Mathf.Abs(tr.y - bl.y);
            if (w < 1f) w = 1f;
            if (h < 1f) h = 1f;

            if (debugTex == null) { debugTex = new Texture2D(1, 1); debugTex.SetPixel(0, 0, Color.white); debugTex.Apply(); }
            var prevColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(x, yScreen, w, h), debugTex);
            GUI.color = prevColor;
        }

        private void DrawRectOutline(Rect worldRect, Color color, float thickness = 2f)
        {
            Vector3 bl = mainCam.WorldToScreenPoint(new Vector3(worldRect.x, worldRect.y, 0));
            Vector3 tr = mainCam.WorldToScreenPoint(new Vector3(worldRect.x + worldRect.width, worldRect.y + worldRect.height, 0));
            float x = Mathf.Min(bl.x, tr.x);
            float yScreen = Screen.height - Mathf.Max(bl.y, tr.y);
            float w = Mathf.Abs(tr.x - bl.x);
            float h = Mathf.Abs(tr.y - bl.y);
            if (w < 1f) w = 1f;
            if (h < 1f) h = 1f;

            if (debugTex == null) { debugTex = new Texture2D(1, 1); debugTex.SetPixel(0, 0, Color.white); debugTex.Apply(); }
            var prevColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(x, yScreen, w, thickness), debugTex);           // top
            GUI.DrawTexture(new Rect(x, yScreen + h - thickness, w, thickness), debugTex); // bottom
            GUI.DrawTexture(new Rect(x, yScreen, thickness, h), debugTex);           // left
            GUI.DrawTexture(new Rect(x + w - thickness, yScreen, thickness, h), debugTex); // right
            GUI.color = prevColor;
        }

        private static Texture2D debugTex;
    }
}
