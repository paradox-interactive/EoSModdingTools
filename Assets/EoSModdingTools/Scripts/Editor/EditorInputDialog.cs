using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RomeroGames
{
    public class EditorInputDialog : EditorWindow
    {
        private string _description;
        private string _inputText;
        private string _okButton;
        private string _cancelButton;
        private bool _shouldClose;
        private Action _onOKButton;

        private static Type[] GetAllDerivedTypes(AppDomain aAppDomain, Type aType)
        {
            List<Type> result = new List<Type>();
            Assembly[] assemblies = aAppDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                Type[] types = assembly.GetTypes();
                foreach (Type type in types)
                {
                    if (type.IsSubclassOf(aType))
                        result.Add(type);
                }
            }

            return result.ToArray();
        }

        private Rect GetEditorMainWindowPos()
        {
            Type containerWinType = GetAllDerivedTypes(AppDomain.CurrentDomain, typeof(ScriptableObject)).FirstOrDefault(t => t.Name == "ContainerWindow");
            if (containerWinType == null)
            {
                throw new MissingMemberException("Can't find internal type ContainerWindow. Maybe something has changed inside Unity");
            }

            FieldInfo showModeField = containerWinType.GetField("m_ShowMode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            PropertyInfo positionProperty = containerWinType.GetProperty("position", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (showModeField == null || positionProperty == null)
            {
                throw new MissingFieldException("Can't find internal fields 'm_ShowMode' or 'position'. Maybe something has changed inside Unity");
            }

            Object[] windows = Resources.FindObjectsOfTypeAll(containerWinType);
            foreach (UnityEngine.Object win in windows)
            {
                int showMode = (int)showModeField.GetValue(win);
                if (showMode == 4) // main window
                {
                    return (Rect)positionProperty.GetValue(win, null);
                }
            }
            throw new NotSupportedException("Can't find internal main window. Maybe something has changed inside Unity");
        }

        private void CenterOnMainWin()
        {
            Rect main = GetEditorMainWindowPos();
            Rect pos = position;
            float w = (main.width - pos.width)*0.5f;
            float h = (main.height - pos.height)*0.5f;
            pos.x = main.x + w;
            pos.y = main.y + h;
            position = pos;
        }

        private void OnGUI()
        {
            // Check if Esc/Return have been pressed
            Event e = Event.current;
            if (e.type == EventType.KeyDown)
            {
                switch (e.keyCode)
                {
                    // Escape pressed
                    case KeyCode.Escape:
                        _shouldClose = true;
                        break;

                    // Enter pressed
                    case KeyCode.Return:
                    case KeyCode.KeypadEnter:
                        _onOKButton?.Invoke();
                        _shouldClose = true;
                        break;
                }
            }

            if (_shouldClose)
            {
                Close();
            }

            // Draw our control
            Rect rect = EditorGUILayout.BeginVertical();

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField(_description);

            EditorGUILayout.Space(8);
            GUI.SetNextControlName("inText");
            _inputText = EditorGUILayout.TextField("", _inputText);
            GUI.FocusControl("inText"); // Focus text field
            EditorGUILayout.Space(12);

            // Draw OK / Cancel buttons
            Rect r = EditorGUILayout.GetControlRect();
            r.width /= 2;
            if (GUI.Button(r, _okButton))
            {
                _onOKButton?.Invoke();
                _shouldClose = true;
            }

            r.x += r.width;
            if (GUI.Button(r, _cancelButton))
            {
                _inputText = null; // Cancel - delete inputText
                _shouldClose = true;
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.EndVertical();

            // Force change size of the window
            if (rect.width != 0 && minSize != rect.size)
            {
                minSize = maxSize = rect.size;
            }
        }

        public static string Show(string title, string description, string inputText, string okButton = "OK", string cancelButton = "Cancel")
        {
            string ret = null;
            EditorInputDialog window = CreateInstance<EditorInputDialog>();
            window.titleContent = new GUIContent(title);
            window._description = description;
            window._inputText = inputText;
            window._okButton = okButton;
            window._cancelButton = cancelButton;
            window._onOKButton += () => ret = window._inputText;
            window.ShowModal();

            window.CenterOnMainWin();

            return ret;
        }
    }
}