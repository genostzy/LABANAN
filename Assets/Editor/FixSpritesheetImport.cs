using UnityEngine;
using UnityEditor;

namespace LABANAN.Editor
{
    [InitializeOnLoad]
    public static class FixSpritesheetImport
    {
        static FixSpritesheetImport()
        {
            EditorApplication.delayCall += Fix;
        }

        [MenuItem("LABANAN/Fix Spritesheet Import Settings")]
        public static void Fix()
        {
            FixTexture("Assets/Resources/Sprites/Red/RED_SPRITESHEET.png");
            FixTexture("Assets/Resources/Sprites/Blue/BLUE_SPRITESHEET.png");
            FixTexture("Assets/Sprites/Red/RED_SPRITESHEET.png");
            FixTexture("Assets/Sprites/Blue/BLUE_SPRITESHEET.png");
            FixTexture("Assets/UI/BG NIGHT.png");
            FixTexture("Assets/UI/PLATFORM.png");
            FixTexture("Assets/Resources/Sprites/BG NIGHT.png");
            FixTexture("Assets/Resources/Sprites/PLATFORM.png");
            AssetDatabase.Refresh();
            Debug.Log("All textures fixed!");
        }

        static void FixTexture(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            importer.isReadable = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            Debug.Log($"Fixed: {path}");
        }
    }
}
