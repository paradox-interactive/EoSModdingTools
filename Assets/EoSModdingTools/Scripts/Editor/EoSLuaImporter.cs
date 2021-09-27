using System.IO;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;

#if !EXCLUDE_MODDING_SUPPORT
namespace RomeroGames
{
    [ScriptedImporter(1, "lua")]
    public class EoSLuaImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            TextAsset textAsset = new TextAsset(File.ReadAllText(ctx.assetPath));

            ctx.AddObjectToAsset("main obj", textAsset);
            ctx.SetMainObject(textAsset);
        }
    }
}
#endif
