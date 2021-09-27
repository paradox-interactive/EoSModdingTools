namespace RomeroGames
{
    public class LocalizationItem
    {
        public string stringKey;
        public string source;
        public string comment;
        public string category;
        public string filename;
        public int lineNumber;
    }

    [System.Serializable]
    public class FileComment
    {
        public string filename;
        public string comment;
    }
}
