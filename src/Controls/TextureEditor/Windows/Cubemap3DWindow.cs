using GLFrameworkEngine;
using ImGuiNET;
using IONET.Collada.FX.Rendering;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UIFramework;

namespace MapStudio.UI
{
    public class Cubemap3DWindow : Window
    {
        public bool HDREncodeAlpha = true;
        public float Gamma = 2.2f;

        //fbo to render viewport
        private Framebuffer Framebuffer;

        private SphereRender Sphere;

        private GLContext Context;

        private GLTexture Texture;

        public Cubemap3DWindow() : base ("Cubemap Viewer", new System.Numerics.Vector2(500, 500))
        {

        }

        public void Load(GLTexture texture)
        {
            Texture = texture;
        }

        private void Init()
        {
            Framebuffer = new Framebuffer(FramebufferTarget.Framebuffer, 32, 32);
            Sphere = new SphereRender();
            Context = new GLContext();
            Context.Camera = new Camera();
            Context.Camera.TargetPosition = new OpenTK.Vector3(0);
            Context.Camera.TargetDistance = 0;
            Context.Camera.ZNear = 0.01f;
            Context.Camera.FovDegrees = 90;
            Context.Camera.UpdateMatrices();
        }

        public override void Render()
        {
            if (Framebuffer == null)
                Init();

            var size = ImGui.GetWindowSize();

            if (ImGui.IsWindowFocused())
                UpdateCamera(Context);

            Draw();

            var tex = (GLTexture)Framebuffer.Attachments[0];
            ImGui.Image(tex.ID, new System.Numerics.Vector2(size.X, size.Y - 28),
                new System.Numerics.Vector2(0, 1),
                new System.Numerics.Vector2(1, 0));
        }

        private bool onEnter = false;
        private bool _mouseDown = false;

        private void UpdateCamera(GLContext context)
        {
            var mouseInfo = InputState.CreateMouseState();
            var keyInfo = InputState.CreateKeyState();
            KeyEventInfo.State = keyInfo;

            if (ImGui.IsAnyMouseDown() && !_mouseDown)
            {
                context.OnMouseDown(mouseInfo, keyInfo);
                _mouseDown = true;

                Workspace.ActiveWorkspace?.OnMouseDown(mouseInfo);
            }

            if (ImGui.IsMouseReleased(ImGuiMouseButton.Left) ||
               ImGui.IsMouseReleased(ImGuiMouseButton.Right) ||
               ImGui.IsMouseReleased(ImGuiMouseButton.Middle))
            {
                context.OnMouseUp(mouseInfo);
                _mouseDown = false;
            }

            context.OnMouseMove(mouseInfo, keyInfo, _mouseDown);

            if (!(ImGuiController.ApplicationHasFocus && IsFocused))
                context.ResetPrevious();

            if (this.IsFocused)
                context.Camera.Controller.KeyPress(keyInfo);
        }

        private void Draw()
        {
            var size = ImGui.GetWindowSize();
            if (Framebuffer.Width != (int)size.X || Framebuffer.Height != (int)size.Y)
            {
                Framebuffer.Resize((int)size.X, (int)size.Y);
                Context.Camera.Width = (int)size.X;
                Context.Camera.Height = (int)size.Y;
                Context.Camera.UpdateMatrices();
            }

            Framebuffer.Bind();

            GL.Disable(EnableCap.CullFace);

            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Lequal);
            GL.Enable(EnableCap.TextureCubeMapSeamless);

            GL.ClearColor(0, 0, 0, 1);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.Viewport(0, 0, Framebuffer.Width, Framebuffer.Height);

            var shader = GlobalShaders.GetShader("CUBEMAP_VIEW");
            Context.CurrentShader = shader;



            var projectionMatrix = Context.Camera.ProjectionMatrix;
            var viewMatrix = Context.Camera.ViewMatrix;

            shader.SetInt("textureType", 0);
            shader.SetMatrix4x4("projection", ref projectionMatrix);
            shader.SetMatrix4x4("view", ref viewMatrix);

            if (Texture is GLTexture2DArray)
            {
                shader.SetInt("textureType", 1);
                shader.SetTexture(Texture, "textureArray", 1);
            }
            else if (Texture is GLTextureCube)
            {
                shader.SetInt("textureType", 2);
                shader.SetTexture(Texture, "cubemapTexture", 2);
            }
            else if (Texture is GLTextureCubeArray)
            {
                shader.SetInt("textureType", 3);
                shader.SetTexture(Texture, "cubemapArrayTexture", 3);
            }

            shader.SetBoolToInt("hdrEncoded", HDREncodeAlpha);
            if (HDREncodeAlpha)
            {
                shader.SetFloat("gamma", Gamma);
                shader.SetFloat("scale", 4f);
                shader.SetFloat("range", 1024f);
            }

            RenderTools.DrawCube();

            Framebuffer.Unbind();

            GL.Enable(EnableCap.CullFace);
            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Lequal);
        }
    }
}
