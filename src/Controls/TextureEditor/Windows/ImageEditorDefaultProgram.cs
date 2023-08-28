using GLFrameworkEngine;
using ImGuiNET;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Toolbox.Core;
using UIFramework;

namespace MapStudio.UI
{
    public class ImageEditorDefaultProgram 
    {
        private string Extension = ImageFileFormats[0];
        private string Format;

        private List<string> FormatList = new List<string>();

        private STGenericTexture Texture;

        public FileSystemWatcher FileWatcher;

        public int ArrayLevel;
        public int MipLevel;

        private bool ShowDialog = false;

        private bool isEdited = false;
        private string FileName;

        private EditChannel EditChannelMode = EditChannel.Default;

        private enum EditChannel
        {
            Default,
            Color,
            Alpha,

            Red,
            Green,
            Blue,
        }

        public ImageEditorDefaultProgram(STGenericTexture texture, int arrayLevel, int mipLevel = 0)
        {
            ArrayLevel = arrayLevel;

            foreach (var format in texture.SupportedFormats)
                FormatList.Add(format.ToString());

            Format = texture.Platform.OutputFormat.ToString();
            Texture = texture;

            SetUpFileSystemWatcher();
        }

        private void SetUpFileSystemWatcher()
        {
            FileWatcher = new FileSystemWatcher();
            FileWatcher.Path = Path.GetTempPath();
            FileWatcher.NotifyFilter = NotifyFilters.Attributes |
                NotifyFilters.CreationTime |
                NotifyFilters.FileName |
                NotifyFilters.LastAccess |
                NotifyFilters.LastWrite |
                NotifyFilters.Size |
                NotifyFilters.Security;

            FileWatcher.EnableRaisingEvents = false;
            FileWatcher.Changed += new FileSystemEventHandler(OnFileWatcherChanged);
            FileWatcher.Filter = "";
        }

        private void OnFileWatcherChanged(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine("File: " + e.FullPath + " " + e.ChangeType);
            //var Result = TinyFileDialog.MessageBoxInfoYesNo("Texture has been modifed in external program! Would you like to apply the edits?");

            FileName = e.FullPath;
            isEdited = true;

           // FileWatcher.Filter = "";
            //FileWatcher.EnableRaisingEvents = false;
        }

        public void ApplyEdits()
        {
            if (!isEdited)
                return;

            isEdited = false;

            TexFormat format = (TexFormat)Enum.Parse(typeof(TexFormat), this.Format);

            //Generate mipmaps
            var edit = Image.Load<Rgba32>(FileName);
            if (this.EditChannelMode != EditChannel.Default)
                edit = SetChannelEdit(edit, EditChannelMode);

            //check if width/height was edited
            if (Texture.Width != edit.Width || Texture.Height != edit.Height)
            {
                //regenerate mip counter
                Texture.MipCount = 1 + (uint)Math.Floor(Math.Log(Math.Max(edit.Width, edit.Height), 2));
            }

            var imageList = ImageSharpTextureHelper.GenerateMipmaps(edit, Texture.MipCount);

            List<byte[]> mipmaps = new List<byte[]>();
            mipmaps.AddRange(imageList.Select(x => x.GetSourceInBytes()));

            Texture.Width = (uint)edit.Width;
            Texture.Height = (uint)edit.Height;

            Texture.Platform.OutputFormat = format;
            Texture.SetImageData(mipmaps, Texture.Width, Texture.Height, ArrayLevel);
        }

        private void SaveChannel(string path, EditChannel channel)
        {
            //original image
            var rgba = ((GLTexture)Texture.RenderableTex).GetBytes(0);
            var image = GetChannel(rgba, channel);
            image.Save(path);
        }

        private Image<Rgba32> GetChannel(byte[] src, EditChannel channel)
        {
            byte[] rgba = new byte[Texture.Width * Texture.Height * 4];

            int index = 0;
            for (int w = 0; w < Texture.Width; w++)
            {
                for (int h = 0; h < Texture.Height; h++)
                {
                    switch (channel)
                    {
                        case EditChannel.Red: //set red
                            rgba[index + 0] = src[index + 0];
                            rgba[index + 1] = src[index + 0];
                            rgba[index + 2] = src[index + 0];
                            rgba[index + 3] = 255;
                            break; 
                        case EditChannel.Green: //set green
                            rgba[index + 0] = src[index + 1];
                            rgba[index + 1] = src[index + 1];
                            rgba[index + 2] = src[index + 1];
                            rgba[index + 3] = 255;
                            break;
                        case EditChannel.Blue: //set blue
                            rgba[index + 0] = src[index + 2];
                            rgba[index + 1] = src[index + 2];
                            rgba[index + 2] = src[index + 2];
                            rgba[index + 3] = 255;
                            break;
                        case EditChannel.Alpha:  //set alpha
                            rgba[index + 0] = src[index + 3];
                            rgba[index + 1] = src[index + 3];
                            rgba[index + 2] = src[index + 3];
                            rgba[index + 3] = 255;
                            break;
                        case EditChannel.Color: //set color only
                            rgba[index + 0] = src[index + 0];
                            rgba[index + 1] = src[index + 1];
                            rgba[index + 2] = src[index + 2];
                            rgba[index + 3] = 255;
                            break;

                    }
                    index += 4;
                }
            }
            return Image.LoadPixelData<Rgba32>(rgba, (int)Texture.Width, (int)Texture.Height);
        }

        private Image<Rgba32> SetChannelEdit(Image<Rgba32> edited, EditChannel channel)
        {
            //original image
            var rgba = ((GLTexture)Texture.RenderableTex).GetBytes(0);
            //target edit
            var target = edited.GetSourceInBytes();

            int index = 0;
            for (int w = 0; w < Texture.Width; w++)
            {
                for (int h = 0; h < Texture.Height; h++)
                {
                    switch (channel)
                    {
                        case EditChannel.Red: rgba[index + 0] = target[index + 0]; break; //set red
                        case EditChannel.Green: rgba[index + 1] = target[index + 0]; break; //set green
                        case EditChannel.Blue: rgba[index + 2] = target[index + 0]; break; //set blue
                        case EditChannel.Alpha: rgba[index + 3] = target[index + 0]; break; //set alpha
                        case EditChannel.Color: //set color only
                            rgba[index + 0] = target[index + 0];
                            rgba[index + 1] = target[index + 1];
                            rgba[index + 2] = target[index + 2];
                            break; 

                    }
                    index += 4;
                }
            }

            //dispose old
            edited.Dispose();
            //newly edited image
            return Image.LoadPixelData<Rgba32>(rgba, (int)Texture.Width, (int)Texture.Height);
        }

        public void Start()
        {
            DialogHandler.Show("Default Program ", DrawUI, LoadDefaultProgram);
        }

        private void DrawUI()
        {
            ImGui.Columns(2);

            ImGui.Text("File Format");
            ImGui.NextColumn();
            ImguiCustomWidgets.ComboScrollable("##FileFormat", Extension, ref Extension, ImageFileFormats);
            ImGui.NextColumn();

            if (FormatList.Count > 0)
            {
                ImGui.Text("Format to save back as");
                ImGui.NextColumn();
                ImguiCustomWidgets.ComboScrollable("##ImageFormat", Format, ref Format, FormatList);
                ImGui.NextColumn();
            }

            ImGui.Text("Channel Edit");
            ImGui.NextColumn();

            ImguiCustomWidgets.ComboScrollable("##ChannelEdit", this.EditChannelMode.ToString(), ref this.EditChannelMode);
            ImGui.NextColumn();

            ImGui.Text("Show Edit Dialog");
            ImGui.NextColumn();
            ImGui.Checkbox("", ref ShowDialog);
            ImGui.NextColumn();

            ImGui.Columns(1);

            DialogHandler.DrawCancelOk();
        }

        private void LoadDefaultProgram(bool isOk)
        {
            if (!isOk) return;

            bool useDefaultEditor = true;

            var setting = new TextureExportSettings()
            {
                ArrayLevel = ArrayLevel,
                MipLevel = MipLevel,
            };

            string ext = GetSelectedExtension(Extension);
            string path = Path.GetTempFileName();

            if (File.Exists(Path.ChangeExtension(path, ext)))
                File.Delete(Path.ChangeExtension(path, ext));

            File.Move(path, Path.ChangeExtension(path, ext));
            path = Path.ChangeExtension(path, ext);

            switch (ext)
            {
                case ".dds":
                    Texture.SaveDDS(path, setting);
                    break;
                case ".astc":
                    Texture.SaveASTC(path);
                    break;
                default:
                    if (this.EditChannelMode == EditChannel.Default)
                        Texture.SaveBitmap(path, setting);
                    else
                        SaveChannel(path, this.EditChannelMode);
                    break;
            }

            //Start watching for changes
            FileWatcher.EnableRaisingEvents = true;
            FileWatcher.Filter = Path.GetFileName(path);

            if (useDefaultEditor)
                OpenWithDefaultProgram(path);
            else
                ShowOpenWithDialog(path);
        }

        private static void OpenWithDefaultProgram(string path)
        {
            using Process fileopener = new Process();

            fileopener.StartInfo.FileName = "explorer";
            fileopener.StartInfo.Arguments = "\"" + path + "\"";
            fileopener.Start();
        }

        private static Process ShowOpenWithDialog(string path)
        {
            var args = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "shell32.dll");
            args += ",OpenAs_RunDLL " + path;
            return Process.Start("rundll32.exe", args);
        }

        public string GetSelectedExtension(string selectedExt)
        {
            string GetSubstringByString(string a, string b, string c)
            {
                return c.Substring((c.IndexOf(a) + a.Length), (c.IndexOf(b) - c.IndexOf(a) - a.Length));
            }

            string output = GetSubstringByString("(", ")", selectedExt);
            output = output.Replace('*', ' ');

            if (output == ".")
                output = ".raw";

            return output;
        }

        static string[] ImageFileFormats = new string[]
        {
           // "Direct Draw Surface (.dds)",
            "Portable Network Graphics (.png)",
            "Joint Photographic Experts Group (.jpg)",
            "TGA (.tga)",
            "Tagged Image File Format (.tif)",
            "Bitmap Image (.bmp)",
        };
    }
}
