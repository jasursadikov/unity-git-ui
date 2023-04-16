using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.tvOS;
using static UnityEditor.ShaderData;

namespace Abuksigun.MRGitUI
{
    public static class GitSettings
    {
        [MenuItem("Assets/Git/Settings", true)]
        public static bool Check() => PackageShortcuts.GetSelectedGitModules().Any();
        [MenuItem("Assets/Git/Settings", priority = 100)]
        public static async void Invoke()
        {
            var window = EditorWindow.CreateInstance<GitSettingsWindow>();
            window.titleContent = new GUIContent("Git Settings");
            await GUIShortcuts.ShowModalWindow(window, new Vector2Int(500, 300));
        }
    }

    [System.Serializable]
    class EditableRecord
    {
        public EditableRecord() { NewlyAdded = true; }
        public EditableRecord(string Alias, string Url) { (this.Alias, this.Url, this.NewlyAdded) = (Alias, Url, false); }
        [field: SerializeField]
        public string Alias { get; set; } = "";
        [field: SerializeField]
        public string Url { get; set; } = "";
        [field: SerializeField]
        public bool NewlyAdded { get; set; }
    };

    class GitSettingsWindow : DefaultWindow
    {
        int lastHash = 0;
        ReorderableList list = null;
        string guid = null;
        [SerializeField]
        List<EditableRecord> editableRemotes;

        protected override void OnGUI()
        {
            var modules = PackageShortcuts.GetSelectedGitModules().ToList();
            var module = GUIShortcuts.ModuleGuidToolbar(modules, guid);
            guid = module?.Guid;
            if (module?.Remotes?.GetResultOrDefault() is { } remotes)
            {
                if (lastHash != remotes.GetHashCode())
                {
                    editableRemotes = remotes.Select(x => new EditableRecord(x.Alias, x.Url)).ToList();
                    var serializedObject = new SerializedObject(this);
                    var property = serializedObject.FindProperty(nameof(editableRemotes));
                    list = new ReorderableList(serializedObject, property, true, false, true, true)
                    {
                        drawElementCallback = (rect, index, isActive, isFocused) => DrawListItems(rect, index, isActive, isFocused, editableRemotes),
                        onAddCallback = (list) => {
                            list.serializedProperty.arraySize++;
                            editableRemotes.Add(new EditableRecord());
                        },
                        onRemoveCallback = (list) => {
                            list.serializedProperty.DeleteArrayElementAtIndex(list.index);
                            editableRemotes.RemoveAt(list.index);
                        },
                    };
                    lastHash = remotes.GetHashCode();
                }
                list?.DoLayoutList();
                if (GUILayout.Button("Save"))
                {
                    foreach (var remote in module.Remotes.GetResultOrDefault())
                        if (!editableRemotes.Any(x => x.Alias == remote.Alias))
                            module.RemoveRemote(remote.Alias);
                    foreach (var remote in editableRemotes)
                    {
                        if (remote.NewlyAdded)
                            module.AddRemote(remote.Alias, remote.Url);
                        else
                            module.SetRemoteUrl(remote.Alias, remote.Url);
                    }
                }
            }
            base.OnGUI();
        }

        static void DrawListItems(Rect rect, int index, bool isActive, bool isFocused, List<EditableRecord> remotes)
        {
            var remote = remotes[index];
            using (new GUILayout.HorizontalScope())
            {
                if (remote.NewlyAdded)
                    remote.Alias = GUI.TextField(rect.Resize(150, 23), remote.Alias);
                else
                    GUI.Label(rect.Resize(150, 23), remote.Alias);
                remote.Url = GUI.TextField(rect.Move(150, 0), remote.Url);
            }
        }
    }
}