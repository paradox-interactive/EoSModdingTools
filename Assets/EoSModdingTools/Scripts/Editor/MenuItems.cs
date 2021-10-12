using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;

namespace RomeroGames
{
    public static class MenuItems
    {
        [MenuItem("Tools/Modding Tools/Create New Mod")]
        private static void CreateNewMod()
        {
            string modName = EditorInputDialog.Show( "Create New Mod", "Please Enter the name of the new mod", string.Empty );
            string title = modName.Trim();

            // White space in mod folder names causes problems when publishing to Paradox Mods
            // Just remove the whitespace and carry on.

            modName = Regex.Replace(modName, @"\s+", "");
            if (string.IsNullOrEmpty(modName))
            {
                return;
            }

            var invalidPathChars = Path.GetInvalidPathChars();
            foreach (char c in modName)
            {
                if (ArrayUtility.Contains(invalidPathChars, c))
                {
                    Debug.LogError($"Failed to create mod '{modName}' because it contains invalid path character '{c}'");
                    return;
                }
            }

            string modPath = ModUtils.CleanPath(Path.Combine(ModUtils.ModsFolder, modName));
            if (AssetDatabase.IsValidFolder(modPath))
            {
                Debug.LogError($"A mod with the same name already exists at: {modPath}");
                return;
            }

            if (!AssetDatabase.IsValidFolder(ModUtils.ModsFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Mods");
            }

            AssetDatabase.CreateFolder(ModUtils.ModsFolder, modName);

            ModConfig asset = ScriptableObject.CreateInstance<ModConfig>();
            asset.Title = title;

            string assetPath = Path.Combine(modPath, $"ModConfig.asset");
            assetPath = ModUtils.CleanPath(assetPath);

            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.CreateFolder(Path.Combine(ModUtils.ModsFolder, modName), "Lua");
            AssetDatabase.CreateFolder(Path.Combine(ModUtils.ModsFolder, modName, "Lua"), "Scripts");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;

            Debug.Log($"Created new mod at: {modPath}");
        }

#if EXCLUDE_MODDING_SUPPORT
        [MenuItem("Tools/Modding Tools/Export Modding Tools")]
        private static void ExportModdingTools()
        {
            ModUtils.ExportModdingTools();
        }

        [MenuItem("Tools/Modding Tools/Export GameSource")]
        public static void ExportGameSource()
        {
            string path = EditorUtility.SaveFilePanel("Export game source archive", "", "GameSource.zip", "zip");
            if (path.Length != 0)
            {
                if (ModUtils.ExportGameSource(path))
                {
                    Debug.Log("Exported game source to: {path}");
                }
                else
                {
                    Debug.LogError("Failed to export game source to: {path}");
                }
            }
        }
#endif
    }
}

