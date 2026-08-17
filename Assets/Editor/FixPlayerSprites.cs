using UnityEngine;
using UnityEditor;

namespace LABANAN.Editor
{
    public static class FixPlayerSprites
    {
        [MenuItem("LABANAN/Fix Player Sprites")]
        public static void Fix()
        {
            var p1 = GameObject.Find("Player1_Red");
            var p2 = GameObject.Find("Player2_Blue");

            if (p1 != null)
            {
                var sr = p1.GetComponent<SpriteRenderer>();
                if (sr == null) sr = p1.AddComponent<SpriteRenderer>();
                sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Red/RED_SPRITESHEET.png");
                sr.sortingOrder = 1;
                if (p1.GetComponent<RectTransform>() == null)
                {
                    // Ensure proper scale
                }
                Debug.Log("Player 1 sprite assigned: " + (sr.sprite != null ? sr.sprite.name : "NULL"));
            }
            else
            {
                Debug.LogError("Player1_Red not found in scene");
            }

            if (p2 != null)
            {
                var sr = p2.GetComponent<SpriteRenderer>();
                if (sr == null) sr = p2.AddComponent<SpriteRenderer>();
                sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Blue/BLUE_SPRITESHEET.png");
                sr.sortingOrder = 1;
                if (p2.transform.localScale.x > 0)
                    p2.transform.localScale = new Vector3(-1, 1, 1);
                Debug.Log("Player 2 sprite assigned: " + (sr.sprite != null ? sr.sprite.name : "NULL"));
            }
            else
            {
                Debug.LogError("Player2_Blue not found in scene");
            }

            // Fix camera to 2D orthographic looking at the right area
            var cam = Camera.main;
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = 6;
                cam.transform.position = new Vector3(9, 5, -10);
            }

            // Add missing UI references
            var canvas = GameObject.Find("Canvas");
            if (canvas != null && canvas.GetComponent<UIManager>() == null)
            {
                canvas.AddComponent<UIManager>();
                Debug.Log("UIManager added to Canvas");
            }

            EditorUtility.SetDirty(p1);
            EditorUtility.SetDirty(p2);
            AssetDatabase.SaveAssets();
            Debug.Log("Player sprites fixed!");
        }
    }
}
