using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace RomeroGames
{
    [CreateAssetMenu(fileName = "ModConfig", menuName = "Modding Tools/ModConfig", order = 1)]
    public class ModConfig : ScriptableObject
    {
        public static readonly List<string> ValidTags = new List<string>
        {
            "Missions",
            "Combat",
            "Weapons",
            "Abilities",
            "Items",
            "Gangsters",
            "Bosses",
            "Gameplay Tweaks",
            "Overhauls",
            "Rackets",
            "Events"
        };

        private static string CleanPath(string path)
        {
            return path.Replace("\\", "/").Replace("//", "/");
        }

        private void OnEnable()
        {
            SetDefaultModsPath();
        }

        private string GetModAssetPath()
        {
            return UnityEditor.AssetDatabase.GetAssetPath(this);
        }

        [Tooltip("Name of the mod displayed in Paradox Launcher")]
        public string Title;

        [Tooltip("Recommended version of game to run this mod. You can use wildcards, e.g. '1.06.*'. Used by Paradox Mods only.")]
        public string GameVersion;

        [Tooltip("Short description displayed in Paradox Launcher. Used by Paradox Mods only.")]
        [TextArea(2, 10)]
        public string ShortDescription;

        [Tooltip("Long description displayed in Paradox Launcher. Used by both Paradox Mods and Steam Workshop.")]
        [TextArea(4, 10)]
        public string LongDescription;

        [Tooltip("Notes for the changes included in this version of the mod. Used by both Paradox Mods and Steam Workshop.")]
        [TextArea(4, 10)]
        public string ChangeNotes;

        public string PreviewImageData;
        public string PreviewImageFilename;

        public List<string> Tags = new List<string>();

        [Tooltip("Local path to install mods for testing. This must be the correct game data path for your game installation (see Modding Documentation)")]
        public string LocalModsPath;

        [Tooltip("Identifier for this mod on Steam Workshop. If this is 0, a new id will be assigned when you publish the mod.")]
        [FormerlySerializedAs("SteamModId")] public ulong SteamWorkshopId;

        [Tooltip("Identifier for this mod on Paradox Mods. If this is 0, a new id will be assigned when you publish the mod.")]
        [FormerlySerializedAs("ParadoxModId")] public int ParadoxModsId;

        [Tooltip("Email address used to sign into your Paradox account.")]
        public string ParadoxEmail;

        [Tooltip("Password used to sign into your Paradox account.")]
        public string ParadoxPassword;

        public string GetModPath() =>  Path.GetDirectoryName(GetModAssetPath());

        public string GetModName()
        {
            string path = GetModPath();
            path = Path.GetFileName(path);
            return path;
        }

        public void SetDefaultModsPath()
        {
            if (string.IsNullOrEmpty(LocalModsPath))
            {
                string dataPath = CleanPath(Application.persistentDataPath);
                DirectoryInfo parent = Directory.GetParent(dataPath).Parent;
                Assert.IsNotNull(parent);

                LocalModsPath = CleanPath(parent.FullName + "/Paradox Interactive/Empire of Sin/Mods");
            }
        }

    }
}