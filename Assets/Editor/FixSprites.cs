using UnityEngine;
using UnityEditor;

namespace LABANAN.Editor
{
    public static class FixSprites
    {
        [MenuItem("LABANAN/Setup Sprites & Fix Players")]
        public static void FixAll()
        {
            var p1 = GameObject.Find("Player1_Red");
            var p2 = GameObject.Find("Player2_Blue");

            if (p1 != null)
            {
                var sr = p1.GetComponent<SpriteRenderer>();
                if (sr == null) sr = p1.AddComponent<SpriteRenderer>();
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sprites/Red/RED_SPRITESHEET.png");
                if (tex != null)
                {
                    sr.sprite = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0f), 64);
                }
                sr.sortingOrder = 1;
                EditorUtility.SetDirty(p1);
            }

            if (p2 != null)
            {
                var sr = p2.GetComponent<SpriteRenderer>();
                if (sr == null) sr = p2.AddComponent<SpriteRenderer>();
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sprites/Blue/BLUE_SPRITESHEET.png");
                if (tex != null)
                {
                    sr.sprite = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0f), 64);
                }
                sr.sortingOrder = 1;
                p2.transform.localScale = new Vector3(-1, 1, 1);
                EditorUtility.SetDirty(p2);
            }

            var cam = Camera.main;
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = 6;
                cam.transform.position = new Vector3(9, 5, -10);
            }

            Debug.Log("Done!");
        }
    }
}
