using System;
using UnityEditor;
using UnityEngine;

namespace RomeroGames
{
    public static class EoSCreateLuaFile
    {
        private const string CreateLuaFileItem = "Assets/Create/Modding Tools/Lua File";
        private const string NewLuaTemplate = "Assets/EoSModdingTools/Scripts/Editor/ScriptTemplates/new-lua-template.lua.txt";


        [MenuItem(CreateLuaFileItem, false)]
        public static void CreateLuaFile( )
        {
            // Get the current selected path
            string selectedPath =  AssetDatabase.GetAssetPath(Selection.activeObject);

            // If its empty then assume Assets
            if( selectedPath == "" )
            {
                selectedPath = "Assets";
            }

            selectedPath = EoSPathUtils.CleanPath( selectedPath );

            // Make sure the path ends in a directory
            int index = selectedPath.LastIndexOf( ".", StringComparison.Ordinal);
            if( index != -1 )
            {
                index = selectedPath.LastIndexOf( "/", StringComparison.Ordinal);
                selectedPath = selectedPath.Substring( 0, index );
            }

            var DoCreateScriptAsset = System.Type.GetType("UnityEditor.ProjectWindowCallback.DoCreateScriptAsset, UnityEditor");

            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0,
                ScriptableObject.CreateInstance( DoCreateScriptAsset ) as UnityEditor.ProjectWindowCallback.EndNameEditAction,
                selectedPath + "/NewScript.lua",
                null,
                NewLuaTemplate);
        }
    }
}