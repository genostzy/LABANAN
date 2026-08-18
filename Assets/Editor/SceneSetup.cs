using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace LABANAN.Editor
{
    public static class SceneSetup
    {
        [UnityEditor.MenuItem("LABANAN/Setup Main Menu Scene")]
        public static void SetupMainMenu()
        {
            var cam = new GameObject("Main Camera");
            cam.tag = "MainCamera";
            var camera = cam.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5;
            camera.backgroundColor = Color.black;
            camera.clearFlags = CameraClearFlags.SolidColor;
            cam.transform.position = new Vector3(0, 0, -10);
            cam.AddComponent<AudioListener>();

            var canvasObj = new GameObject("Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            CreateText(canvasObj.transform, "TitleText", "LABANAN", 72, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.7f), new Vector2(600, 100));
            CreateButton(canvasObj.transform, "OnlineButton", "ONLINE", 36,
                new Vector2(0.5f, 0.4f), new Vector2(300, 60));
            CreateButton(canvasObj.transform, "ExitButton", "EXIT", 36,
                new Vector2(0.5f, 0.25f), new Vector2(300, 60));

            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            var audioMgr = new GameObject("AudioManager");
            audioMgr.AddComponent<AudioManager>();
            audioMgr.AddComponent<AudioSource>();
            audioMgr.AddComponent<AudioSource>();

            Debug.Log("Main Menu scene created!");
        }

        [UnityEditor.MenuItem("LABANAN/Setup Online Lobby Scene")]
        public static void SetupOnlineLobby()
        {
            var cam = new GameObject("Main Camera");
            cam.tag = "MainCamera";
            var camera = cam.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5;
            camera.backgroundColor = Color.black;
            camera.clearFlags = CameraClearFlags.SolidColor;
            cam.transform.position = new Vector3(0, 0, -10);
            cam.AddComponent<AudioListener>();

            var canvasObj = new GameObject("Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            CreateText(canvasObj.transform, "TitleText", "ONLINE LOBBY", 48, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.8f), new Vector2(400, 80));
            CreateButton(canvasObj.transform, "CreateRoomButton", "CREATE ROOM", 32,
                new Vector2(0.5f, 0.55f), new Vector2(300, 60));
            CreateButton(canvasObj.transform, "JoinRoomButton", "JOIN ROOM", 32,
                new Vector2(0.5f, 0.4f), new Vector2(300, 60));
            CreateButton(canvasObj.transform, "BackButton", "BACK", 28,
                new Vector2(0.5f, 0.2f), new Vector2(200, 50));

            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            Debug.Log("Online Lobby scene created!");
        }

        [UnityEditor.MenuItem("LABANAN/Setup Game Scene")]
        public static void SetupGame()
        {
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go.scene.isLoaded && go.name != "Directional Light")
                    Object.DestroyImmediate(go);
            }

            // Camera - centered between both players (6 and 12), slightly above platforms
            var cam = new GameObject("Main Camera");
            cam.tag = "MainCamera";
            var camera = cam.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6;
            camera.backgroundColor = new Color(0.05f, 0.05f, 0.1f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            cam.transform.position = new Vector3(9, 1.5f, -10);
            cam.AddComponent<AudioListener>();

            // Background - will be set up at runtime by GameLoop
            var bg = new GameObject("Background");
            bg.transform.position = new Vector3(9f, 2.5f, 5);
            var bgSR = bg.AddComponent<SpriteRenderer>();
            bgSR.sortingOrder = -1;
            bgSR.sprite = CreateSquareSprite();
            bgSR.color = new Color(0.05f, 0.05f, 0.1f);
            bg.transform.localScale = new Vector3(20f, 12f, 1f);

            // Managers
            var managers = new GameObject("Managers");
            managers.AddComponent<GameLoop>();
            managers.AddComponent<GameManager>();
            managers.AddComponent<NetworkManager>();

            // AudioManager
            var audioMgr = new GameObject("AudioManager");
            audioMgr.AddComponent<AudioManager>();
            audioMgr.AddComponent<AudioSource>();
            audioMgr.AddComponent<AudioSource>();

            // Player 1 (Red) - spawn on main platform
            var p1 = new GameObject("Player1_Red");
            p1.transform.position = new Vector3(6f, 0.5f, 0);
            var sr1 = p1.AddComponent<SpriteRenderer>();
            sr1.sortingOrder = 2;
            var redTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sprites/Red/RED_SPRITESHEET.png");
            if (redTex != null)
                sr1.sprite = Sprite.Create(redTex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0f), 64);

            // Player 2 (Blue) - spawn on main platform
            var p2 = new GameObject("Player2_Blue");
            p2.transform.position = new Vector3(12f, 0.5f, 0);
            var sr2 = p2.AddComponent<SpriteRenderer>();
            sr2.sortingOrder = 2;
            var blueTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sprites/Blue/BLUE_SPRITESHEET.png");
            if (blueTex != null)
                sr2.sprite = Sprite.Create(blueTex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0f), 64);

            // Platform visuals - match collision rects exactly (hidden at runtime by GameLoop)
            // Main platform: X=3.0-15.0, Y=0.0-0.5
            var mainPlat = new GameObject("MainPlatform");
            mainPlat.transform.position = new Vector3(9f, 0.25f, 0);
            var mainSR = mainPlat.AddComponent<SpriteRenderer>();
            mainSR.sortingOrder = 1;
            mainSR.color = new Color(0.3f, 0.35f, 0.3f);
            mainSR.sprite = CreateSquareSprite();
            mainPlat.transform.localScale = new Vector3(12f, 0.5f, 1f);

            // Left platform: X=1.5-4.5, Y=3.0-3.5
            var leftPlat = new GameObject("LeftPlatform");
            leftPlat.transform.position = new Vector3(3f, 3.25f, 0);
            var leftSR = leftPlat.AddComponent<SpriteRenderer>();
            leftSR.sortingOrder = 1;
            leftSR.color = new Color(0.35f, 0.3f, 0.3f);
            leftSR.sprite = CreateSquareSprite();
            leftPlat.transform.localScale = new Vector3(3f, 0.5f, 1f);

            // Right platform: X=13.5-16.5, Y=3.0-3.5
            var rightPlat = new GameObject("RightPlatform");
            rightPlat.transform.position = new Vector3(15f, 3.25f, 0);
            var rightSR = rightPlat.AddComponent<SpriteRenderer>();
            rightSR.sortingOrder = 1;
            rightSR.color = new Color(0.3f, 0.3f, 0.35f);
            rightSR.sprite = CreateSquareSprite();
            rightPlat.transform.localScale = new Vector3(3f, 0.5f, 1f);

            // Canvas
            var canvasObj = new GameObject("Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            // Health Bars
            CreateImage(canvasObj.transform, "P1HealthBar", Color.red,
                new Vector2(0.02f, 0.92f), new Vector2(0.35f, 0.97f));
            CreateImage(canvasObj.transform, "P2HealthBar", new Color(0, 0.5f, 1f),
                new Vector2(0.65f, 0.92f), new Vector2(0.98f, 0.97f));

            CreateText(canvasObj.transform, "TimerText", "60", 48, TextAnchor.MiddleCenter,
                new Vector2(0.45f, 0.92f), new Vector2(0.55f, 0.98f));
            CreateText(canvasObj.transform, "RoundText", "Round 1", 32, TextAnchor.MiddleCenter,
                new Vector2(0.35f, 0.82f), new Vector2(0.65f, 0.9f));
            CreateText(canvasObj.transform, "P1WinsText", "Wins: 0", 28, TextAnchor.MiddleLeft,
                new Vector2(0.02f, 0.85f), new Vector2(0.15f, 0.92f), Color.red);
            CreateText(canvasObj.transform, "P2WinsText", "Wins: 0", 28, TextAnchor.MiddleRight,
                new Vector2(0.85f, 0.85f), new Vector2(0.98f, 0.92f), new Color(0, 0.5f, 1f));

            var laban = CreateText(canvasObj.transform, "LabanText", "LABAN!", 120, TextAnchor.MiddleCenter,
                new Vector2(0.2f, 0.3f), new Vector2(0.8f, 0.7f), Color.yellow);
            laban.gameObject.SetActive(false);

            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            canvasObj.AddComponent<UIManager>();

            Debug.Log("Game scene created!");
        }

        static Sprite CreateSquareSprite()
        {
            var tex = new Texture2D(4, 4);
            var colors = new Color[16];
            for (int i = 0; i < 16; i++) colors[i] = Color.white;
            tex.SetPixels(colors);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
        }

        static Text CreateText(Transform parent, string name, string text, int fontSize,
            TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax, Color? color = null)
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
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.alignment = alignment;
            t.color = color ?? Color.white;
            t.raycastTarget = false;
            return t;
        }

        static Button CreateButton(Transform parent, string name, string label, int fontSize,
            Vector2 anchorCenter, Vector2 size)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = anchorCenter - size * 0.5f / new Vector2(Screen.width, Screen.height);
            rt.anchorMax = anchorCenter + size * 0.5f / new Vector2(Screen.width, Screen.height);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = obj.AddComponent<Image>();
            img.color = new Color(0.8f, 0.8f, 0.8f);
            var btn = obj.AddComponent<Button>();

            var txt = CreateText(obj.transform, "Label", label, fontSize, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Color.black);
            txt.rectTransform.anchorMin = Vector2.zero;
            txt.rectTransform.anchorMax = Vector2.one;
            txt.rectTransform.offsetMin = Vector2.zero;
            txt.rectTransform.offsetMax = Vector2.zero;

            return btn;
        }

        static Image CreateImage(Transform parent, string name, Color color,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = obj.AddComponent<Image>();
            img.color = color;
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            return img;
        }
    }
}
