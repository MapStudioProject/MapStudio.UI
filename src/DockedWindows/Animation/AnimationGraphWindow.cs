using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using ImGuiNET;
using CurveEditorLibrary;
using Toolbox.Core.Animations;
using UIFramework;
using Toolbox.Core;

namespace MapStudio.UI
{
    public class AnimationGraphWindow : DockWindow
    {
        public override string Name => "ANIMATION_GRAPH";
        //Only use child window scrolling
        public override ImGuiWindowFlags Flags => ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoScrollWithMouse;

        public static bool ShowConstants = true;

        public bool IsActive => AnimationPlayer.IsPlaying;
        public bool IsRecordMode = false;

        AnimationPlayer AnimationPlayer { get; set; }
        AnimationTimelineControl TimelineBackground { get; set; }
        DopeSheetEditor DopeSheet;
        AnimCurveEditor AnimCurveEditor;
        public AnimationTree AnimationHierarchy { get; set; }

        public AnimationProperties PropertyWindow;

        public bool IsEditorDopeSheet = true;

        public STAnimation ActiveAnimation;

        float hierachySize = 0;

        private bool _mouseDown;
        private bool onEnter = false;

        private bool updateTimelineRender = true;

        public AnimationGraphWindow(DockSpaceWindow parent) : base(parent)
        {
            Init();
        }

        private void Init()
        {
            hierachySize = 200;
            PropertyWindow = new AnimationProperties();
            AnimationPlayer = new AnimationPlayer();
            TimelineBackground = new AnimationTimelineControl();
            AnimationHierarchy = new AnimationTree(PropertyWindow, TimelineBackground);
            PropertyWindow.Opened = false;

            TimelineBackground.OnLoad();
            TimelineBackground.BackColor = System.Drawing.Color.FromArgb(40, 40, 40, 40);
            AnimationPlayer.OnFrameChanged += delegate
            {
                if (TimelineBackground.CurrentFrame != (int)AnimationPlayer.CurrentFrame)
                    TimelineBackground.CurrentFrame = (int)AnimationPlayer.CurrentFrame;
            };

            TimelineBackground.OnFrameChanged += delegate {
                if (AnimationPlayer.CurrentFrame != TimelineBackground.CurrentFrame)
                    AnimationPlayer.SetFrame(TimelineBackground.CurrentFrame);
            };
            TimelineBackground.OnFrameCountChanged += delegate {
                AnimationPlayer.FrameCount = TimelineBackground.FrameCount;
                if (ActiveAnimation != null)
                    ActiveAnimation.FrameCount = TimelineBackground.FrameCount;
            };
            DopeSheet = new DopeSheetEditor(TimelineBackground, AnimationHierarchy);
            AnimCurveEditor = new AnimCurveEditor(TimelineBackground, AnimationHierarchy);
            AnimCurveEditor.TrackSelected += delegate
            {
                updateTimelineRender = true;
            };
            AnimCurveEditor.OnValueUpdated += delegate
            {
                AnimationPlayer.SetFrame(AnimationPlayer.CurrentFrame);
            };
            AnimationHierarchy.OnValueUpdated += delegate
            {
                AnimationPlayer.SetFrame(AnimationPlayer.CurrentFrame);
            };
            AnimationHierarchy.OnFrameCountUpdated += delegate
            {
                if (ActiveAnimation != null)
                {
                    TimelineBackground.FrameCount = (int)ActiveAnimation.FrameCount;
                    AnimationPlayer.FrameCount = ActiveAnimation.FrameCount;
                    TimelineBackground.SetFrameRange(ActiveAnimation.FrameCount);
                    //Update frame display
                    TimelineBackground.CurrentFrame = (int)ActiveAnimation.Frame;
                    AnimationPlayer.SetFrame(ActiveAnimation.Frame);
                    updateTimelineRender = true;
                }
            };
            TimelineBackground.ValueEditor = false;

            //Dock settings
            this.DockDirection = ImGuiDir.Down;
            this.SplitRatio = 0.3f;
            this.Opened = true;
        }

        public void ResetAnimations()
        {
            AnimationPlayer.Reset();
            updateTimelineRender = true;
            AnimationHierarchy.TreeView.Nodes.Clear();
            ActiveAnimation = null;

            GLFrameworkEngine.GLContext.ActiveContext.Camera.ResetAnimations();
        }

        public void ClearAnimations()
        {
            AnimationPlayer.CurrentAnimations.Clear();
            AnimationPlayer.StartFrame = 0;
            AnimationPlayer.CurrentFrame = 0;
            AnimationPlayer.ResetModels();

            updateTimelineRender = true;
        }

        public void SetFrame(float frame)
        {
            AnimationPlayer.SetFrame(frame);
        }

        public void Update() { updateTimelineRender = true; }

        public void Stop() { AnimationPlayer.Stop(); }

        public void Reset()
        {
            TimelineBackground.CurrentFrame = 0;
            TimelineBackground.FrameCount = 1;
            AnimationPlayer.Reset(true);

            updateTimelineRender = true;
        }

        public void AddAnimation(STAnimation animation, bool reset = true)
        {
            ActiveAnimation = animation;

            AnimationPlayer.AddAnimation(animation, "", reset);

            DopeSheet.Reset();
            AnimCurveEditor.Reset();
            AnimationHierarchy.Load(new List<STAnimation>() { animation });
            //Todo, high frame counts can cause freeze issues atm
            TimelineBackground.SetFrameRange(AnimationPlayer.FrameCount, 10);
            AnimationPlayer.SetFrame(AnimationPlayer.CurrentFrame);

            updateTimelineRender = true;
        }

        public override void Render()
        {
            if (ImGui.BeginMenuBar())
            {
                ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4());

                float height = ImGui.GetFrameHeight();
                Vector2 menuSize = new Vector2(height, height);

                if (IsRecordMode)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, ThemeHandler.Theme.Error);
                    if (ImGui.Button($"  {'\uf8d9'}  ", menuSize))
                        IsRecordMode = !IsRecordMode;
                    ImGui.PopStyleColor();
                }
                else
                {
                    if (ImGui.Button($"  {'\uf8d9'}  ", menuSize))
                        IsRecordMode = !IsRecordMode;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Records edited keyable values to activate animation");

                if (ImGui.Button($"  {IconManager.RESET_ICON}  ", menuSize))
                    ResetAnimations();

                if (ImGui.IsItemHovered()) ImGui.SetTooltip(TranslationSource.GetText("RESET"));

                if (ImGui.Button($"  {IconManager.PREV_FAST_FORWARD_ICON}  ", menuSize))
                    AdvanceFirstKeyFrame();

                if (ImGui.IsItemHovered()) ImGui.SetTooltip("First frame");

                if (ImGui.Button($"  {IconManager.PREV_STEP_ICON}  ", menuSize))
                    AdvancePreviousKeyFrame();

                if (ImGui.IsItemHovered()) ImGui.SetTooltip("Previous frame");

                if (!AnimationPlayer.IsPlaying)
                {
                    if (ImGui.Button($"  {IconManager.PLAY_ICON}  ", menuSize))
                        AnimationPlayer.Play();
                }
                else
                {
                    if (ImGui.Button($"  {IconManager.PAUSE_ICON}  ", menuSize))
                        AnimationPlayer.Pause();
                }

                if (ImGui.IsItemHovered()) ImGui.SetTooltip($"{(AnimationPlayer.IsPlaying ? "Stop" : "Play")}");

                if (ImGui.Button($"  {IconManager.NEXT_STEP_ICON}  ", menuSize))
                    AdvanceNextKeyFrame();

                if (ImGui.IsItemHovered()) ImGui.SetTooltip("Next frame");

                if (ImGui.Button($"  {IconManager.NEXT_FAST_FORWARD_ICON}  ", menuSize))
                    AdvanceLastKeyFrame();

                if (ImGui.IsItemHovered()) ImGui.SetTooltip("Last frame");

                ImGui.PopStyleColor();

                ImGui.SetNextItemWidth(100);

                float frame = AnimationPlayer.CurrentFrame;
                if (ImGui.DragFloat(TranslationSource.GetText("##FRAME"), ref frame, 1, 0)) {
                    UpdateCurrentFrame(frame);
                }

                if (ActiveAnimation != null)
                {
                    float frameCount = ActiveAnimation.FrameCount;

                    ImGui.SetNextItemWidth(100);
                    if (ImGui.DragFloat(TranslationSource.GetText("##FRAME_COUNT"), ref frameCount, 1, 0))
                    {
                        frameCount = MathF.Max(frameCount, 1.0f);

                        ActiveAnimation.FrameCount = frameCount;
                        AnimationPlayer.FrameCount = frameCount;
                        TimelineBackground.SetFrameRange(frameCount);
                        updateTimelineRender = true;
                    }
                    bool loop = ActiveAnimation.Loop;

                    ImGui.SetNextItemWidth(100);
                    if (ImGui.Checkbox(TranslationSource.GetText("LOOP"), ref loop)) {
                        ActiveAnimation.Loop = loop;
                    }
                }

                if (ImGui.Checkbox(TranslationSource.GetText("SHOW_CONSTANTS"), ref AnimationGraphWindow.ShowConstants))
                {
                }
                
                ImGui.SetCursorPosX(ImGui.GetWindowWidth() - 170);

                ImGui.SetNextItemWidth(120);
                if (ImGui.DragFloat(TranslationSource.GetText("FPS"), ref AnimationPlayer.FrameRate, 1, 1, 240)) {
                    AnimationPlayer.UpdateFramerate();
                }

                ImGui.EndMenuBar();
            }

            var size = ImGui.GetWindowSize();
            var posY = ImGui.GetCursorPosY();

            ImGui.Columns(2);
            hierachySize = ImGui.GetColumnWidth(0);
            var propSize = ImGui.GetColumnWidth(2);

            if (ImGui.BeginChild("animation_hierarchy1", new System.Numerics.Vector2(hierachySize, size.Y - posY - 25), true))
            {
                AnimationHierarchy.Render();
            }
            ImGui.EndChild();

            DrawTimelineEditorSwitch();

            ImGui.NextColumn();

            float propertyMenuHeight =!IsEditorDopeSheet ? 50 : 0;

            if (!IsEditorDopeSheet)
            {
                if (ImGui.BeginChild("timeline_menu", new System.Numerics.Vector2(ImGui.GetColumnWidth(), 22), false, 
                    ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.MenuBar))
                {
                    AnimCurveEditor.RenderMenu();
                }
                ImGui.EndChild();
            }

            if (ImGui.BeginChild("timeline_child1", new System.Numerics.Vector2(ImGui.GetColumnWidth(), size.Y - posY - propertyMenuHeight - 22), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                DrawDopeSheetTimeline();
            }
            ImGui.EndChild();

            if (propertyMenuHeight > 0)
            {
                if (ImGui.BeginChild("properties_menu", new System.Numerics.Vector2(ImGui.GetColumnWidth(), propertyMenuHeight), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
                {
                    DrawPropertiesMenu();
                }
                ImGui.EndChild();
            }

            ImGui.NextColumn();
        }

        private void DrawTimelineEditorSwitch()
        {
            //Place the cursor bottom right of the tree
            //Draw buttons to switch editors
            var selectColor = ImGui.GetStyle().Colors[(int)ImGuiCol.Header];
            var buttonColor = ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBg];
            var textColor = ImGui.GetStyle().Colors[(int)ImGuiCol.Text];
            var disabledColor = ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled];

            var buttonWidth = hierachySize / 2;

            bool DrawButton(string name, bool selected)
            {
                var color = selected ? selectColor : buttonColor;

                ImGui.PushStyleColor(ImGuiCol.Text, selected ? textColor : disabledColor);
                ImGui.PushStyleColor(ImGuiCol.Button, color);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, color);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, color);
                ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 1);

                bool clicked = ImGui.Button(name, new Vector2(buttonWidth, 21));

                ImGui.PopStyleColor(4);
                ImGui.PopStyleVar();

                return clicked;
            }

            if (DrawButton("Dope Sheet", IsEditorDopeSheet))
            {
                IsEditorDopeSheet = true;
                TimelineBackground.ValueEditor = false;
                updateTimelineRender = true;
            }

            ImGui.SameLine();

            if (DrawButton("Curve Editor", !IsEditorDopeSheet))
            {
                IsEditorDopeSheet = false;
                TimelineBackground.ValueEditor = true;
                updateTimelineRender = true;
            }
        }

        private void DrawTrackProperties()
        {
            var track = this.AnimCurveEditor.SelectedTracks.FirstOrDefault();
            if (track == null)
                return;

            ImguiPropertyColumn.Begin("propertiesTrack");

            ImguiPropertyColumn.Combo<STInterpoaltionType>("Interpolation", track.Track, "InterpolationType");
            ImguiPropertyColumn.Combo<STLoopMode>("WrapMode", track.Track, "WrapMode");

            ImguiPropertyColumn.End();
        }

        private void DrawPropertiesMenu()
        {
            ImguiPropertyColumn.Begin("properties", 5, false);

            var selected = IsEditorDopeSheet ? this.DopeSheet.SelectedKeys : this.AnimCurveEditor.SelectedKeys;
            var selectedKey = selected.FirstOrDefault();

            if (selectedKey == null)
            {
                ImguiPropertyColumn.End();
                return;
            }

            float frame = selectedKey.Frame;
            float value = selectedKey.KeyFrame.Value;
            var track = selectedKey.GetTrack();
            var keyFrame = selectedKey.KeyFrame;

            ImGuiHelper.BoldText("Interpolation"); ImGui.NextColumn();
            ImGuiHelper.BoldText("Frame"); ImGui.NextColumn();
            ImGuiHelper.BoldText("Value"); ImGui.NextColumn();
            ImGuiHelper.BoldText("Slope In"); ImGui.NextColumn();
            ImGuiHelper.BoldText("Slope Out"); ImGui.NextColumn();

            ImguiPropertyColumn.Combo<STInterpoaltionType>("Interpolation", track, "InterpolationType");

            if (ImguiPropertyColumn.DragFloat("Frame", ref frame, 1f))
                selectedKey.Frame = frame;

            if (ImguiPropertyColumn.DragFloat("Value", ref value, 1f))
                selectedKey.Value = value;

            if (keyFrame is STHermiteKeyFrame)
            {
                var tangentIn = selectedKey.SlopeIn;
                var tangentOut = selectedKey.SlopeOut;

                if (ImguiPropertyColumn.DragFloat("Slope In", ref tangentIn, 0.01f))
                    selectedKey.SlopeIn = tangentIn;

                if (ImguiPropertyColumn.DragFloat("Slope Out", ref tangentOut, 0.01f))
                    selectedKey.SlopeOut = tangentOut;
            }

         //   ImguiPropertyColumn.Combo<STLoopMode>("WrapMode", track, "WrapMode");
        }

        private void DrawProperties()
        {
            ImguiPropertyColumn.Begin("properties");

            var selected = IsEditorDopeSheet ? this.DopeSheet.SelectedKeys : this.AnimCurveEditor.SelectedKeys;
            var selectedKey = selected.FirstOrDefault();

            if (!this.IsEditorDopeSheet)
            {
                var selectedTrack = this.AnimCurveEditor.SelectedTracks.FirstOrDefault();
                if (selectedTrack != null)
                {
                    ImGui.Text("Selected Key");
                    ImGui.NextColumn();

                    ImGui.PushItemWidth(ImGui.GetColumnWidth(1) - 15);

                    string key = selectedKey != null ? $"Key{selectedTrack.Keys.IndexOf(selectedKey)}" : $"None";
                    if (ImGui.BeginCombo("##SelectedKey", key))
                    {
                        int index = 0;
                        foreach (var k in selectedTrack.Keys)
                        {
                            bool select = k == selectedKey;
                            if (ImGui.Selectable($"Key{index++}", select))
                            {
                                this.AnimCurveEditor.DeselectAll();
                                k.IsSelected = true;
                                this.AnimCurveEditor.SelectedKeys.Add(k);
                                this.AnimCurveEditor.SelectionChanged();
                            }
                        }
                        ImGui.EndCombo();
                    }

                    ImGui.PopItemWidth();

                    ImGui.NextColumn();
                }
            }

            if (selectedKey == null)
            {
                ImguiPropertyColumn.End();
                return;
            }

            float frame = selectedKey.Frame;
            float value = selectedKey.KeyFrame.Value;
            var track = selectedKey.GetTrack();
            var keyFrame = selectedKey.KeyFrame;

            if (ImguiPropertyColumn.DragFloat("Frame", ref frame, 1f))
                selectedKey.Frame = frame;

            if (ImguiPropertyColumn.DragFloat("Value", ref value, 1f))
                selectedKey.Value = value;

            if (keyFrame is STHermiteKeyFrame)
            {
                var tangentIn = selectedKey.SlopeIn;
                var tangentOut = selectedKey.SlopeOut;

                if (ImguiPropertyColumn.DragFloat("Slope In", ref tangentIn, 0.01f))
                    selectedKey.SlopeIn = tangentIn;

                if (ImguiPropertyColumn.DragFloat("Slope Out", ref tangentOut, 0.01f))
                    selectedKey.SlopeOut = tangentOut;
            }
            if (keyFrame is STLinearKeyFrame)
            {
                var delta = ((STLinearKeyFrame)keyFrame).Delta;

                if (ImguiPropertyColumn.DragFloat("Delta Out", ref delta, 0.01f))
                    ((STLinearKeyFrame)keyFrame).Delta = delta;
            }

            ImGui.EndColumns();
        }

        private void AdvanceLastKeyFrame()
        {
            if (ActiveAnimation == null)
                return;

            List<STKeyFrame> keys = GetAllKeys();
            var prevKey = keys.LastOrDefault();
            if (prevKey != null)
                UpdateCurrentFrame(prevKey.Frame);
            else
                UpdateCurrentFrame(ActiveAnimation.FrameCount);
        }

        private void AdvanceFirstKeyFrame()
        {
            if (ActiveAnimation == null)
                return;

            List<STKeyFrame> keys = GetAllKeys();
            //Advance to previous key in track
            var nextKey = keys.FirstOrDefault();
            if (nextKey != null)
                UpdateCurrentFrame(nextKey.Frame);
            else
                UpdateCurrentFrame(0);
        }

        private List<STKeyFrame> GetAllKeys()
        {
            List<STKeyFrame> keys = new List<STKeyFrame>();
            foreach (var group in ActiveAnimation.AnimGroups)
                GetAllKeys(group, ref keys);
            return keys.OrderBy(x => x.Frame).ToList();
        }

        static void GetAllKeys(STAnimGroup group, ref List<STKeyFrame> keys)
        {
            foreach (var track in group.GetTracks()) {
                keys.AddRange(track.KeyFrames);
            }

            foreach (var subGroup in group.SubAnimGroups)
                GetAllKeys(subGroup, ref keys);
        }

        private void AdvancePreviousKeyFrame()
        {
            if (ActiveAnimation == null)
                return;

            List<STKeyFrame> keys = GetAllKeys();
            var frame = AnimationPlayer.CurrentFrame;
            var prevKey = keys.LastOrDefault(x => (int)x.Frame < frame);
            if (prevKey != null)
                UpdateCurrentFrame(prevKey.Frame);
        }

        private void AdvanceNextKeyFrame()
        {
            if (ActiveAnimation == null)
                return;

            List<STKeyFrame> keys = GetAllKeys();

            var frame = AnimationPlayer.CurrentFrame;
            var nextKey = keys.FirstOrDefault(x => (int)x.Frame > frame);
            if (nextKey != null)
                UpdateCurrentFrame(nextKey.Frame);
        }

        private void UpdateCurrentFrame(float frame)
        {
            AnimationPlayer.SetFrame(frame);
            TimelineBackground.CurrentFrame = (int)frame;
            updateTimelineRender = true;
        }

        private void DrawDopeSheetTimeline()
        {
            var size = ImGui.GetWindowSize();
            var pos = ImGui.GetCursorPos();
            var viewerSize = size - pos;
            if (TimelineBackground.Width != viewerSize.X || TimelineBackground.Height != viewerSize.Y)
            {
                TimelineBackground.Width = (int)viewerSize.X;
                TimelineBackground.Height = (int)viewerSize.Y;
                TimelineBackground.Resize();
                updateTimelineRender = true;
            }

            if (TimelineBackground.FrameCount != AnimationPlayer.FrameCount)
                TimelineBackground.FrameCount = (int)AnimationPlayer.FrameCount;

            var backgroundColor = ImGui.GetStyle().Colors[(int)ImGuiCol.MenuBarBg];
            TimelineBackground.BGColor = new Vector4(backgroundColor.X, backgroundColor.Y, backgroundColor.Z, 1.0f);

            if (ImGui.IsWindowHovered() && ImGui.IsWindowFocused() || _mouseDown)
                UpdateCurveEvents();
            else
            {
                previousMouseWheel = 0;
                onEnter = true;
            }

            if (updateTimelineRender || AnimationPlayer.IsPlaying)
            {
                TimelineBackground.Render();
                updateTimelineRender = false;
            }

            var id = TimelineBackground.GetTextureID();
            ImGui.Image((IntPtr)id, viewerSize,
                new System.Numerics.Vector2(0, 1),
                new System.Numerics.Vector2(1, 0));

            ImGui.SetCursorPos(pos);
            TimelineBackground.DrawText();
            ImGui.SetCursorPos(pos);

            if (IsEditorDopeSheet)
                DopeSheet.Render();
            else
                AnimCurveEditor.Render();
        }

        public void OnKeyDown(GLFrameworkEngine.KeyEventInfo state)
        {
            if (IsEditorDopeSheet)
                DopeSheet.OnKeyDown(state);
            else
                AnimCurveEditor.OnKeyDown(state);
        }

        private float previousMouseWheel;

        private void UpdateCurveEvents()
        {
            var mouseInfo = ImGuiHelper.CreateMouseState();

            bool controlDown = ImGui.GetIO().KeyCtrl;
            bool shiftDown = ImGui.GetIO().KeyShift;

            if (onEnter)
            {
                if (IsEditorDopeSheet)
                    DopeSheet.ResetMouse(mouseInfo);
                else
                    AnimCurveEditor.ResetMouse(mouseInfo);

                TimelineBackground.ResetMouse(mouseInfo);
                onEnter = false;
            }

            if (ImGui.IsAnyMouseDown() && !_mouseDown)
            {
                if (IsEditorDopeSheet)
                    DopeSheet.OnMouseDown(mouseInfo);
                else
                    AnimCurveEditor.OnMouseDown(mouseInfo);
                TimelineBackground.OnMouseDown(mouseInfo);
                previousMouseWheel = 0;
                _mouseDown = true;
            }

            if (ImGui.IsMouseReleased(ImGuiMouseButton.Left) ||
               ImGui.IsMouseReleased(ImGuiMouseButton.Right) ||
               ImGui.IsMouseReleased(ImGuiMouseButton.Middle))
            {
                if (IsEditorDopeSheet)
                    DopeSheet.OnMouseUp(mouseInfo);
                else
                    AnimCurveEditor.OnMouseUp(mouseInfo);

                TimelineBackground.OnMouseUp(mouseInfo);
                _mouseDown = false;
            }

            if (previousMouseWheel == 0)
                previousMouseWheel = mouseInfo.WheelPrecise;

            mouseInfo.Delta = mouseInfo.WheelPrecise - previousMouseWheel;
            previousMouseWheel = mouseInfo.WheelPrecise;

            //  if (_mouseDown)
            TimelineBackground.OnMouseMove(mouseInfo);

            if (IsEditorDopeSheet)
                DopeSheet.OnMouseMove(mouseInfo);
            else
                AnimCurveEditor.OnMouseMove(mouseInfo);

            TimelineBackground.OnMouseWheel(mouseInfo, controlDown, shiftDown);

            updateTimelineRender = true;
        }

        public void Dispose()
        {
            AnimationHierarchy.Dispose();
            AnimationPlayer.Stop();
            AnimationPlayer.Dispose();
            ClearAnimations();
        }
    }
}
