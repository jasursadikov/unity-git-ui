using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Abuksigun.PackageShortcuts
{
    public static class GitStaging
    {
        const int TopPanelHeight = 120;
        const int MiddlePanelWidth = 40;

        [MenuItem("Assets/Git Staging", true)]
        public static bool Check() => PackageShortcuts.GetSelectedGitModules().Any();

        [MenuItem("Assets/Git Staging", priority = 100)]
        public static async void Invoke()
        {
            string commitMessage = "";
            var modules = PackageShortcuts.GetSelectedGitModules().ToArray();
            var tasks = new Task<CommandResult>[modules.Length];
            var scrollPositions = new (Vector2 unstaged, Vector2 staged)[modules.Length];
            var selection = Enumerable.Repeat((unstaged:new List<string>(), staged: new List<string>()), modules.Length).ToArray();

            string[] moduleNames = modules.Select(x => x.ShortName).ToArray();
            int tab = 0;
            
            await GUIShortcuts.ShowModalWindow("Commit", new Vector2Int(600, 400), (window) => {
                
                GUILayout.Label("Commit message");
                commitMessage = GUILayout.TextArea(commitMessage, GUILayout.Height(40));
                
                int modulesWithStagedFiles = modules.Count(x => x.GitStatus.GetResultOrDefault()?.Staged?.Count() > 0);
                bool commitAvailable = modulesWithStagedFiles > 0 && !string.IsNullOrWhiteSpace(commitMessage) && !tasks.Any(x => x != null && !x.IsCompleted);

                using (new EditorGUI.DisabledGroupScope(!commitAvailable))
                using (new GUILayout.HorizontalScope())
                {
                    if (GUILayout.Button($"Commit {modulesWithStagedFiles}/{modules.Length} modules", GUILayout.Width(200)))
                    {
                        tasks = modules.Select(module => module.RunGit($"commit -m {commitMessage.WrapUp()}")).ToArray();
                        commitMessage = "";
                    }
                    if (GUILayout.Button($"Stash {modulesWithStagedFiles}/{modules.Length} modules", GUILayout.Width(200)))
                    {
                        tasks = modules.Select(module => {
                            var files = module.GitStatus.GetResultOrDefault().Files.Where(x => x.IsStaged).Select(x => x.FullPath);
                            return module.RunGit($"stash push -m {commitMessage.WrapUp()} -- {PackageShortcuts.JoinFileNames(files)}");
                        }).ToArray();
                        commitMessage = "";
                    }
                }

                tab = moduleNames.Length > 1 ? GUILayout.Toolbar(tab, moduleNames) : 0;
                var module = modules[tab];
                var task = tasks[tab];
                var unstagedSelection = selection[tab].unstaged;
                var stagedSelection = selection[tab].staged;

                GUILayout.Label($"{module.Name} [{module.CurrentBranch.GetResultOrDefault() ?? ".."}]");

                if (module.GitRepoPath.GetResultOrDefault() is { } gitRepoPath && module.GitStatus.GetResultOrDefault() is { } status)
                {
                    var scrollHeight = GUILayout.Height(window.position.height - TopPanelHeight);
                    var scrollWidth = GUILayout.Width((window.position.width - MiddlePanelWidth) / 2);

                    using (new EditorGUI.DisabledGroupScope(task != null && !task.IsCompleted))
                    using (new GUILayout.HorizontalScope())
                    {
                        GUIShortcuts.DrawList(gitRepoPath, status.Unstaged, unstagedSelection, ref scrollPositions[tab].unstaged, false, scrollHeight, scrollWidth);
                        using (new GUILayout.VerticalScope())
                        {
                            if (GUILayout.Button(">>", GUILayout.Width(MiddlePanelWidth)))
                            {
                                tasks[tab] = module.RunGit($"add -f -- {PackageShortcuts.JoinFileNames(unstagedSelection)}");
                                unstagedSelection.Clear();
                            }
                            if (GUILayout.Button("<<", GUILayout.Width(MiddlePanelWidth)))
                            {
                                tasks[tab] = module.RunGit($"reset -q -- {PackageShortcuts.JoinFileNames(stagedSelection)}");
                                stagedSelection.Clear();
                            }
                            if (GUILayout.Button("More", GUILayout.Width(MiddlePanelWidth)))
                            {
                                _ = ShowContextMenu(module, status.Files.Where(x => unstagedSelection.Contains(x.FullPath)|| stagedSelection.Contains(x.FullPath)));
                            }
                        }
                        GUIShortcuts.DrawList(module.GitRepoPath.Result, status.Staged, stagedSelection, ref scrollPositions[tab].staged, true, scrollHeight, scrollWidth);
                    }
                }
            });
            await Task.WhenAll(tasks.Where(x => x != null));
        }

        static async Task ShowContextMenu(Module module, IEnumerable<FileStatus> files)
        {
            if (!files.Any())
                return;
            Task task = null;
            GenericMenu menu = new GenericMenu();
            string filesList = PackageShortcuts.JoinFileNames(files.Select(x => x.FullPath));
            if (files.Any(x => x.IsInIndex))
            {
                menu.AddItem(new GUIContent("Diff"), false, () => task = Diff.ShowDiff(module, files.First().FullPath, files.First().IsStaged));
                menu.AddItem(new GUIContent("Discrad"), false, () => {
                    if (EditorUtility.DisplayDialog($"Are you sure you want DISCARD these files", filesList, "Yes", "No"))
                        task = module.RunGit($"checkout -- {filesList}");
                });
            }
            menu.AddItem(new GUIContent("Delete"), false, () => {
                if (EditorUtility.DisplayDialog($"Are you sure you want DELETE these files", filesList, "Yes", "No"))
                {
                    foreach (var file in files)
                        File.Delete(file.FullPath);
                }
            });
            menu.ShowAsContext();
            if (task != null)
                await task;
        }
    }
}
