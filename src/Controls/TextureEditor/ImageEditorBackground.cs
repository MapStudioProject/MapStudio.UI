using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using GLFrameworkEngine;
using ImGuiNET;

namespace MapStudio.UI
{
    public class ImageEditorBackground
    {
        ImageEditor ImageEditor;

        RenderMesh<VertexPositionTexCoord> QuadDrawer;

        static VertexPositionTexCoord[] Vertices = new VertexPositionTexCoord[]
        {
            new VertexPositionTexCoord(new Vector3(-1, 1, 0), new Vector2(0, 1)),
            new VertexPositionTexCoord(new Vector3(-1, -1, 0), new Vector2(0, 0)),
            new VertexPositionTexCoord(new Vector3(1, 1, 0), new Vector2(1, 1)),
            new VertexPositionTexCoord(new Vector3(1, -1, 0), new Vector2(1, 0)),
        };

        public ImageEditorBackground(ImageEditor editor) {
            ImageEditor = editor;
        }

        public void Init()
        {
            if (QuadDrawer == null)
                QuadDrawer = new RenderMesh<VertexPositionTexCoord>(Vertices, PrimitiveType.TriangleStrip)
                {
                    //Disable stat display as it is only used for the 3D viewport
                    DebugStats = false,
                };
        }

        public void Draw(STGenericTexture texture, int width, int height, Viewport2D.Camera2D camera)
        {
            Vector3 scale = new Vector3(1, 1, 1);
            scale = UpdateAspectScale(scale, width, height, texture);

            Init();

            var shader = GlobalShaders.GetShader("IMAGE_EDITOR");
            shader.Enable();

            var cameraMtx = Matrix4.CreateScale(100) * camera.ProjectionMatrix;
            shader.SetMatrix4x4("mtxCam", ref cameraMtx);
            shader.SetInt("channelSelector", -1);

            GL.Disable(EnableCap.Blend);

            DrawBackground(shader);

            if (ImageEditor.Settings.Zoom)
                cameraMtx = camera.ViewMatrix * camera.ProjectionMatrix;
            else
            {
                cameraMtx = Matrix4.Identity;

                var size = new System.Numerics.Vector2(width, height);

                //Aspect size

                #region Calculate Aspect Size
                float tw, th, tx, ty;

                int w = (int)texture.Width;
                int h = (int)texture.Height;

                double whRatio = (double)w / h;
                if (texture.Width >= texture.Height)
                {
                    tw = size.X;
                    th = (int)(tw / whRatio);
                    if (th > size.Y)
                    {
                        th = size.Y;
                        tw = (int)(th * whRatio);
                    }
                }
                else
                {
                    th = size.Y;
                    tw = (int)(th * whRatio);
                }

                tx = (size.X - tw) / 2;
                ty = (size.Y - th) / 2;

                #endregion

                scale = new Vector3(tw / size.X, th/ size.Y, 1);
            }

            shader.SetMatrix4x4("mtxCam", ref cameraMtx);

            DrawImage(shader, texture, scale.Xy);
        }

        static GLMaterialBlendState ImageBlendState = new GLMaterialBlendState() 
        {
            BlendColor = true,
        };

        private void DrawImage(ShaderProgram shader, STGenericTexture texture, Vector2 scale)
        {
            ImageBlendState.RenderBlendState();

            //Draw main texture quad inside boundings (0, 1)
             shader.SetVector2("scale", scale);
           // shader.SetVector2("scale", new Vector2(1));
            shader.SetVector4("uColor", new Vector4(1));
            shader.SetBoolToInt("isSRGB", texture.IsSRGB);
            shader.SetBoolToInt("isBC5S", texture.Platform.OutputFormat == TexFormat.BC5_SNORM);
            shader.SetBoolToInt("displayAlpha", ImageEditor.Settings.DisplayAlpha || ImageEditor.SelectedChannelIndex == 3);
            shader.SetVector2("texCoordScale", new Vector2(1));
            shader.SetFloat("width", texture.Width);
            shader.SetFloat("height", texture.Height);
            shader.SetInt("currentMipLevel", ImageEditor.currentMipLevel);
            shader.SetInt("currentArrayLevel", ImageEditor.currentArrayLevel);

            if (ImageEditor.SelectedChannelIndex != -1)
                shader.SetInt("channelSelector", ImageEditor.SelectedChannelIndex);

            //Determine what texture to display by type
            if (texture.RenderableTex is GLTextureCube)
            {
                GL.ActiveTexture(TextureUnit.Texture3);
                BindTexture(texture);   
                shader.SetInt("textureCubeInput", 3);
                shader.SetInt("textureType", 2);
            }
            else if (texture.RenderableTex is GLTexture2DArray)
            {
                GL.ActiveTexture(TextureUnit.Texture2);
                BindTexture(texture);
                shader.SetInt("textureArrayInput", 2);
                shader.SetInt("textureType", 1);
            }
            else
            {
                GL.ActiveTexture(TextureUnit.Texture1);
                BindTexture(texture);
                shader.SetInt("textureInput", 1);
                shader.SetInt("textureType", 0);
            }
            shader.SetInt("hasTexture", 1);

            //Draw background
            QuadDrawer.Draw(shader);
        }

        private void DrawBackground(ShaderProgram shader)
        {
            var backgroundTexture = IconManager.GetTextureIcon("CHECKERBOARD");

            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, backgroundTexture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            shader.SetInt("backgroundTexture", 1);
            shader.SetInt("backgroundMode", (int)ImageEditor.Settings.SelectedBackground);
            shader.SetVector4("backgroundColor", new Vector4(1));
            if (ImageEditor.Settings.SelectedBackground == ImageEditorSettings.BackgroundType.Custom)
            shader.SetVector4("backgroundColor", new Vector4(
                ImageEditor.Settings.BackgroundColor.X, 
                ImageEditor.Settings.BackgroundColor.Y, 
                ImageEditor.Settings.BackgroundColor.Z,
                ImageEditor.Settings.BackgroundColor.W));

            shader.SetInt("hasTexture", 0);
            shader.SetVector2("scale", new Vector2(30));
            shader.SetVector2("texCoordScale", new Vector2(30));

            shader.SetVector4("uColor", new Vector4(1));

            //Draw background
            QuadDrawer.Draw(shader);
        }

        private Vector3 UpdateAspectScale(Vector3 scale, int width, int height, STGenericTexture tex)
        {
            //Adjust scale via aspect ratio
            if (width > height)
            {
                float aspect = (float)tex.Width / (float)tex.Height;
                scale.X *= aspect;
            }
            else
            {
                float aspect = (float)tex.Height / (float)tex.Width;
                scale.Y *= aspect;
            }
            return scale;
        }

        private void BindTexture(STGenericTexture tex)
        {
            if (tex == null)
                return;

            if (tex.RenderableTex == null)
                tex.LoadRenderableTexture();

            var target = ((GLTexture)tex.RenderableTex).Target;
            var texID = tex.RenderableTex.ID;

            GL.BindTexture(target, texID);

            //Fixed mip layer with nearest setting
            //Only use mipmaps if used in rendered texture
            if (tex.MipCount > 1)
                GL.TexParameter(target, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.NearestMipmapNearest);
            else
                GL.TexParameter(target, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);

            GL.TexParameter(target, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(target, TextureParameterName.TextureMaxLod, (int)15);
            GL.TexParameter(target, TextureParameterName.TextureMinLod, 0);

            //RGBA channel swizzles
            int[] mask = new int[4] { (int)All.Red, (int)All.Green, (int)All.Blue, (int)All.Alpha };
            if (ImageEditor.Settings.UseChannelComponents)
            {
                mask = new int[4]
                {
                    OpenGLHelper.GetSwizzle(tex.RedChannel),
                    OpenGLHelper.GetSwizzle(tex.GreenChannel),
                    OpenGLHelper.GetSwizzle(tex.BlueChannel),
                    //For now prevent full disappearance of zero alpha types on alpha channel.
                    //This is typically used on BC4 and BC5 types when not using alpha data.
                    tex.AlphaChannel == STChannelType.Zero ? 1 : OpenGLHelper.GetSwizzle(tex.AlphaChannel),
                };
            }

            GL.TexParameter(target, TextureParameterName.TextureSwizzleRgba, mask);
        }
    }
}
