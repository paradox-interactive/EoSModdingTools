using System.Threading.Tasks;
using UnityEngine;
using PDX;
using UnityEditor;

namespace RomeroGames
{
    public static class ParadoxModsUtils
    {
        private const string ParadoxNamespace  = "empire_of_sin";

        private static async Task<(SDK.Context, string)> InitContext(ModConfig modConfig)
        {
#if UNITY_EDITOR_WIN
            SDK.Platform pdxPlatform = SDK.Platform.Windows;
#elif UNITY_EDITOR_OSX
                SDK.Platform pdxPlatform = SDK.Platform.MacOS;
#else
                Debug.LogError("Unsupported build platform");
                return;
#endif
            SDK.Config config = new SDK.Config()
            {
                Environment = SDK.BackendEnvironment.Live,
            };

            SDK.Context _context = SDK.Context.Create(pdxPlatform, ParadoxNamespace, config);

            SDK.Credential.EmailAndPassword MailAndPassCred = new SDK.Credential.EmailAndPassword(
                email: modConfig.ParadoxEmail,
                password: modConfig.ParadoxPassword
            );

            // A Player can be logged in to their Paradox Account using an instance of Credential.EmailAndPassword.
            SDK.Account.Result.Login loginResult = await _context.Account.Login(MailAndPassCred);
            if (loginResult.Success)
            {
                return (_context, string.Empty);
            }

            string message = $"Paradox login failed.\n{loginResult.Error.Raw}";
            return (null, message);
        }

        public static async Task<(bool,string)> Upload(ModConfig modConfig, string modArchiveFile, string modPreviewFile)
        {
            SDK.Context context;
            string errorMessage;

            (context, errorMessage) = await InitContext(modConfig);
            if (context == null)
            {
                return (false, errorMessage);
            }

            bool success = false;
            errorMessage = string.Empty;

            if (modConfig.ParadoxModsId == 0)
            {
                Task<SDK.Mods.Result.Publish> publishOp = context.Mods.Publish(
                    displayName: modConfig.Title,
                    recommendedGameVersion: modConfig.GameVersion ?? string.Empty,
                    shortDescription: modConfig.ShortDescription,
                    longDescription: modConfig.LongDescription,
                    contentAbsolutePath: modArchiveFile,
                    os: "Any", // Todo: Seems to be the only supported tag?
                    tags: modConfig.Tags,
                    thumbnailAbsolutePath: modPreviewFile
                );
                SDK.Mods.Result.Publish publishResult = await publishOp;

                if (publishResult.Success)
                {
                    success = true;

                    // The call returns a new mod id for the published mod. This mod id will be needed to later update the published mod.
                    modConfig.ParadoxModsId = publishResult.ModId;
                    EditorUtility.SetDirty(modConfig);
                    AssetDatabase.SaveAssets();
                }
                else
                {
                    errorMessage = publishResult.Error.ToString();
                }
            }
            else
            {
                Task<SDK.Mods.Result.PublishUpdate> publishUpdateOp = context.Mods.PublishUpdate(
                    modId: modConfig.ParadoxModsId,
                    recommendedGameVersion: modConfig.GameVersion ?? string.Empty,
                    shortDescription: modConfig.ShortDescription,
                    longDescription: modConfig.LongDescription,
                    contentAbsolutePath: modArchiveFile,
                    os: "Any", // Todo: Seems to be the only supported value?
                    tags: modConfig.Tags,
                    thumbnailAbsolutePath: modPreviewFile,
                    changeLog: modConfig.ChangeNotes);
                SDK.Mods.Result.PublishUpdate publishResult = await publishUpdateOp;
                if (publishResult.Success)
                {
                    success = true;
                }
                else
                {
                    errorMessage = publishResult.Error.ToString();
                }
            }
            await context.Shutdown();

            if (!success)
            {
                return (false, $"Failed to publish to Paradox Mods. {errorMessage}");
            }

            return (true, string.Empty);
        }
    }
}