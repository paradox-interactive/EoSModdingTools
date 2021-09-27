using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Steamworks;
using Steamworks.Ugc;
using Editor = Steamworks.Ugc.Editor;

namespace RomeroGames
{
    public static class SteamWorkshopUtils
    {
        private const uint SteamAppId = 604540;

        public static bool RequestShutdown { get; set; }

        public static bool ShowSteamWarning { get; private set; }

        public static bool IsUploading;
        public static float UploadProgress;

        private class ProgressClass :  IProgress<float>
        {
            private float _lastValue = 0;

            public ProgressClass()
            {
                UploadProgress = 0;
            }

            public void Report( float value )
            {
                if (_lastValue >= value)
                {
                    return;
                }

                _lastValue = value;
                UploadProgress = value;
            }
        }

        private static void OnEditorUpdate()
        {
            SteamClient.RunCallbacks();

            if (RequestShutdown && !IsUploading)
            {
                ShutdownSteam();
                RequestShutdown = false;
                ShowSteamWarning = true;
            }
        }

        public static bool InitSteam()
        {
            if (!SteamClient.IsValid)
            {
                try
                {
                    SteamClient.Init( SteamAppId );
                    EditorApplication.update -= OnEditorUpdate;
                    EditorApplication.update += OnEditorUpdate;
                }
                catch (Exception e )
                {
                    //     Steam is closed?
                    //     Can't find steam_api dll?
                    //     Don't have permission to play app?
                    Debug.LogError(e.Message);
                    return false;
                }
            }

            return true;
        }

        private static void ShutdownSteam()
        {
            try
            {
                EditorApplication.update -= OnEditorUpdate;
                SteamClient.Shutdown();
            }
            catch (Exception e )
            {
                Debug.LogError(e.Message);
            }
        }

        public static async Task<(bool,string)> Upload(ModConfig modConfig, string modArchiveFile, string modPreviewFile)
        {
            IsUploading = true;
            PublishResult result;

            try
            {
                modArchiveFile = System.IO.Path.GetFullPath(modArchiveFile);
                modPreviewFile = System.IO.Path.GetFullPath(modPreviewFile);

                // If the SteamId is 0 then upload this as a new mod and record the new file id assigned by Steam so we
                // can update the same item on Steam the next time we publish.
                Editor uploadOp = modConfig.SteamWorkshopId == 0 ? Editor.NewCommunityFile : new Editor(modConfig.SteamWorkshopId);

                uploadOp.WithTitle( modConfig.Title );

                if (!string.IsNullOrEmpty(modConfig.LongDescription))
                {
                    uploadOp.WithDescription( modConfig.LongDescription );
                }

                uploadOp.WithContent(modArchiveFile);

                if (string.IsNullOrEmpty(modPreviewFile) ||
                    !System.IO.File.Exists(modPreviewFile))
                {
                    Debug.LogWarning("No preview image found for mod. Please add a Preview.png file in the mod folder.");
                }
                else
                {
                    uploadOp.WithPreviewFile(modPreviewFile);
                }

                foreach (string tag in modConfig.Tags)
                {
                    uploadOp.WithTag(tag);
                }
                uploadOp.WithChangeLog(modConfig.ChangeNotes);

                result = await uploadOp.SubmitAsync( new ProgressClass() );
            }
            catch (Exception e)
            {
                IsUploading = false;
                UploadProgress = 0;
                return (false, $"Steam upload failed. {e.Message}");
            }

            IsUploading = false;
            UploadProgress = 0;

            if (result.Success)
            {
                ulong steamId = result.FileId;

                if (modConfig.SteamWorkshopId != steamId)
                {
                    modConfig.SteamWorkshopId = steamId;
                    EditorUtility.SetDirty(modConfig);
                    AssetDatabase.SaveAssets();
                }
            }
            else
            {
                return (false, $"Steam upload failed {result.Result}");
            }

            return (true, string.Empty);
        }

        public static async void ListSteamMods()
        {
            if (!InitSteam())
            {
                return;
            }

            Query query = Query.All;

            ResultPage? page = await query.GetPageAsync(1);
            if (page.HasValue)
            {
                Debug.Log($"Page {1} has {page.Value.ResultCount} items");

                int itemIndex = 0;
                foreach (Item entry in page.Value.Entries)
                {
                    Debug.Log( $"Entry {itemIndex++}: {entry.Title}" );
                }
            }

            RequestShutdown = true;
        }
    }
}
