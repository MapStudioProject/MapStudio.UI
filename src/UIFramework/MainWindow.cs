using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using System.ComponentModel;
using OpenTK;
using OpenTK.Input;
using System.Drawing;
using System.Runtime.InteropServices;

namespace UIFramework
{
    #region WINAPI
    [DllImport("dwmapi.dll", SetLastError = true)]
    private static extern bool DwmSetWindowAttribute(IntPtr handle, int param, in int value, int size);


    [DllImport("uxtheme.dll", SetLastError = true)]
    private static extern bool SetWindowTheme(IntPtr handle, string? subAppName, string? subIDList);
    #endregion

    public class MainWindow : DockSpaceWindow
    {
        /// <summary>
        /// Windows attached to the main window.
        /// </summary>
        public List<Window> Windows = new List<Window>();

        /// <summary>
        /// The menu items on the top of the main window.
        /// </summary>
        public List<MenuItem> MenuItems = new List<MenuItem>();

        public static bool ForceFocus = true;

        //General window info
        protected static GameWindow _window;
        float font_scale = 1.0f;
        bool fullscreen = true;
        bool p_open = true;
        ImGuiDockNodeFlags dockspace_flags = ImGuiDockNodeFlags.None;

        public MainWindow() : base("dock_main")
        {
        }

        internal void Init(GameWindow window) {
            _window = window;
        }

        public void OnApplicationLoad()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                //Support dark mode on Windows
                //ported from https://github.com/libsdl-org/SDL/issues/4776#issuecomment-926976455

                SetWindowTheme(_window.WindowInfo.Handle, "DarkMode_Explorer", null);

                ToggleDarkmode(true);
            }


            
            this.Name = "WindowSpace";

            //Disable the docking buttons
            ImGui.GetStyle().WindowMenuButtonPosition = ImGuiDir.None;

            //Enable docking support
            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable;
            //Allow activating dragged controls as text input
            ImGui.GetIO().ConfigDragClickToInputText = true;
            //Enable up/down key navigation
            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
            //Only move via the title bar instead of the whole window
            ImGui.GetIO().ConfigWindowsMoveFromTitleBarOnly = true;

            //Load theme files
            MapStudio.UI.ThemeHandler.Load();
            
            OnLoad();
        }
        
        /// <summary>
        /// Enables/disables darkmode for this window (works only on Windows 10+)
        /// </summary>
        /// <param name="value"></param>
        public void ToggleDarkmode(bool value)
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                if (!DwmSetWindowAttribute(_window.WindowInfo.Handle, 20, value ? 1 : 0, sizeof(int)))
                    DwmSetWindowAttribute(_window.WindowInfo.Handle, 19, value ? 1 : 0, sizeof(int));
            }
        }

        public void OnRenderFrame()
        {
            var window_flags = ImGuiWindowFlags.NoDocking;

            if (fullscreen)
            {
                ImGuiViewportPtr viewport = ImGui.GetMainViewport();
                ImGui.SetNextWindowPos(viewport.WorkPos);
                ImGui.SetNextWindowSize(viewport.WorkSize);
                ImGui.SetNextWindowViewport(viewport.ID);
                ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0.0f);
                ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0.0f);
                window_flags |= ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove;
                window_flags |= ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus;
            }

            if ((dockspace_flags & ImGuiDockNodeFlags.PassthruCentralNode) != 0)
                window_flags |= ImGuiWindowFlags.NoBackground;

            ImGui.Begin("WindowSpace", ref p_open, window_flags);

            if (fullscreen)
                ImGui.PopStyleVar(2);

            Render();

            ImGui.End();
        }

        public override void Render()
        {
            if (ImGui.BeginMainMenuBar())
            {
                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new System.Numerics.Vector2(8, 6));
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(8, 4));
                ImGui.PushStyleColor(ImGuiCol.Separator, new System.Numerics.Vector4(0.4f, 0.4f, 0.4f, 1.0f));

                foreach (var item in MenuItems)
                    MapStudio.UI.ImGuiHelper.DrawMenuItem(item, false);

                MainMenuDraw();

                ImGui.PopStyleVar(2);
                ImGui.PopStyleColor(1);

                //Display FPS at right side of screen
                float width = ImGui.GetWindowWidth();
                float framerate = ImGui.GetIO().Framerate;

                ImGui.SetCursorPosX(width - 100);
                ImGui.Text($"({framerate:0.#} FPS)");

                ImGui.EndMainMenuBar();
            }

            var dock_id = ImGui.GetID("##DockspaceRoot");

            unsafe
            {
                //Create an inital dock space for docking workspaces.
                ImGui.DockSpace(dock_id, new System.Numerics.Vector2(0.0f, 0.0f), 0, window_class);
            }
            //base.Render();
        }

        public virtual void MainMenuDraw()
        {

        }

        public virtual void OnResize(int width, int height)
        {
        }

        public virtual void OnFileDrop(string fileName)
        {
        }

        public virtual void OnKeyDown(KeyboardKeyEventArgs e)
        {
        }

        public virtual void OnFocusedChanged()
        {
        }

        public virtual void OnClosing(CancelEventArgs e)
        {
        }
    }
}
