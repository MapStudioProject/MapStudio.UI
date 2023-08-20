using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using UIFramework;

namespace MapStudio.UI
{
    public class ImageColorFillEditor : Window
    {
        private ImageEditDrawArgs Parameters;

        private ImageEditorHelper Editor;

        public ImageColorFillEditor(ImageEditorHelper editorHelper, ImageEditDrawArgs args)
            : base("Color Fill Editor", new Vector2(130, 80))
        {
            Editor = editorHelper;
            Parameters = args;
        }

        public override void OnWindowClosing()
        {
            Editor.Reset();
        }

        public override void Render()
        {
            bool update = false;

            ImGui.Columns(2);

            ImGuiHelper.BoldText("Color");
            ImGui.NextColumn();

            update |= ImGui.ColorEdit4("##ColorFillEdit", ref Parameters.Color, ImGuiColorEditFlags.NoInputs);
            ImGui.NextColumn();

            ImGui.Columns(1);

            if (update)
                Editor.DrawImage();

            var width = ImGui.GetWindowWidth();

            if (ImGui.Button("Apply", new Vector2(width / 2 - 5, 22)))
            {
                Editor.EndEdit();
            }
            ImGui.SameLine();
            if (ImGui.Button("Cancel", new Vector2(width / 2 - 5, 22)))
            {
                Editor.Reset();
            }
        }
    }
}
