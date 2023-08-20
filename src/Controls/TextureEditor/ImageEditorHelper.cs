using GLFrameworkEngine;
using ImGuiNET;
using IONET.Collada.FX.Custom_Types;
using OpenTK.Graphics.OpenGL;
using SharpEXR.ColorSpace;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;
using UIFramework;
using static MapStudio.UI.TabControl;

namespace MapStudio.UI
{
    public class ImageEditorHelper
    {
        public bool isActive = false;


        private STGenericTexture Texture;

        private GLTexture RenderSource;

        private int ArrayLevel = 0;

        private Framebuffer Framebuffer;

        private EditMode Mode;

        Window Window = null;

        ImageEditDrawArgs DrawArgs;

        public ImageEditorHelper()
        {
            DrawArgs = new ImageEditDrawArgs();
        }

        public ImageEditorHelper(STGenericTexture texture)
        {
            Texture = texture;
        }

        public enum EditMode
        {
            ColorFill,
            HSBC,
            NormalMap,
        }

        public void BeginEdit(STGenericTexture texture, EditMode editMode, int array_level = 0)
        {
            Mode = editMode;
            ArrayLevel = array_level; //array level to edit
            Texture = texture; //texture to update and edit

            //Get rendered display and get pixels to edit
            RenderSource = texture.RenderableTex as GLTexture;

            var format = Texture.IsSRGB ? PixelInternalFormat.Srgb : PixelInternalFormat.Rgba;

            //Render texture instance to show edits
            Framebuffer = new Framebuffer(FramebufferTarget.Framebuffer, RenderSource.Width, RenderSource.Height, format);

            DrawImage();

            isActive = true;

            if (this.Mode == EditMode.HSBC)
                Window = new ImageHSBEditor(this, DrawArgs);
            if (this.Mode == EditMode.ColorFill)
                Window = new ImageColorFillEditor(this, DrawArgs);
            if (this.Mode == EditMode.NormalMap)
                Window = new ImageNormalMapGenEditor(this, DrawArgs);
        }

        public void Reset()
        {
            Texture.RenderableTex = RenderSource;
            Framebuffer?.Dispose();

            isActive = false;
            GLContext.ActiveContext.UpdateViewport = true;
        }

        public void DrawImage()
        {
            if (this.Mode == EditMode.ColorFill)
            {
                StandardMaterial mat = new StandardMaterial();
                mat.CameraMatrix = OpenTK.Matrix4.Identity;
                mat.Color = new OpenTK.Vector4(DrawArgs.Color.X, DrawArgs.Color.Y, DrawArgs.Color.Z, DrawArgs.Color.W);
                mat.Render(GLContext.ActiveContext);
            }
            else
            {
                var shader = GlobalShaders.GetShader("ImageTool", Path.Combine("Editor", "ImageTool"));
                shader.Enable();

                shader.SetInt("textureType", 0);
                shader.SetInt("currentArrayLevel", 0);
                shader.SetInt("currentMipLevel", 0);
                shader.SetBool("isSRGB", Texture.IsSRGB);
                shader.SetBool("isBC5S", Texture.Platform.OutputFormat == TexFormat.BC5_SNORM);
                shader.SetBool("ConvertToNormalMap", false);

                if (this.Mode == EditMode.NormalMap)
                {
                    shader.SetBool("ConvertToNormalMap", true);
                }

                //Editor parameters
                shader.SetFloat("uBrightness", DrawArgs.Brightness);
                shader.SetFloat("uSaturation", DrawArgs.Saturation);
                shader.SetFloat("uHue", DrawArgs.Hue);
                shader.SetFloat("uContrast", DrawArgs.Contrast);
                shader.SetVector2("heightmapSize", new OpenTK.Vector2(RenderSource.Width, RenderSource.Height));
                shader.SetVector2("viewportSize", new OpenTK.Vector2(Framebuffer.Width, Framebuffer.Height));
                shader.SetFloat("normalStrength", DrawArgs.NormalMapStrength);
                
                if (RenderSource is GLTexture2D)
                    shader.SetTexture(RenderSource, "textureInput", 1);
                if (RenderSource is GLTexture2DArray)
                    shader.SetTexture(RenderSource, "textureArrayInput", 2);
                if (RenderSource is GLTextureCube)
                    shader.SetTexture(RenderSource, "textureCubeInput", 3);
            }


            Framebuffer.Bind();

            GL.Viewport(0, 0, Framebuffer.Width, Framebuffer.Height);

            GL.ClearColor(1, 0, 0, 1);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            ScreenQuadRender.Draw();

            var tex = (GLTexture)Framebuffer.Attachments[0];
            tex.MipCount = RenderSource.MipCount;

            if (Texture.MipCount > 1)
                tex.GenerateMipmaps();

            tex.SetFilter(RenderSource.MinFilter, RenderSource.MagFilter);
            Texture.RenderableTex = tex;

            Framebuffer.Unbind();

            GLContext.ActiveContext.UpdateViewport = true;
        }

        public void DrawUI()
        {
            if (!isActive)
                return;

            Window?.Show();
        }

        public void RegenerateMipmaps(int count)
        {
            var source = this.RenderSource.GetBytes(0);
            Texture.MipCount = (uint)count;
            Apply(source);
        }

        public void EndEdit()
        {
            var tex = (GLTexture)Framebuffer.Attachments[0];
            Apply(tex.GetBytes(0));
        }

        public void Apply(byte[] data)
        {
            //Generate mipmaps
            var edit = Image.LoadPixelData<Rgba32>(data, RenderSource.Width, RenderSource.Height);
            //check if width/height was edited
            if (Texture.Width != edit.Width || Texture.Height != edit.Height)
            {
                //regenerate mip counter
                Texture.MipCount = 1 + (uint)Math.Floor(Math.Log(Math.Max(edit.Width, edit.Height), 2));
            }

            var imageList = ImageSharpTextureHelper.GenerateMipmaps(edit, Texture.MipCount);

            List<byte[]> mipmaps = new List<byte[]>();
            mipmaps.AddRange(imageList.Select(x => x.GetSourceInBytes()));

            Texture.SetImageData(mipmaps, Texture.Width, Texture.Height, ArrayLevel);

            edit.Dispose();
            foreach (var img in imageList)
                img.Dispose();

            Framebuffer?.Dispose();

            isActive = false;
        }
    }
}
