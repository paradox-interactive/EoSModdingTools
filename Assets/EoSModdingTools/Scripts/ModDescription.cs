using System;

namespace RomeroGames
{
    [Serializable]
    public class ModDescription
    {
        public const string ModDescriptionFile = "ModDescription.json";

        public int FormatVersion = 1;
        public string ModName;
        public string Title;
        public string GameVersion;
        public ulong SteamWorkshopId;
        public int ParadoxModsId;
    }
}