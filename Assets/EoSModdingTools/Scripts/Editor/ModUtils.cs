using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using UnityEngine.Assertions;

namespace RomeroGames
{
    public enum ModHostType
    {
        Local,
        SteamWorkshop,
        ParadoxMods
    }

    public static class ModUtils
    {
        public const string ModsFolder = "Assets/Mods";

        private static void CreateZip(string modPath, List<string> InputFiles, ModDescription modDescription, string OutputFilePath, int CompressionLevel = 9)
        {
            Assert.IsNotNull(modDescription);

            using (ZipOutputStream OutputStream = new ZipOutputStream(File.Create(OutputFilePath)))
            {
                OutputStream.SetLevel(CompressionLevel);
                byte[] buffer = new byte[4096];

                DateTime now = DateTime.Now;
                ZipEntry modDescriptionEntry = new ZipEntry(ModDescription.ModDescriptionFile)
                {
                    DateTime = now
                };

                OutputStream.PutNextEntry(modDescriptionEntry);
                string modDescriptionJSON = JsonUtility.ToJson(modDescription, true);
                byte[] bytes = Encoding.UTF8.GetBytes(modDescriptionJSON);
                OutputStream.Write(bytes, 0, bytes.Length);

                foreach (string file in InputFiles)
                {
                    string fileInZip = EoSPathUtils.CleanPath(file.Replace(modPath, "Raw~/"));

                    ZipEntry entry = new ZipEntry(fileInZip)
                    {
                        DateTime = now
                    };

                    OutputStream.PutNextEntry(entry);

                    using (FileStream fs = File.OpenRead(file))
                    {
                        int sourceBytes;
                        do
                        {
                            sourceBytes = fs.Read(buffer, 0, buffer.Length);
                            OutputStream.Write(buffer, 0, sourceBytes);
                        } while (sourceBytes > 0);
                    }
                }

                OutputStream.Finish();
                OutputStream.Close();
            }
        }

        private static bool BuildArchive(ModConfig modConfig, out string modArchiveFile, out string archiveErrorMessage)
        {
            string modPath = modConfig.GetModPath();
            string modName = modConfig.GetModName();

            List<string> inputFiles = new List<string>();

            string[] assets = AssetDatabase.FindAssets("", new [] { modPath } );
            foreach (string asset in assets)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(asset);
                string extension = Path.GetExtension(assetPath);
                switch (extension)
                {
                    case ".lua":
                    case ".json":
                        inputFiles.Add(assetPath);
                        break;
                }
            }

            if (inputFiles.Count == 0)
            {
                modArchiveFile = string.Empty;
                archiveErrorMessage = "Failed to build mod archive. Mod does not contain any files";
                return false;
            }

            modPath += Path.DirectorySeparatorChar;
            modPath = EoSPathUtils.CleanPath(modPath).Replace("//", "/");

            string tempPath = $"Temp/{modName}.zip";
            ModDescription modDescription = new ModDescription
            {
                ModName = modName,
                Title = modConfig.Title,
                GameVersion = modConfig.GameVersion,
                SteamWorkshopId = modConfig.SteamWorkshopId,
                ParadoxModsId = modConfig.ParadoxModsId,
            };

            try
            {
                CreateZip(modPath, inputFiles, modDescription, tempPath);
            }
            catch (Exception ex)
            {
                modArchiveFile = string.Empty;
                archiveErrorMessage = $"Failed to build mod archive. {ex.Message}";
                return false;
            }

            modArchiveFile = tempPath;
            archiveErrorMessage = string.Empty;
            return true;
        }

        private static bool BuildPreviewImage(ModConfig modConfig, out string previewImageFile, out string errorMessage)
        {
            previewImageFile = string.Empty;
            errorMessage = String.Empty;

            string previewImageData = modConfig.PreviewImageData;
            if (string.IsNullOrEmpty(previewImageData) ||
                string.IsNullOrEmpty(modConfig.PreviewImageFilename))
            {
                // Expand the preview image section
                EditorPrefs.SetBool("EoSModdingTools.PreviewImageFoldout", true);

                errorMessage = "Failed to write preview image. No preview image selected.";
                return false;
            }

            previewImageFile = EoSPathUtils.CombinePath("Temp", modConfig.PreviewImageFilename);
            try
            {
                FileUtil.DeleteFileOrDirectory(previewImageFile);
                File.WriteAllBytes(previewImageFile, Convert.FromBase64String(previewImageData));
            }
            catch (Exception e)
            {
                errorMessage = $"Failed to write preview image: {previewImageFile}. {e.Message}";
                return false;
            }

            return true;
        }

        public static string CleanPath(string path)
        {
            return path.Replace("\\", "/").Replace("//", "/");
        }

        public static bool ValidateConfig(ModHostType modHostType, ModConfig modConfig, out string validationErrorMessage)
        {
            bool isValid = true;
            StringBuilder sb = new StringBuilder();

            if (modHostType == ModHostType.Local)
            {
                if (string.IsNullOrEmpty(modConfig.LocalModsPath) ||
                    !Directory.Exists(modConfig.LocalModsPath))
                {
                    sb.AppendLine("Local mods path does not exist");
                    isValid = false;
                }
            }

            if (modHostType == ModHostType.SteamWorkshop ||
                modHostType == ModHostType.ParadoxMods)
            {
                if (string.IsNullOrEmpty(modConfig.Title))
                {
                    sb.AppendLine("Title is empty");
                    isValid = false;
                }

                if (string.IsNullOrEmpty(modConfig.ShortDescription))
                {
                    sb.AppendLine("Short Description is empty");
                    isValid = false;
                }

                if (string.IsNullOrEmpty(modConfig.LongDescription))
                {
                    sb.AppendLine("Long Description is empty");
                    isValid = false;
                }

                if (string.IsNullOrEmpty(modConfig.ChangeNotes))
                {
                    sb.AppendLine("Change Notes is empty");
                    isValid = false;
                }

                if (string.IsNullOrEmpty(modConfig.PreviewImageData) ||
                    string.IsNullOrEmpty(modConfig.PreviewImageFilename))
                {
                    sb.AppendLine("No Preview Image selected");
                    isValid = false;
                }
            }

            if (modHostType == ModHostType.ParadoxMods &&
                (string.IsNullOrEmpty(modConfig.ParadoxEmail) || string.IsNullOrEmpty(modConfig.ParadoxPassword)))
            {
                sb.AppendLine("Paradox user account info is empty");
                isValid = false;
            }

            if (!string.IsNullOrEmpty(modConfig.GameVersion))
            {
                string[] versionParts = modConfig.GameVersion.Split('.');

                bool validGameVersion = true;
                if (versionParts.Length != 3)
                {
                    validGameVersion = false;
                }
                else
                {
                    foreach (string part in versionParts)
                    {
                        if (part == "*")
                        {
                            continue;
                        }

                        foreach (char c in part)
                        {
                            if (c < '0' || c > '9')
                            {
                                validGameVersion = false;
                                break;
                            }
                        }

                        if (!int.TryParse(part, out int partNumber))
                        {
                            validGameVersion = false;
                            break;
                        }
                    }
                }

                if (!validGameVersion)
                {
                    sb.AppendLine("Game Version format is invalid.");
                    isValid = false;
                }
            }

            validationErrorMessage = sb.ToString();

            return isValid;
        }

        private static async Task PublishModInternal(ModHostType modHostType, ModConfig modConfig, Action<bool, string> onComplete)
        {
            Assert.IsNotNull(modConfig);

            bool success;
            string errorMessage = string.Empty;

            try
            {
                if (!ValidateConfig(modHostType, modConfig, out string validationErrorMessage))
                {
                    onComplete(false, validationErrorMessage);
                    return;
                }

                string modName = modConfig.GetModName();
                BuildModResources(modName);

                if (!BuildArchive(modConfig, out string modArchiveFile, out string archiveErrorMessage))
                {
                    onComplete(false, archiveErrorMessage);
                    return;
                }

                switch (modHostType)
                {
                    case ModHostType.SteamWorkshop:
                    {
                        if (!SteamWorkshopUtils.InitSteam())
                        {
                            onComplete(false, "Failed to initialize Steam");
                            return;
                        }

                        if (!BuildPreviewImage(modConfig, out string previewImageFile, out string previewErrorMessage))
                        {
                            onComplete(false, previewErrorMessage);
                            return;
                        }

                        (success, errorMessage) = await SteamWorkshopUtils.Upload(modConfig, modArchiveFile, previewImageFile);

                        SteamWorkshopUtils.RequestShutdown = true;
                    }
                        break;

                    case ModHostType.ParadoxMods:
                    {
                        if (!BuildPreviewImage(modConfig, out string previewImageFile, out string previewErrorMessage))
                        {
                            onComplete(false, previewErrorMessage);
                            return;
                        }

                        (success, errorMessage) = await ParadoxModsUtils.Upload(modConfig, modArchiveFile, previewImageFile);
                    }
                        break;

                    default:
                        string outputPath = CleanPath($"{modConfig.LocalModsPath}/{modName}.zip");
                        FileUtil.DeleteFileOrDirectory(outputPath);
                        FileUtil.MoveFileOrDirectory(modArchiveFile, outputPath);
                        success = true;
                        break;
                }
            }
            catch (Exception e)
            {
                onComplete(false, $"Upload failed: {e.Message}");
                return;
            }

            onComplete(success, errorMessage);
        }

        public static bool IsPublishing;

        public static async void PublishMod(ModHostType modHostType, ModConfig modConfig, Action<bool, string> onComplete)
        {
            if (IsPublishing)
            {
                onComplete(false, "Failed to publish because a publish is currently in progress");
                return;
            }

            IsPublishing = true;
            await PublishModInternal(modHostType, modConfig, onComplete);
            IsPublishing = false;
        }

        public static void BuildModResources(string modName)
        {
            List<string> modNames = new List<string>{ modName };

            BRScript.CleanScripts(modNames);
            BRScript.GenerateScripts(modNames, false);
            LocalizationProcessor.GenerateLocalizationFiles(modNames, false, null);
        }

        public static string GetModPath(string modName)
        {
            return string.Format("{0}/{1}", ModsFolder, modName);
        }

        public static string GetStringFilePath(string modName)
        {
            return string.Format("{0}/{1}/Localization/{1}_en.json", ModsFolder, modName);
        }

        public static List<string> GetModNames()
        {
            List<string> modNames = new List<string>();
            var modFolders = EoSPathUtils.GetSubFolders(ModsFolder);
            foreach (string modFolder in modFolders)
            {
                string modName = Path.GetFileName(modFolder);
                modNames.Add(modName);
            }

            return modNames;
        }

        public static void ShowEditorNotification(string notificationText)
        {
            EditorWindow view = EditorWindow.GetWindow<SceneView>();
            view.ShowNotification(new GUIContent(notificationText));
            view.Repaint();
        }

        public static void ShowEditorPopup(string title, string message)
        {
            EditorUtility.DisplayDialog(title, message, "Ok");
        }

#if EXCLUDE_MODDING_SUPPORT
        public static void ExportModdingTools()
        {
            const string ModdingToolsVersion = "1.0.1";

            string exportPath = EditorUtility.SaveFilePanel(
                "Export Modding Tools",
                "",
                $"EoSModdingTools_{ModdingToolsVersion}",
                "unitypackage");

            if (string.IsNullOrEmpty(exportPath))
            {
                return;
            }

            try
            {
                AssetDatabase.ExportPackage("Assets/EoSModdingTools", exportPath, ExportPackageOptions.Recurse);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to export EoSModdingTools. {ex}");
                return;
            }

            Debug.Log($"Exported EoSModdingTools: {exportPath}");
        }

        public static bool ExportGameSource(string archiveFile)
        {
            Dictionary<string, string> archiveFiles = new Dictionary<string, string>();

            string[] guids = AssetDatabase.FindAssets("*", new []
            {
                "Assets/Mods/GameData/Lua",
                "Assets/Mods/GameData/BRScript",
                "Assets/Mods/GameData/Localization",
                "Assets/Mods/DLC4/Lua",
                "Assets/Mods/DLC4/BRScript",
                "Assets/Mods/DLC4/Localization",
                "Assets/Mods/DLC5/Lua",
                "Assets/Mods/DLC5/BRScript",
                "Assets/Mods/DLC5/Localization"
            });

            foreach (string guid in guids)
            {
                string inputPath = AssetDatabase.GUIDToAssetPath(guid);
                if (inputPath.Contains("/Lua/BR/"))
                {
                    continue;
                }

                if (inputPath.Contains("/Localization/") &&
                    Path.GetExtension(inputPath) != ".json")
                {
                    continue;
                }

                if (AssetDatabase.IsValidFolder(inputPath))
                {
                    continue;
                }

                string outputPath;
                if (inputPath.StartsWith("Assets/Mods/"))
                {
                    outputPath = inputPath.Replace("Assets/Mods/", "");
                }
                else
                {
                    outputPath = inputPath;
                }

                inputPath = EoSPathUtils.CleanPath(inputPath);
                outputPath = EoSPathUtils.CleanPath(outputPath);

                archiveFiles[inputPath] = outputPath;
            }

            archiveFiles["Assets/EoSModdingTools/License.txt"] = "License.txt";

            string readmeText = @"This archive contains all the Lua source code files for Empire of Sin. 
These files can be used as a reference for modding the game.
For more information on modding Empire of Sin please visit: https://eos.paradoxwikis.com/Empire_of_Sin_Wiki";

            try
            {
                using (ZipOutputStream OutputStream = new ZipOutputStream(File.Create(archiveFile)))
                {
                    OutputStream.SetLevel(9);
                    byte[] buffer = new byte[4096];

                    DateTime now = DateTime.Now;

                    ZipEntry readmeEntry = new ZipEntry("Readme.txt")
                    {
                        DateTime = now
                    };
                    OutputStream.PutNextEntry(readmeEntry);
                    var bytes = Encoding.UTF8.GetBytes(readmeText);
                    OutputStream.Write(bytes, 0, bytes.Length);

                    foreach (var kv in archiveFiles)
                    {
                        string inputFile = kv.Key;
                        string outputFile = kv.Value;

                        ZipEntry entry = new ZipEntry(outputFile)
                        {
                            DateTime = now
                        };

                        OutputStream.PutNextEntry(entry);

                        using (FileStream fs = File.OpenRead(inputFile))
                        {
                            int sourceBytes;
                            do
                            {
                                sourceBytes = fs.Read(buffer, 0, buffer.Length);
                                OutputStream.Write(buffer, 0, sourceBytes);
                            } while (sourceBytes > 0);
                        }
                    }

                    OutputStream.Finish();
                    OutputStream.Close();
                }
            }
            catch (Exception ex)
            {
                // No need to rethrow the exception as for our purposes its handled.
                Console.WriteLine("Exception during processing {0}", ex);
                return false;
            }

            Debug.Log($"Exported game source archive: {archiveFile}");
            return true;
        }
#endif
    }
}
