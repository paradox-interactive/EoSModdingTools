//#define USE_PARADOX_SANDBOX
#define ENABLE_STEAM_WORKSHOP
#define ENABLE_PARADOX_MODS

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.Assertions;

namespace RomeroGames
{
    [CustomEditor(typeof(ModConfig))]
    public class ModConfigEditor : Editor
    {
        private const string WikiURL = "https://eos.paradoxwikis.com/Modding";

#if USE_PARADOX_SANDBOX
        private const string ParadoxModsURL = "https://sandbox-mods.paradoxplaza.com/games/renegade";
        private const string ParadoxModsItemURL = "https://sandbox-mods.paradoxplaza.com/mods/{0}/Any";
#else
        private const string ParadoxModsURL = "https://mods.paradoxplaza.com/games/renegade";
        private const string ParadoxModsItemURL = "https://mods.paradoxplaza.com/mods/{0}/Any";
#endif

        private const string SteamWorkshopURL = "https://steamcommunity.com/workshop/browse/?appid=604540";
        private const string SteamWorkshopItemURL = "https://steamcommunity.com/sharedfiles/filedetails/?id={0}";

        private const string ModdingTools_DescriptionFoldout = "EoSModdingTools.DescriptionFoldout";
        private const string ModdingTools_PreviewImageFoldout = "EoSModdingTools.PreviewImageFoldout";
        private const string ModdingTools_TagsFoldout = "EoSModdingTools.TagsFoldout";
        private const string ModdingTools_LocalFoldout = "EoSModdingTools.LocalFoldout";
        private const string ModdingTools_ParadoxFoldout = "EoSModdingTools.ParadoxFoldout";
        private const string ModdingTools_SteamFoldout = "EoSModdingTools.SteamFoldout";

        private readonly GUIContent _modIdContent = new GUIContent("Mod Id", "Id used to reference this mod. This is the same as the folder name.");
        private readonly GUIContent _viewDocsContent = new GUIContent("View Modding Documentation", "View the modding documentation wiki page");
        private readonly GUIContent _descriptionContent = new GUIContent("Description", "Describe your mod to help players understand what it does and what has changed in each update.");
        private readonly GUIContent _previewImageContent = new GUIContent("Preview Image", "Provide a preview image for this mod to display on Paradox Mods / Steamworks");
        private readonly GUIContent _selectPreviewImageContent = new GUIContent("Select Preview Image", "Select a preview image file");
        private readonly GUIContent _clearPreviewImageContent = new GUIContent("Clear Preview Image", "Clears the selected preview image");
        private readonly GUIContent _tagsContent = new GUIContent("Tags", "Tags help players find similar types of mod");
        private readonly GUIContent _localModsContent = new GUIContent("Local Mods", "Mods installed locally on your computer for testing");
        private readonly GUIContent _filePickerContent = new GUIContent("...", "Select the local mods folder in the game data path");
        private readonly GUIContent _publishLocalContent = new GUIContent("Publish", "Install the mod in the Empire of Sin mods folder on your computer for testing");
        private readonly GUIContent _viewLocalContent = new GUIContent("View local mods folder", "View the Empire of Sin mods folder on your computer");
        private readonly GUIContent _paradoxModsContent = new GUIContent("Paradox Mods", "Mods hosted on the Paradox Mods service");
        private readonly GUIContent _publishParadoxContent = new GUIContent("Publish", "Publish the mod on the Paradox Mods service");
        private readonly GUIContent _viewParadoxContent = new GUIContent("View on Paradox Mods", "View the published mod on the Paradox Mods website");
        private readonly GUIContent _steamWorkshopContent = new GUIContent("Steam Workshop", "Mods hosted on the Steam Workshop service");
        private readonly GUIContent _publishSteamWorkshopContent = new GUIContent("Publish", "Publish the mod on the Steam Workshop service");
        private readonly GUIContent _viewSteamWorkshopContent = new GUIContent("View on Steam Workshop", "View the published mod on Steam Workshop");

        private SerializedProperty _title;
        private SerializedProperty _shortDescription;
        private SerializedProperty _longDescription;
        private SerializedProperty _changeNotes;
        private SerializedProperty _gameVersion;
        private SerializedProperty _tags;
        private SerializedProperty _localModsPath;
        private SerializedProperty _paradoxModsId;
        private SerializedProperty _paradoxEmail;
        private SerializedProperty _paradoxPassword;
        private SerializedProperty _steamWorkshopId;

        private Texture2D _previewImageTexture;

        private static bool _descriptionFoldout
        {
            get
            {
                if (!EditorPrefs.HasKey(ModdingTools_DescriptionFoldout))
                {
                    // Expand description foldout by default
                    EditorPrefs.SetBool(ModdingTools_DescriptionFoldout, true);
                }
                return EditorPrefs.GetBool(ModdingTools_DescriptionFoldout);
            }
            set => EditorPrefs.SetBool(ModdingTools_DescriptionFoldout, value);
        }

        private static bool _previewImageFoldout
        {
            get
            {
                if (!EditorPrefs.HasKey(ModdingTools_PreviewImageFoldout))
                {
                    // Expand preview image foldout by default
                    EditorPrefs.SetBool(ModdingTools_PreviewImageFoldout, true);
                }
                return EditorPrefs.GetBool(ModdingTools_PreviewImageFoldout);
            }
            set => EditorPrefs.SetBool(ModdingTools_PreviewImageFoldout, value);
        }

        private static bool _tagsFoldout
        {
            get => EditorPrefs.GetBool(ModdingTools_TagsFoldout);
            set => EditorPrefs.SetBool(ModdingTools_TagsFoldout, value);
        }

        private static bool _localFoldout
        {
            get => EditorPrefs.GetBool(ModdingTools_LocalFoldout);
            set => EditorPrefs.SetBool(ModdingTools_LocalFoldout, value);
        }

        private static bool _paradoxFoldout
        {
            get => EditorPrefs.GetBool(ModdingTools_ParadoxFoldout);
            set => EditorPrefs.SetBool(ModdingTools_ParadoxFoldout, value);
        }

        private static bool _steamFoldout
        {
            get => EditorPrefs.GetBool(ModdingTools_SteamFoldout);
            set => EditorPrefs.SetBool(ModdingTools_SteamFoldout, value);
        }

        private GUIStyle _textStyle;

        private void OnEnable()
        {
            _title = serializedObject.FindProperty("Title");
            _shortDescription = serializedObject.FindProperty("ShortDescription");
            _longDescription = serializedObject.FindProperty("LongDescription");
            _changeNotes = serializedObject.FindProperty("ChangeNotes");
            _gameVersion = serializedObject.FindProperty("GameVersion");
            _tags = serializedObject.FindProperty("Tags");
            _localModsPath = serializedObject.FindProperty("LocalModsPath");
            _paradoxModsId = serializedObject.FindProperty("ParadoxModsId");
            _paradoxEmail = serializedObject.FindProperty("ParadoxEmail");
            _paradoxPassword = serializedObject.FindProperty("ParadoxPassword");
            _steamWorkshopId = serializedObject.FindProperty("SteamWorkshopId");

            UpdatePreviewImageTexture();

            ModConfig modConfig = target as ModConfig;
            Assert.IsNotNull(modConfig);
            modConfig.SetDefaultModsPath();
        }

        private void OnDisable()
        {
            EditorUtility.ClearProgressBar();
        }

        private void LoadPreviewImage(string path)
        {
            try
            {
                ModConfig modConfig = target as ModConfig;
                Assert.IsNotNull(modConfig);

                byte[] bytes = File.ReadAllBytes(path);

                if (bytes.Length > 1024 * 1024)
                {
                    string message = "Preview image must be less than 1mb";
                    Debug.LogError(message);
                    ModUtils.ShowEditorNotification(message);
                    return;
                }

                modConfig.PreviewImageData = Convert.ToBase64String(bytes);
                modConfig.PreviewImageFilename = Path.GetFileName(path);

                EditorUtility.SetDirty(modConfig);
                AssetDatabase.SaveAssets();

                UpdatePreviewImageTexture();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load preview image. {e.Message}");
            }
        }

        private void ClearPreviewImage()
        {
            ModConfig modConfig = target as ModConfig;
            Assert.IsNotNull(modConfig);

            modConfig.PreviewImageData = null;
            modConfig.PreviewImageFilename = null;

            EditorUtility.SetDirty(modConfig);
            AssetDatabase.SaveAssets();

            UpdatePreviewImageTexture();
        }

        private void UpdatePreviewImageTexture()
        {
            ModConfig modConfig = target as ModConfig;
            Assert.IsNotNull(modConfig);

            byte[] bytes = null;
            if (!string.IsNullOrEmpty(modConfig.PreviewImageData))
            {
                bytes = Convert.FromBase64String(modConfig.PreviewImageData);
            }

            if (bytes == null)
            {
                DestroyImmediate(_previewImageTexture);
                _previewImageTexture = null;
            }
            else
            {
                _previewImageTexture = new Texture2D(0,0);
                _previewImageTexture.LoadImage(bytes);
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (GUILayout.Button(_viewDocsContent))
            {
                Application.OpenURL(WikiURL);
            }

            EditorGUILayout.Space();

            ModConfig modConfig = target as ModConfig;
            Assert.IsNotNull(modConfig);

            EditorGUILayout.LabelField(_modIdContent, new GUIContent(modConfig.GetModName()));

            EditorGUILayout.PropertyField(_title);
            EditorGUILayout.PropertyField(_gameVersion);

            _descriptionFoldout = EditorGUILayout.Foldout(_descriptionFoldout, _descriptionContent);
            if (_descriptionFoldout)
            {
                EditorGUILayout.PropertyField(_shortDescription);
                EditorGUILayout.PropertyField(_longDescription);
                EditorGUILayout.PropertyField(_changeNotes);

                if (string.IsNullOrEmpty(_longDescription.stringValue) ||
                    string.IsNullOrEmpty(_changeNotes.stringValue) ||
                    string.IsNullOrEmpty(_shortDescription.stringValue))
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox("Please enter short description, long description and change notes for this mod.", MessageType.Warning);
                    EditorGUILayout.Space();
                }
            }

            _previewImageFoldout = EditorGUILayout.Foldout(_previewImageFoldout, _previewImageContent);
            if (_previewImageFoldout)
            {
                if (_previewImageTexture == null)
                {
                    EditorGUILayout.HelpBox("Choose a preview image to display for this mod on Paradox Mods / Steam Workshop. Recommended dimensions are 1024x768. Image must be .jpg and less than 1mb", MessageType.Warning);
                }
                else
                {
                    Rect r = GUILayoutUtility.GetRect(200, 100);
                    EditorGUI.DrawPreviewTexture(r, _previewImageTexture, null, ScaleMode.ScaleToFit);
                    EditorGUILayout.LabelField(modConfig.PreviewImageFilename, EditorStyles.miniLabel);
                }

                EditorGUILayout.Space();

                if (GUILayout.Button(_selectPreviewImageContent))
                {
                    string path = EditorUtility.OpenFilePanel("Select preview image", "", "jpg,jpeg");
                    if (path.Length != 0)
                    {
                        LoadPreviewImage(path);
                    }
                }

                if (GUILayout.Button(_clearPreviewImageContent))
                {
                    ClearPreviewImage();
                }
            }

            _tagsFoldout = EditorGUILayout.Foldout(_tagsFoldout, _tagsContent);
            if (_tagsFoldout)
            {
                EditorGUILayout.HelpBox("Select applicable tags to help players find your mod. Do not select tags that are not relevant to your mod.", MessageType.Info);

                List<string> tags = modConfig.Tags;
                bool changed = false;

                // Remove any tags that are now invalid
                foreach (string tag in tags)
                {
                    if (!ModConfig.ValidTags.Contains(tag))
                    {
                        tags.Remove(tag);
                    }
                }

                // Display a toggle control for each possible tag
                foreach (string tag in ModConfig.ValidTags)
                {
                    bool active = tags.Contains(tag);
                    bool newActive = EditorGUILayout.Toggle(tag, active);

                    if (newActive != active)
                    {
                        changed = true;
                        if (newActive)
                        {
                            tags.Add(tag);
                        }
                        else
                        {
                            tags.Remove(tag);
                        }
                    }
                }

                if (changed)
                {
                    tags.Sort();
                }
            }

            EditorGUILayout.Space();

            if (_textStyle == null)
            {
                _textStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Publish Mod", _textStyle);

            _localFoldout = EditorGUILayout.Foldout(_localFoldout, _localModsContent);
            if (_localFoldout)
            {
                EditorGUILayout.HelpBox("Publish the mod to the Empire of Sin mods folder for testing on this computer. The mod will then be available for selection in Paradox Launcher when the game starts.", MessageType.Info);

                GUILayout.BeginVertical("window");

                GUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(_localModsPath);

                if (GUILayout.Button(_filePickerContent, GUILayout.Width(40)))
                {
                    string path = EditorUtility.OpenFolderPanel("Select local mods folder", "", "");
                    if (path.Length != 0)
                    {
                        _localModsPath.stringValue = path;
                    }
                }

                GUILayout.EndHorizontal();

                bool canPublish = ModUtils.ValidateConfig(ModHostType.Local, modConfig, out string validationErrorMessage);
                using (new EditorGUI.DisabledScope(!canPublish && !ModUtils.IsPublishing))
                {
                    if (GUILayout.Button(_publishLocalContent))
                    {
                        string message;
                        ModUtils.PublishMod(ModHostType.Local, modConfig, (result, errorMessage) =>
                        {
                            if (result)
                            {
                                message = $"Published '{modConfig.GetModName()}' to local";
                                Debug.Log(message);
                                ModUtils.ShowEditorNotification(message);
                            }
                            else
                            {
                                message = $"Failed to publish '{modConfig.GetModName()}' to local.";
                                Debug.LogError($"{message}. {errorMessage}");
                                ModUtils.ShowEditorPopup(message, errorMessage);
                            }
                        });
                    }
                }
                if (!canPublish)
                {
                    EditorGUILayout.HelpBox($"Please fix the following issues to enable publishing:\n{validationErrorMessage}", MessageType.Warning);
                }

                if (GUILayout.Button(_viewLocalContent))
                {
                    bool success = false;
                    string modFile = EoSPathUtils.CombinePath(_localModsPath.stringValue, modConfig.GetModName() + ".zip");
                    if (File.Exists(modFile))
                    {
                        EditorUtility.RevealInFinder(modFile);
                        success = true;
                    }
                    else
                    {
                        string modsFolder = _localModsPath.stringValue;
                        if (Directory.Exists(modsFolder))
                        {
                            EditorUtility.RevealInFinder(modsFolder);
                            success = true;
                        }
                    }

                    if (!success)
                    {
                        Debug.LogError($"Local publish path not found: {_localModsPath.stringValue}");
                    }
                }

                GUILayout.EndVertical();
            }

#if ENABLE_PARADOX_MODS
            _paradoxFoldout = EditorGUILayout.Foldout(_paradoxFoldout, _paradoxModsContent);
            if (_paradoxFoldout)
            {
                EditorGUILayout.HelpBox("Publish the mod to the Paradox Mods service.", MessageType.Info);

                GUILayout.BeginVertical("window");
                EditorGUILayout.PropertyField(_paradoxEmail);
                _paradoxPassword.stringValue = EditorGUILayout.PasswordField("Paradox Password", _paradoxPassword.stringValue);
                EditorGUILayout.PropertyField(_paradoxModsId);

                bool canPublish = ModUtils.ValidateConfig(ModHostType.ParadoxMods, modConfig, out string validationErrorMessage);
                using (new EditorGUI.DisabledScope(!canPublish && !ModUtils.IsPublishing))
                {
                    if (GUILayout.Button(_publishParadoxContent))
                    {
                        string message;
                        ModUtils.PublishMod(ModHostType.ParadoxMods, modConfig, (result, errorMessage) =>
                        {
                            if (result)
                            {
                                message = $"Published '{modConfig.GetModName()}' to Paradox Mods";
                                Debug.Log(message);
                                ModUtils.ShowEditorNotification(message);
                            }
                            else
                            {
                                message = $"Failed to publish '{modConfig.GetModName()}' to Paradox Mods";
                                Debug.LogError($"{message}. {errorMessage}");
                                ModUtils.ShowEditorPopup(message, errorMessage);
                            }
                        });
                    }
                }
                if (!canPublish)
                {
                    EditorGUILayout.HelpBox($"Please fix the following issues to enable publishing:\n{validationErrorMessage}", MessageType.Warning);
                }

                if (GUILayout.Button(_viewParadoxContent))
                {
                    if (modConfig.ParadoxModsId == 0)
                    {
                        Application.OpenURL(ParadoxModsURL);
                    }
                    else
                    {
                        string url = string.Format(ParadoxModsItemURL, modConfig.ParadoxModsId);
                        Application.OpenURL(url);
                    }
                }

                GUILayout.EndVertical();
            }
#endif

#if ENABLE_STEAM_WORKSHOP
            _steamFoldout = EditorGUILayout.Foldout(_steamFoldout, _steamWorkshopContent);
            if (_steamFoldout)
            {
                EditorGUILayout.HelpBox("Publish the mod to the Steam Workshop service", MessageType.Info);

                GUILayout.BeginVertical("window");
                EditorGUILayout.PropertyField(_steamWorkshopId);

                bool canPublish = ModUtils.ValidateConfig(ModHostType.SteamWorkshop, modConfig, out string validationErrorMessage);
                using (new EditorGUI.DisabledScope(!canPublish && !ModUtils.IsPublishing))
                {
                    if (GUILayout.Button(_publishSteamWorkshopContent))
                    {
                        string message;
                        ModUtils.PublishMod(ModHostType.SteamWorkshop, modConfig, (result, errorMessage) =>
                        {
                            if (result)
                            {
                                message = $"Published '{modConfig.GetModName()}' to Steam Workshop";
                                Debug.Log(message);
                                ModUtils.ShowEditorNotification(message);
                            }
                            else
                            {
                                message = $"Failed to publish '{modConfig.GetModName()}' to Steam Workshop";
                                Debug.LogError($"{message}. {errorMessage}");
                                ModUtils.ShowEditorPopup(message, errorMessage);
                            }
                        });
                    }
                }
                if (!canPublish)
                {
                    EditorGUILayout.HelpBox($"Please fix the following issues to enable publishing:\n{validationErrorMessage}", MessageType.Warning);
                }

                if (GUILayout.Button(_viewSteamWorkshopContent))
                {
                    if (modConfig.SteamWorkshopId == 0)
                    {
                        Application.OpenURL(SteamWorkshopURL);
                    }
                    else
                    {
                        string url = string.Format(SteamWorkshopItemURL, modConfig.SteamWorkshopId);
                        Application.OpenURL(url);
                    }
                }

                // if (GUILayout.Button("List Steam Mods"))
                // {
                //     SteamWorkshopUtils.ListSteamMods();
                // }

                if (SteamWorkshopUtils.ShowSteamWarning)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox(
                        "The Steam Client may now appear to show Empire of Sin as currently playing. This is due to an issue in the Steam Client that we have no control over. Restarting Unity should reset it.",
                        MessageType.Warning);
                }
                GUILayout.EndVertical();
            }
#endif

            if (SteamWorkshopUtils.IsUploading)
            {
                EditorUtility.DisplayProgressBar("Publishing Mod", $"Publishing {modConfig.GetModName()} to Steam Workshop", SteamWorkshopUtils.UploadProgress);
            }
            else
            {
                EditorUtility.ClearProgressBar();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}

