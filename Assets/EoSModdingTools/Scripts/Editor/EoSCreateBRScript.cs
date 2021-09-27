using System;
using UnityEditor;
using UnityEngine;

namespace RomeroGames
{
    public static class EoSCreateBRScript
    {
        private const string CreateBRScriptItem = "Assets/Create/Modding Tools/BR Script";
        private const string NewBRScriptTemplate = "Assets/EoSModdingTools/Scripts/Editor/ScriptTemplates/new-brscript-template.br.txt";

        [MenuItem(CreateBRScriptItem, false)]
        public static void CreateBRScript( )
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
                selectedPath + "/NewScript.br",
                null,
                NewBRScriptTemplate);
        }
    }
}