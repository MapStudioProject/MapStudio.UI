using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLFrameworkEngine;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using Toolbox.Core;
using System.Drawing;
using Toolbox.Core.IO;
using ImGuiNET;

namespace MapStudio.UI
{
    public class ImageEditorViewport : Viewport2D
    {
        public STGenericTexture ActiveTexture;

        ImageEditorBackground ImageBackground;
        ImageEditor ImageEditor;

        public ImageEditorViewport(ImageEditor editor)
        {
            ImageEditor = editor;
            ImageBackground = new ImageEditorBackground(editor);
        }

        public override void RenderScene()
        {
            var shader = GlobalShaders.GetShader("IMAGE_EDITOR");
            shader.Enable();

            ImageBackground.Draw(ActiveTexture, Width, Height, Camera);
        }

        public override void DrawImage()
        {
            var id = GetViewportTexture();

            ImGui.Image((IntPtr)id, new System.Numerics.Vector2(Width, Height));
        }

        public void Reset() {
        }
    }
}
