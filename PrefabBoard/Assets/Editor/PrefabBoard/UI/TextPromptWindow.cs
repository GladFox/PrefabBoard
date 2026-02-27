using System;
using UnityEditor;
using UnityEngine;

namespace PrefabBoard.Editor.UI
{
    public sealed class TextPromptWindow : EditorWindow
    {
        private Action<string> _onConfirm;
        private string _label = "Name";
        private string _value = string.Empty;

        public static void Show(string title, string label, string initialValue, Action<string> onConfirm)
        {
            var window = CreateInstance<TextPromptWindow>();
            window.titleContent = new GUIContent(string.IsNullOrWhiteSpace(title) ? "Input" : title);
            window._label = string.IsNullOrWhiteSpace(label) ? "Name" : label;
            window._value = initialValue ?? string.Empty;
            window._onConfirm = onConfirm;
            window.minSize = new Vector2(320f, 92f);
            window.maxSize = new Vector2(720f, 92f);
            window.ShowUtility();
            window.Focus();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(6f);
            GUI.SetNextControlName("PromptTextField");
            _value = EditorGUILayout.TextField(_label, _value);
            EditorGUILayout.Space(10f);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Cancel", GUILayout.Height(22f)))
            {
                Close();
                return;
            }

            if (GUILayout.Button("OK", GUILayout.Height(22f)))
            {
                ConfirmAndClose();
                return;
            }
            EditorGUILayout.EndHorizontal();

            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
                {
                    ConfirmAndClose();
                    Event.current.Use();
                }
                else if (Event.current.keyCode == KeyCode.Escape)
                {
                    Close();
                    Event.current.Use();
                }
            }
        }

        private void OnFocus()
        {
            EditorGUI.FocusTextInControl("PromptTextField");
        }

        private void ConfirmAndClose()
        {
            _onConfirm?.Invoke(_value);
            Close();
        }
    }
}
