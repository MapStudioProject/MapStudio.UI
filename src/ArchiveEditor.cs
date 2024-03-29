﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Toolbox.Core;
using Toolbox.Core.ViewModels;
using ImGuiNET;

namespace MapStudio.UI
{
    public class ArchiveEditor
    {
        public static void Load(IArchiveFile file, NodeBase root)
        {
            CreateObjectHiearchy(root, file);
        }

        public static void ReloadTree(IArchiveFile file, NodeBase root)
        {
             foreach (var node in root.Children)
            {
                if (node is FileNode)
                    ReloadFileNode(file, node as FileNode);
                else
                    ReloadTree(file, node);
            }
        }

        static void ReloadFileNode(IArchiveFile file, FileNode node)
        {
            foreach (var f in file.Files)
            {
                if (f.FileName == node.FullPath)
                    node.Reload(f);
            }
        }

        static NodeBase CreateObjectHiearchy(NodeBase parent, IArchiveFile archiveFile)
        {
            // build a TreeNode collection from the file list
            foreach (var file in archiveFile.Files)
            {
                string[] paths = file.FileName.Split('/');
                ProcessTree(parent, file, paths, 0);
            }
            return parent;
        }

        static void ProcessTree(NodeBase parent, ArchiveFileInfo file, string[] paths, int index)
        {
            string currentPath = paths[index];
            if (paths.Length - 1 == index)
            {
                var fileNode = new FileNode(currentPath, file);
                string ext = Utils.GetExtension(currentPath);
                if (string.IsNullOrEmpty(Path.GetExtension(file.FileName)))
                    fileNode.Header += ".bin";

                parent.AddChild(fileNode);
                return;
            }

            var node = FindFolderNode(parent, currentPath);
            if (node == null)
            {
                node = new FolderNode(currentPath);
                node.Icon = "Folder";
                parent.AddChild(node);
            }

            ProcessTree(node, file, paths, index + 1);
        }

        private static NodeBase FindFolderNode(NodeBase parent, string path)
        {
            NodeBase node = null;
            foreach (var child in parent.Children.ToArray())
            {
                if (child.Header.Equals(path))
                {
                    node = child;
                    break;
                }
            }

            return node;
        }

        class FolderNode : NodeBase
        {
            public FolderNode(string name) : base(name)
            {
                Icon = IconManager.FOLDER_ICON.ToString();
            }
        }

        class FileNode : NodeBase, IPropertyUI
        {
            /// <summary>
            /// The attached file information from an archive file.
            /// </summary>
            private ArchiveFileInfo FileInfo;

            public string FullPath => FileInfo.FileName;

            public FileNode(string name, ArchiveFileInfo fileInfo) : base(name)
            {
                CanRename = fileInfo.ParentArchiveFile.CanRenameFiles;
                FileInfo = fileInfo;
                Tag = fileInfo;
                Icon = IconManager.FILE_ICON.ToString();
                if (!string.IsNullOrEmpty(FileInfo.Icon))
                    Icon = FileInfo.Icon;

                IconColor = FileInfo.IconColor;

                if (name.EndsWith(".byaml") || name.EndsWith(".byml"))
                    IconColor = new System.Numerics.Vector4(0.564f, 0.792f, 0.97f, 1);

                if (name.EndsWith(".bcmdl") || name.EndsWith(".bch"))
                {
                    IconColor = new System.Numerics.Vector4(1, 0.5f, 0, 1);
                    Icon = '\uf1b2'.ToString();
                }

                ContextMenus.Add(new MenuItemModel("Rename", () => {
                    ActivateRename = true;
                })
                { IsEnabled = fileInfo.ParentArchiveFile.CanRenameFiles });

                ContextMenus.Add(new MenuItemModel("Export Raw Data", Export));
                ContextMenus.Add(new MenuItemModel("Export Raw Data to File Location", ExportToFileLocation));

                ContextMenus.Add(new MenuItemModel("Replace Raw Data", Replace) { IsEnabled = fileInfo.ParentArchiveFile.CanReplaceFiles });
                ContextMenus.Add(new MenuItemModel(""));
                ContextMenus.Add(new MenuItemModel("Delete", Delete) { IsEnabled = fileInfo.ParentArchiveFile.CanDeleteFiles });

                OnSelected += delegate  
                {
                };
                OnHeaderRenamed += delegate
                {
                    FileInfo.FileName = GetFullPath(this);
                };

                if (FileInfo.OpenFileFormatOnLoad && FileInfo.FileFormat == null)
                    OpenFile();
            }

            public void Reload(ArchiveFileInfo fileInfo)
            {
                if (FileInfo != null && FileInfo.FileFormat != null)
                    fileInfo.FileFormat = FileInfo.FileFormat;

                FileInfo = fileInfo;
            }

            private static string GetFullPath(NodeBase node)
            {
                return String.Join(Path.AltDirectorySeparatorChar, GetPaths(node));
            }

            private static List<string> GetPaths(NodeBase node)
            {
                List<string> paths = new List<string>();
                if (node.Parent != null && node.Parent is FolderNode)
                    GetParentPaths(ref paths, node.Parent);
                paths.Add(node.Header);
                return paths;
            }

            private static void GetParentPaths(ref List<string> paths , NodeBase node)
            {
                if (node.Parent != null && node.Parent is FolderNode)
                    GetParentPaths(ref paths, node.Parent);
                paths.Add(node.Header);
            }

            private void Export()
            {
                ImguiFileDialog dlg = new ImguiFileDialog();
                dlg.SaveDialog = true;
                dlg.FilterAll = true;
                dlg.FileName = this.Header;
                if (dlg.ShowDialog())
                    ExportFile(dlg.FilePath);
            }

            private void ExportToFileLocation()
            {
                string folderPath = ((IFileFormat)FileInfo.ParentArchiveFile).FileInfo.FolderPath;
                if (!Directory.Exists(folderPath))
                    return;

                string filePath = Path.Combine(folderPath, Path.GetFileName(this.Header));
                ExportFile(filePath);
            }

            private void ExportFile(string filePath)
            {
                if (FileInfo.FileFormat != null && FileInfo.FileFormat.CanSave)
                {
                    var mem = new MemoryStream();
                    FileInfo.FileFormat.Save(mem);
                    File.WriteAllBytes(filePath, mem.ToArray());
                }
                else
                {
                    File.WriteAllBytes(filePath, FileInfo.AsBytes());
                }
            }

            private void Replace()
            {
                ImguiFileDialog dlg = new ImguiFileDialog();
                dlg.FilterAll = true;
                if (dlg.ShowDialog())
                {
                    FileInfo.SetData(File.ReadAllBytes(dlg.FilePath));
                    if (FileInfo.FileFormat != null)
                        OpenFile();
                }
            }

            private void Delete()
            {
                int result = TinyFileDialog.MessageBoxInfoYesNo($"Are you sure you want to remove file {Header}? This operation cannot be undone.");
                if (result != 1)
                    return;

                FileInfo.ParentArchiveFile.DeleteFile(FileInfo);
                this.Parent.Children.Remove(this);
            }

            public override void OnDoubleClicked()
            {
                if (FileInfo.FileFormat == null)
                    OpenFile();
            }

            private void OpenFile()
            {
                FileInfo.FileFormat = FileInfo.OpenFile();
                if (FileInfo.FileFormat == null)
                    return;

                var editor = FileInfo.FileFormat as FileEditor;
                editor.Scene.Init();

                editor.Root.Header = this.Header;
                editor.Root.Icon = this.Icon;
                editor.Root.IconColor = this.IconColor;
                editor.Root.CanRename = this.CanRename;

                this.Tag = FileInfo.FileFormat;
                this.TagUI = new NodePropertyUI();
                this.TagUI.Tag = editor.Root.TagUI;
                editor.Root.Tag = FileInfo.FileFormat;

                var parent = this.Parent;
                var children = parent.Children;

                //Insert the loaded file node and swap the current archive node
                var index = children.IndexOf(this);
                children.RemoveAt(index);
                children.Insert(index, editor.Root);

                //Insert all the archive menus into the root node but as first menu
                var archiveNode = new MenuItemModel("Archive");
                archiveNode.MenuItems.AddRange(this.ContextMenus);
                editor.Root.ContextMenus.Insert(0, archiveNode);

                editor.Root.OnHeaderRenamed += delegate
                {
                    this.Header = editor.Root.Header;
                };

                //Load as archive file if needed
                if (FileInfo.FileFormat is IArchiveFile)
                    ArchiveEditor.Load((IArchiveFile)FileInfo.FileFormat, editor.Root);

                Workspace.ActiveWorkspace.ViewportWindow.Pipeline.AddFile(editor, this.Header);
                Workspace.ActiveWorkspace.ActiveEditor = editor;
            }

            public void SaveFile()
            {
                if (FileInfo.FileFormat != null && FileInfo.FileFormat.CanSave)
                {
                    var mem = new MemoryStream();
                    FileInfo.FileFormat.Save(mem);
                    //Compress if necessary
                    FileInfo.FileData = FileInfo.CompressData(mem);
                }
            }

            public Type GetTypeUI()
            {
                return typeof(HexWindow);
            }

            public void OnLoadUI(object uiInstance)
            {
                ((HexWindow)uiInstance).Load(FileInfo);
            }

            public void OnRenderUI(object uiInstance)
            {
                if (this.FileInfo.FileFormat == null)
                    ((HexWindow)uiInstance).Render();
            }
        }

        class HexWindow
        {
            MemoryEditor MemoryEditor = new MemoryEditor();

            private byte[] Data;

            private ArchiveFileInfo File;

            private EditorMode Editor = EditorMode.HexEditor;

            private IFileFormat FileFormat;

            private ImguiTextEditor TextEditor = new ImguiTextEditor();

            private bool reload = false;

            public void Load(ArchiveFileInfo file)
            {
                Data = file.AsBytes();
                File = file;
                FileFormat = null;
                TextEditor.Clear();
                reload = true;

                LoadFile();
                if (Editor == EditorMode.TextEditor)
                    PrepareTextEditor();
            }

            public void Render()
            {
                ImGui.PushItemWidth(250);
                if (ImguiCustomWidgets.ComboScrollable("##Editor", Editor.ToString(), ref Editor))
                {
                    LoadFile();
                    if (Editor == EditorMode.TextEditor)
                        PrepareTextEditor();
                }
                ImGui.PopItemWidth();

                if (Editor != EditorMode.HexEditor)
                {
                    ImGui.SameLine();
                    ImGui.PushItemWidth(200);
                    if (ImGui.Button("Save", new System.Numerics.Vector2(100, 22)))
                    {
                        if (this.Editor == EditorMode.TextEditor)
                            SaveTextEditor();

                        var mem = new MemoryStream();
                        FileFormat.Save(mem);
                        this.File.FileData = new MemoryStream(mem.ToArray());
                    }
                    ImGui.PopItemWidth();
                }

                ImGui.BeginChild("editorChild");

                switch (Editor)
                {
                    case EditorMode.Properties:
                        ShowProperties();
                        break;
                    case EditorMode.HexEditor:
                        if (Data != null)
                            MemoryEditor.Draw(Data, Data.Length);
                        break;
                    case EditorMode.FileEditor:
                        if (FileFormat != null)
                            ((FileEditor)FileFormat).DrawArchiveFileEditor();
                        break;
                    case EditorMode.TextEditor:
                        TextEditor.Render();
                        break;
                }

                ImGui.EndChild();
            }

            private void ShowProperties()
            {
                if (File.FileData == null)
                    return;

                if (ImGui.CollapsingHeader("Properties", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    ImGui.PushStyleColor(ImGuiCol.ChildBg, ThemeHandler.Theme.FrameBg);

                    ImGui.BeginChild("propertiesWindow");

                    ImGui.Columns(2);
                    ImGuiHelper.BoldText("Property"); ImGui.NextColumn();
                    ImGuiHelper.BoldText("Value"); ImGui.NextColumn();

                    ImGui.Text("File Size"); ImGui.NextColumn();
                    ImGui.Text(STMath.GetFileSize(File.FileData.Length)); ImGui.NextColumn();

                    ImGui.Columns(1);

                    ImGui.EndChild();

                    ImGui.PopStyleColor();
                }
            }

            private void LoadFile()
            {
                if (Editor == EditorMode.FileEditor || Editor == EditorMode.TextEditor)
                {
                    if (FileFormat == null)
                        FileFormat = File.OpenFile();
                }
            }

            private void PrepareTextEditor()
            {
                if (FileFormat as FileEditor != null)
                    TextEditor.Load(((FileEditor)FileFormat).ToTextEditor());
            }

            private void SaveTextEditor()
            {
                if (!string.IsNullOrEmpty(TextEditor.Text))
                    ((FileEditor)FileFormat).SaveTextEditor(TextEditor.Text);
            }

            enum EditorMode
            {
                Properties,
                HexEditor,
                FileEditor,
                TextEditor,
            }
        }
    }
}
