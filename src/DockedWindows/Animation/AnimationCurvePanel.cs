using CurveEditorLibrary;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.Animations;

namespace MapStudio.UI
{
    /// <summary>
    /// A panel for handling curve editing. 
    /// </summary>
    public class AnimationCurvePanel
    {
        //curve drawer/editor
        AnimCurveEditor CurveEditor;
        //background with timeline controller
        AnimationTimelineControl TimelineBG { get; set; }

        private bool updateTimelineRender = true;
        private bool onEnter;
        private bool _mouseDown;
        private float previousMouseWheel = 0;

        public AnimationCurvePanel()
        {
            TimelineBG = new AnimationTimelineControl();
            TimelineBG.ValueEditor = true; //value editor for up/down values
            TimelineBG.OnLoad();
            TimelineBG.BackColor = System.Drawing.Color.FromArgb(40, 40, 40, 40);

            CurveEditor = new AnimCurveEditor(TimelineBG);
        }

        //Sets the track to use in curve editor
        public void SetTrack(STAnimation anim, STAnimationTrack track)
        {
            CurveEditor.OnTrackSelect(anim, track);
        }

        //Sets the frame range
        public void SetFrame(float min, float max)
        {
            TimelineBG.SetFrameRangeMinMax(min, max);
        }

        //Sets the value range
        public void SetValue(float min, float max)
        {
            TimelineBG.SetValueRange(min, max);
        }

        public void Render()
        {
            var size = ImGui.GetWindowSize();
            var pos = ImGui.GetCursorPos();
            var viewerSize = size;
            //resize timeline by viewport window
            if (TimelineBG.Width != viewerSize.X || TimelineBG.Height != viewerSize.Y)
            {
                TimelineBG.Width = (int)viewerSize.X;
                TimelineBG.Height = (int)viewerSize.Y;
                TimelineBG.Resize();
                updateTimelineRender = true;
            }

            //Set background color
            var backgroundColor = ImGui.GetStyle().Colors[(int)ImGuiCol.MenuBarBg];
            TimelineBG.BGColor = new Vector4(backgroundColor.X, backgroundColor.Y, backgroundColor.Z, 1.0f);

            //Set curve mouse events during focus, hover or when mouse is held down
            if (ImGui.IsWindowHovered() && ImGui.IsWindowFocused() || _mouseDown)
                UpdateCurveEvents();

            //update render if needed
            if (updateTimelineRender)
            {
                TimelineBG.Render();
                updateTimelineRender = false;
            }

            //Display background
            var id = TimelineBG.GetTextureID();
            ImGui.Image((IntPtr)id, viewerSize,
                new System.Numerics.Vector2(0, 1),
                new System.Numerics.Vector2(1, 0));

            //Display frame/value text
            ImGui.SetCursorPos(pos);
            TimelineBG.DrawText();
            ImGui.SetCursorPos(pos);

            //Display curve editor
            CurveEditor.Render();
        }

        public void OnKeyDown(GLFrameworkEngine.KeyEventInfo state)
        {
            CurveEditor.OnKeyDown(state);
        }

        private void UpdateCurveEvents()
        {
            var mouseInfo = ImGuiHelper.CreateMouseState();

            bool controlDown = ImGui.GetIO().KeyCtrl;
            bool shiftDown = ImGui.GetIO().KeyShift;

            if (onEnter)
            {
                CurveEditor.ResetMouse(mouseInfo);
                TimelineBG.ResetMouse(mouseInfo);
                onEnter = false;
            }

            if (ImGui.IsAnyMouseDown() && !_mouseDown)
            {
                CurveEditor.OnMouseDown(mouseInfo);
                TimelineBG.OnMouseDown(mouseInfo);
                previousMouseWheel = 0;
                _mouseDown = true;
            }

            if (ImGui.IsMouseReleased(ImGuiMouseButton.Left) ||
               ImGui.IsMouseReleased(ImGuiMouseButton.Right) ||
               ImGui.IsMouseReleased(ImGuiMouseButton.Middle))
            {
                CurveEditor.OnMouseUp(mouseInfo);
                TimelineBG.OnMouseUp(mouseInfo);
                _mouseDown = false;
            }

            if (previousMouseWheel == 0)
                previousMouseWheel = mouseInfo.WheelPrecise;

            mouseInfo.Delta = mouseInfo.WheelPrecise - previousMouseWheel;
            previousMouseWheel = mouseInfo.WheelPrecise;

            TimelineBG.OnMouseMove(mouseInfo);
            CurveEditor.OnMouseMove(mouseInfo);

            TimelineBG.OnMouseWheel(mouseInfo, controlDown, shiftDown);

            updateTimelineRender = true;
        }
    }
}
