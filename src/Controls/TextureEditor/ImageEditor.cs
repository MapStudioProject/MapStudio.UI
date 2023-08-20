using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using Toolbox.Core;
using GLFrameworkEngine;
using System.Numerics;

namespace MapStudio.UI
{
    public class ImageEditor
    {
        ImageEditorViewport ImageCanvas;
        Cubemap3DWindow Cubemap3DWindow = new Cubemap3DWindow() { Opened = false };

        public static ImageEditor imageEditor = new ImageEditor();

        public int SelectedChannelIndex = -1;

        public STGenericTexture ActiveTexture { get; set; }

        public ImageEditorSettings Settings => GlobalSettings.Current.ImageEditor;

        public int currentArrayLevel = 0;
        public int currentMipLevel = 0;

        static float h = 600;
        static float w = 600;

        public ImageEditorHelper Editor;

        private ImageEditorDefaultProgram EditDefaultProgram;

        private void Init() {
            ImageCanvas = new ImageEditorViewport(this);
            ImageCanvas.OnLoad();
            ImageCanvas.Camera.Zoom = 150;
        }

        public static void LoadEditor(STGenericTexture texture) {
            //Reset any edits during an editor switch
            if (imageEditor.Editor != null && imageEditor.Editor.isActive)
            {
                if (imageEditor.ActiveTexture != texture)
                    imageEditor.Editor?.Reset();
            }

            imageEditor.Render(texture);
        }

        public void Render(STGenericTexture texture) {
            if (ImageCanvas == null)
                Init();

            var size = ImGui.GetWindowSize();

            if (ActiveTexture != texture) {
                ActiveTexture = texture;
                currentMipLevel = 0;
                currentArrayLevel = 0;
            }

            if (Cubemap3DWindow.Opened)
                Cubemap3DWindow.Show();

            EditDefaultProgram?.ApplyEdits();

            if (Settings.DisplayProperties)
            {
                if (Settings.DisplayVertical)
                {
                    var propertyWindowSize = new Vector2(size.X, size.Y - h - 20);

                    DrawPropertiesDock(propertyWindowSize);
                    DrawVerticalDivider();

                    var canvasWindowSize = new Vector2(size.X, h - 20);
                    DrawImageCanvasView(canvasWindowSize);
                }
                else
                {
                    ImGui.Columns(2);

                    var canvasWindowSize = new Vector2(ImGui.GetColumnWidth(1), size.Y - 30);
                    DrawImageCanvasView(canvasWindowSize);
                    ImGui.NextColumn();

                    var propertyWindowSize = new Vector2(ImGui.GetColumnWidth(0), size.Y - 30);

                    DrawPropertiesDock(propertyWindowSize);
                    ImGui.NextColumn();

                    ImGui.Columns(1);
                }
            }
            else
            {
                var canvasWindowSize = new Vector2(size.X, size.Y - 20);
                DrawImageCanvasView(canvasWindowSize);
            }
        }

        private void DrawVerticalDivider()
        {
            var height = ImGui.GetWindowHeight();

            ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetStyle().Colors[(int)ImGuiCol.Separator]);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ImGui.GetStyle().Colors[(int)ImGuiCol.SeparatorHovered]);
            ImGui.Button("##hsplitter", new Vector2(-1, 2));
            if (ImGui.IsItemActive())
            {
                var deltaY = ImGui.GetIO().MouseDelta.Y;
                if (h - deltaY < height - 22 && h - deltaY > 22)
                    h -= deltaY;
            }
            ImGui.PopStyleColor(2);
        }

        private void DrawPropertiesDock(Vector2 propertyWindowSize)
        {
            if (ImGui.BeginChild("##IMAGE_TABMENU", propertyWindowSize, true))
            {
                ImGui.BeginTabBar("image_menu");
                if (ImguiCustomWidgets.BeginTab("image_menu", "Properties"))
                {
                    ImguiBinder.LoadPropertiesComponentModelBase(ActiveTexture.DisplayProperties, (o, e) =>
                    {
                        ActiveTexture.DisplayPropertiesChanged?.Invoke(o, e);
                    });
                    ImGui.EndTabItem();
                }
                if (ImguiCustomWidgets.BeginTab("image_menu", "User Data"))
                {
                    ImGui.EndTabItem();
                }
                ImGui.EndTabBar();
            }
            ImGui.EndChild();
        }

        private void DrawImageCanvasView(Vector2 canvasWindowSize)
        {
            var menuSize = new Vector2(22, 22);

            if (ImGui.BeginChild("CANVAS_WINDOW", canvasWindowSize, false,
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.MenuBar))
            {
                if (ImGui.BeginMenuBar())
                {
                    if (ImGui.BeginMenu("File"))
                    {
                        ImGui.AlignTextToFramePadding();
                        if (ImGui.MenuItem("Export"))
                        {

                            ImguiFileDialog dlg = new ImguiFileDialog();
                            dlg.SaveDialog = true;
                            dlg.FileName = $"{ActiveTexture.Name}.png";
                            dlg.AddFilter(".dds", ".dds");
                            foreach (var ext in TextureDialog.SupportedExtensions)
                                dlg.AddFilter(ext, ext);

                            if (dlg.ShowDialog())
                                ActiveTexture.Export(dlg.FilePath, new TextureExportSettings());
                        }
                        ImGui.EndMenu();
                    }
                    if (ImGui.BeginMenu("Edit"))
                    {
                        ImGui.AlignTextToFramePadding();
                        if (ImGui.MenuItem("Fill Color"))
                        {
                            ColorFillEdit();
                        }
                        ImGui.AlignTextToFramePadding();
                        if (ImGui.MenuItem("With External Program"))
                        {
                            EditDefaultProgram = new ImageEditorDefaultProgram(
                                ActiveTexture, currentArrayLevel, currentMipLevel);

                            EditDefaultProgram.Start();
                        }
                        ImGui.AlignTextToFramePadding();
                        if (ImGui.MenuItem("Hue-Sat-Brightness-Contrast"))
                        {
                            BrightnessEdit();
                        }
                        ImGui.AlignTextToFramePadding();
                        if (ImGui.MenuItem("To Normal Map"))
                        {
                            NormalMapEdit();
                        }
                        ImGui.EndMenu();
                    }
                    if (ImGui.BeginMenu("View"))
                    {
                        ImGui.AlignTextToFramePadding();
                        if (ImGui.MenuItem("Enable Zoom", "", Settings.Zoom))
                        {
                            Settings.Zoom = !Settings.Zoom;
                            Settings.Save();
                        }
                        ImGui.AlignTextToFramePadding();
                        if (ImGui.MenuItem("Display Properties", "", Settings.DisplayProperties))
                        {
                            Settings.DisplayProperties = !Settings.DisplayProperties;
                            Settings.Save();
                        }
                        ImGui.AlignTextToFramePadding();
                        if (ImGui.MenuItem("Display Vertical", "", Settings.DisplayVertical))
                        {
                            Settings.DisplayVertical = !Settings.DisplayVertical;
                            Settings.Save();
                        }
                        ImGui.AlignTextToFramePadding();
                        if (ImGui.MenuItem("Display Alpha", "", Settings.DisplayAlpha))
                        {
                            Settings.DisplayAlpha = !Settings.DisplayAlpha;
                        }
                        ImGui.AlignTextToFramePadding();
                        if (ImGui.MenuItem("Use Component Selector", "", Settings.UseChannelComponents))
                        {
                            Settings.UseChannelComponents = !Settings.UseChannelComponents;
                            Settings.Save();
                        }
                        ImGui.AlignTextToFramePadding();
                        if (ImGui.MenuItem("View Cubemap 3D", "", Cubemap3DWindow.Opened))
                        {
                            if (this.ActiveTexture.ArrayCount > 1)
                            {
                                Cubemap3DWindow.Load((GLTexture)this.ActiveTexture.RenderableTex);
                                Cubemap3DWindow.Opened = !Cubemap3DWindow.Opened;
                            }
                        }
                        
                        ImGui.EndMenu();
                    }
                    if (ImGui.BeginMenu("Image"))
                    {
                        ImGui.AlignTextToFramePadding();
                        if (ImGui.MenuItem("Generate Mipmaps"))
                        {
                            ImageEditorHelper editorHelper = new ImageEditorHelper();
                            editorHelper.BeginEdit(ActiveTexture, ImageEditorHelper.EditMode.NormalMap);
                            editorHelper.RegenerateMipmaps((int)ActiveTexture.MipCount);
                        }
                        ImGui.AlignTextToFramePadding();
                        if (ImGui.MenuItem("Resize"))
                        {
                        
                        }
                        ImGui.AlignTextToFramePadding();
                        if (ImGui.MenuItem("Re - Encode"))
                        {

                        }
                        ImGui.AlignTextToFramePadding();
                        if (ImGui.MenuItem("Flip Horizontal"))
                        {

                        }
                        ImGui.EndMenu();
                    }
                    if (ImGui.BeginMenu("Adjustments"))
                    {
                        ImGui.EndMenu();
                    }
                    ImGui.EndMenuBar();
                }

                //Make icon buttons invisible aside from the icon itself.
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4());
                {
                    //Draw icon bar
                    if (ImGui.ImageButton((IntPtr)IconManager.GetTextureIcon("IMG_EDIT_BUTTON"), menuSize))
                    {
                        EditDefaultProgram = new ImageEditorDefaultProgram(
                              ActiveTexture, currentArrayLevel, currentMipLevel);

                        EditDefaultProgram.Start();
                    }
                    ImGui.SameLine();
                    ImguiCustomWidgets.ImageButtonToggle(
                        IconManager.GetTextureIcon("IMG_ALPHA_BUTTON"),
                        IconManager.GetTextureIcon("IMG_NOALPHA_BUTTON"), ref Settings.DisplayAlpha, menuSize);
                    ImGui.SameLine();
                }

                ImGui.PushItemWidth(150);
                ImguiCustomWidgets.ComboScrollable("##imageCB", Settings.SelectedBackground.ToString(), ref Settings.SelectedBackground);
                ImGui.PopItemWidth();

                if (Settings.SelectedBackground == ImageEditorSettings.BackgroundType.Custom)
                {
                    ImGui.SameLine();
                    ImGui.ColorEdit4("##BackgroundColor", ref Settings.BackgroundColor, ImGuiColorEditFlags.NoInputs);
                }

                ImGui.PopStyleColor();

                //Draw the array and mip level counter buttons
                ImGui.AlignTextToFramePadding();
                ImGui.Text("Array Level " + $"{currentArrayLevel} / {ActiveTexture.ArrayCount - 1}");
                ImGui.SameLine();
                if (ImGui.Button("<##array", menuSize)) { AdjustArrayLevel(-1); }
                ImGui.SameLine();
                if (ImGui.Button(">##array", menuSize)) { AdjustArrayLevel(1); }
                ImGui.SameLine();

                ImGui.Text("Mip Level " + $"{currentMipLevel} / {ActiveTexture.MipCount - 1}");
                ImGui.SameLine();
                if (ImGui.Button("<##mip", menuSize)) { AdjustMipLevel(-1); }
                ImGui.SameLine();
                if (ImGui.Button(">##mip", menuSize)) { AdjustMipLevel(1); }

                ImGui.SameLine();
                ImGui.PushItemWidth(150);

                string[] channelList = new string[] { "Red", "Green", "Blue", "Alpha" };
                string channel = SelectedChannelIndex == -1 ? "RGBA" : channelList[SelectedChannelIndex];
                if (ImGui.BeginCombo("##ChannelSel", $"Channel {channel}"))
                {
                    bool select = SelectedChannelIndex == -1;
                    if (ImGui.Selectable("RGBA", select))
                        SelectedChannelIndex = -1;

                    for (int i = 0; i < 4; i++)
                    {
                        select = SelectedChannelIndex == i;
                        if (ImGui.Selectable(channelList[i], select))
                            SelectedChannelIndex = i;
                        if (select)
                            ImGui.SetItemDefaultFocus();
                    }

                    if (select)
                        ImGui.SetItemDefaultFocus();

                    ImGui.EndCombo();
                }

                ImGui.PopItemWidth();

                Editor?.DrawUI();

                var cursorY = ImGui.GetCursorPosY();

                //Draw the main image canvas
                DrawImageCanvas(new Vector2(canvasWindowSize.X, canvasWindowSize.Y - 120));

                ImGui.SetCursorPosY(cursorY + canvasWindowSize.Y - 115);

                if (!Settings.DisplayAlpha)
                    ImGui.TextColored(ThemeHandler.Theme.Warning, $"Note! Alpha is hidden in viewer!");
                else
                    ImGui.Text("");

                if (this.ActiveTexture != null)
                    ImGui.Text($"Zoom: 100 Image {this.ActiveTexture.Width} x {this.ActiveTexture.Height} Data Size: {this.ActiveTexture.DataSize}");

                ContextMenuRightClick();
            }
            ImGui.EndChild();
        }

        private void ContextMenuRightClick()
        {
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new System.Numerics.Vector2(8, 1));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(8, 2));
            ImGui.PushStyleColor(ImGuiCol.Separator, new System.Numerics.Vector4(0.4f, 0.4f, 0.4f, 1.0f));

            if (ImGui.BeginPopupContextItem("##OUTLINER_POPUP", ImGuiPopupFlags.MouseButtonRight))
            {
                if (ImGui.MenuItem("Open In Default Program"))
                {
                    EditDefaultProgram = new ImageEditorDefaultProgram(
                              ActiveTexture, currentArrayLevel, currentMipLevel);

                    EditDefaultProgram.Start();
                }
                ImGui.EndPopup();
            }
            ImGui.PopStyleVar(2);
            ImGui.PopStyleColor(1);
        }

        private void ColorFillEdit()
        {
            Editor = new ImageEditorHelper();
            Editor.BeginEdit(this.ActiveTexture, ImageEditorHelper.EditMode.ColorFill, currentArrayLevel);
        }

        private void BrightnessEdit()
        {
            Editor = new ImageEditorHelper();
            Editor.BeginEdit(this.ActiveTexture, ImageEditorHelper.EditMode.HSBC, currentArrayLevel);
        }

        private void NormalMapEdit()
        {
            Editor = new ImageEditorHelper();
            Editor.BeginEdit(this.ActiveTexture, ImageEditorHelper.EditMode.NormalMap, currentArrayLevel);
        }

        private void DrawImageCanvas(Vector2 size)
        {
            ImageCanvas.ActiveTexture = ActiveTexture;
            ImageCanvas.Render((int)size.X, (int)size.Y);
        }

        private void AdjustArrayLevel(int increment)
        {
            if (increment < 0 && currentArrayLevel > 0)
                currentArrayLevel--;
            if (increment > 0 && currentArrayLevel < ActiveTexture.ArrayCount - 1)
                currentArrayLevel++;
        }

        private void AdjustMipLevel(int increment)
        {
            if (increment < 0 && currentMipLevel > 0)
                currentMipLevel--;
            if (increment > 0 && currentMipLevel < ActiveTexture.MipCount - 1)
                currentMipLevel++;
        }
    }
}
