using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using ImGuiNET;
using Toolbox.Core.Animations;
using CurveEditorLibrary;
using GLFrameworkEngine;
using UIFramework;

namespace MapStudio.UI
{
    public class AnimCurveEditor
    {
        private string TrackName = "Select a track to edit";

        //Copy
        List<AnimationTree.KeyNode> CopiedKeys = new List<AnimationTree.KeyNode>();

        //Selection
        public List<AnimationTree.KeyNode> SelectedKeys = new List<AnimationTree.KeyNode>();
        public List<float> PreDragFrames = new List<float>();
        public List<float> PreDragValues = new List<float>();
        public List<float> PreDragInSlopes = new List<float>();
        public List<float> PreDragOutSlopes = new List<float>();

        //Undo/redo
        private UndoStack UndoStack = new UndoStack();

        //The boundings of the current selection
        private BoundingBox2D SelectionBounding;
        private float SelectedFrameMin;
        private float SelectedFrameMax;

        //The selection box tool
        private UIFramework.SelectionBox SelectionBox = new UIFramework.SelectionBox();

        //Dope sheet editor comprised of a tree and a timeline
        private AnimationTimelineControl Timeline;
        private AnimationTree Tree;

        private AnimationTree.TrackNode SelectedTrack;

        private ResizeMode _ResizeMode = ResizeMode.None;
        private ResizeOperation ResizeOp = null;

        private bool _IsMoving;

        //Movement
        private bool IsMoving
        {
            get { return _IsMoving; }
            set
            {
                if (_IsMoving != value)
                {
                    if (value)
                        SetupMoveOperation();

                    //A check for when the movement has just been set (for undo operations)
                    movementChanged = value;
                    _IsMoving = value;
                }
            }
        }

        Vector2 lastMousePos;
        Vector2 mouseDownPos;

        bool movementChanged = false;
        bool _mouseDown = false;

        public AnimCurveEditor(AnimationTimelineControl timeline, AnimationTree tree) {
            Timeline = timeline;
            Tree = tree;
            tree.OnRemoved += delegate
            {
                DeselectAll();
            };
            SelectionBounding = new BoundingBox2D(new Vector2(), new Vector2());
            SelectionBox.OnSelectionStart += delegate
            {
                DeselectAll();
            };
        }

        //deselects all keys currently selected
        private void DeselectAll()
        {
            foreach (var key in SelectedKeys)
            {
                key.SelectedByBox = false;
                key.IsSelected = false;
                key.IsTangentInSelected = false;
                key.IsTangentOutSelected = false;
            }
            SelectedKeys.Clear();
        }

        /// <summary>
        /// Clears all selected keys from the dope sheet
        /// </summary>
        public void Reset()
        {
            SelectedKeys.Clear();
            SelectedTrack = null;
            TrackName = "Select a track to edit";
        }

        public void OnTrackSelect(AnimationTree.TrackNode track)
        {
            TrackName = track.GetPath();

            if (track.Keys.Count == 0)
            {
                Timeline.SetValueRange(-5, 5);
                return;
            }

            var min = track.Keys.Max(x => x.KeyFrame.Value);
            var max = track.Keys.Min(x => x.KeyFrame.Value);

            Timeline.SetValueRange(min - 1, max + 1);
        }

        /// <summary>
        /// Draw the dope sheet.
        /// </summary>
        public void Render()
        {
            //Enable selection box unless moving
            SelectionBox.Enabled = IsMoving ? false : true;

            if (SelectedKeys.Count > 0)
            {
            }

            //Draw through the node hiearchy and draw the keyed tracks
            foreach (var node in Tree.TreeView.Nodes)
                DrawCurve(node);

            //Draw selection boundary
            DrawSelectionBounds();
            //Draw selection box
            if (Timeline.IsHoverOverCurrentFrameHandle && !SelectionBox.IsActive)
                return;

            if ((ImGui.IsWindowHovered() || SelectionBox.IsActive) && !ImGui.IsAnyItemHovered())
                SelectionBox.Render();
        }

        //Draws a box around the selected dope keys for transforming
        private void DrawSelectionBounds()
        {
            if (SelectedKeys.Count == 0)
                return;

            CalculateSelection();

            var minBox = SelectionBounding.Min;
            var maxBox = SelectionBounding.Max;

            ImGui.GetWindowDrawList().AddRectFilled(minBox, maxBox,
               ImGui.ColorConvertFloat4ToU32(new Vector4(0.5f, 0.5f, 0.5f, 0.5f)));
        }

        //Reset previous mouse position
        public void ResetMouse(MouseEventInfo mouse)
        {
            lastMousePos = new Vector2(mouse.X, mouse.Y);
        }

        private void DrawCurve(UIFramework.TreeNode node)
        {
            //Draw the dope sheet according to the tree hiearchy
            if (node.IsSelected && node is AnimationTree.TrackNode)
            {
                //Track node drawing
                var trackNode = node as AnimationTree.TrackNode;
                DrawTrackCurve(trackNode);
            }
            //Shift each position to match the current tree node hiearchy
            if (node.IsExpanded)
            {
                foreach (var c in node.Children)
                    DrawCurve(c);
            }
        }

        private void DrawTrackCurve(AnimationTree.TrackNode track)
        {
            //Timeline resized too small, don't display
            if (Timeline.Height - 40 <= 0)
                return;

            if (SelectedTrack != track)
            {
                SelectedTrack = track;
                OnTrackSelect(track);
            }

            //Check if the window has been clicked
            bool clicked = ImGui.IsMouseDown(0) && ImGui.IsWindowFocused() && ImGui.IsWindowHovered() && !ImGui.IsAnyItemHovered();
            //Get the position in screen coordinates for custom drawing
            var screenPos = ImGui.GetCursorScreenPos();
            //Total width from a frame value
            float frameWidth = Timeline.FramesToPixelsX(1f);
            //Starting cursor pos
            var curPos = ImGui.GetCursorPos();


            //Offset of the min frame range
            float offset = Timeline.FramesToPixelsX(Timeline.frameRangeMin);

            //Draw the full bar representing the tracks ranges
            var minBox = screenPos;
            var maxBox = screenPos + new Vector2(Timeline.Width, ImGui.GetFrameHeight());

            ImGui.GetWindowDrawList().AddRectFilled(minBox, maxBox,
               ImGui.ColorConvertFloat4ToU32(new Vector4(0.5f, 0.5f, 0.5f, 0.1f)));

            var valueLineColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0, 0, 1));
            var lineColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1));

            //Display interpolated first
            for (int i = 0; i < track.Anim.FrameCount; i++)
            {
                Vector2 GetPos(int frame)
                {
                    float value = track.Track.GetFrameValue(frame);

                    float posX = frame * frameWidth - offset;
                    float posY = Timeline.ValueToY(value);
                    return screenPos + new Vector2(posX, posY);
                }

                ImGui.GetWindowDrawList().AddLine(GetPos(i), GetPos(i + 1), valueLineColor);
            }

            //Draw keys and slopes
            foreach (var keyFrame in track.Keys)
            {
                if (track.Track.InterpolationType == STInterpoaltionType.Constant)
                    continue;

                var kf = keyFrame.KeyFrame;

                float height = ImGui.GetFrameHeight();
                var color = keyFrame.IsSelected ? new Vector4(1, 1, 0, 1.0f) : new Vector4(1);

                float posY = Timeline.ValueToY(keyFrame.KeyFrame.Value);

                //Draw at the frame position
                var pos = screenPos + new Vector2(keyFrame.Frame * frameWidth - offset, posY);
                //Circle to represent a keyed value
                ImGui.GetWindowDrawList().AddCircleFilled(
                    new Vector2(pos.X, pos.Y), 5,
                    ImGui.ColorConvertFloat4ToU32(color));
                //Add a border around the circle
                ImGui.GetWindowDrawList().AddCircle(new Vector2(pos.X, pos.Y), 5,
                ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 1)));
                //Hit detection for selecting keys
                float hitboxSize = 5;
                var min = new Vector2(pos.X - hitboxSize, pos.Y - hitboxSize);
                var max = new Vector2(pos.X + hitboxSize, pos.Y + hitboxSize);

                keyFrame.Min = min;
                keyFrame.Max = max;

                //Select via selection box
                if (SelectionBox.IsActive)
                {
                    SelectionBox.CheckFrameSelection(keyFrame, min, max);
                    if (keyFrame.IsSelected && !SelectedKeys.Contains(keyFrame))
                    {
                        keyFrame.SelectedByBox = true;
                        SelectedKeys.Add(keyFrame);
                        SelectionChanged();
                    }
                    if (!keyFrame.IsSelected && SelectedKeys.Contains(keyFrame))
                    {
                        SelectedKeys.Remove(keyFrame);
                        keyFrame.SelectedByBox = false;
                        SelectionChanged();
                    }
                } //Select via mouse click
                else if (!IsMoving && clicked && ImGui.IsMouseHoveringRect(min, max))
                {
                    if (!ImGui.GetIO().KeyCtrl && !keyFrame.SelectedByBox)
                    {
                        DeselectAll();
                        SelectionChanged();
                    }

                    SelectionBox.Enabled = false;

                    if (!SelectedKeys.Contains(keyFrame))
                    {
                        SelectedKeys.Add(keyFrame);
                        SelectionChanged();
                    }
                    keyFrame.IsSelected = true;
                    IsMoving = true;
                }

                float scaleFactorX = Timeline.FramesToPixelsX(1f);
                float scaleFactorY = -Timeline.ValueUnitsToPixelsY(1f);

                if (track.Track.InterpolationType == STInterpoaltionType.Hermite && kf is STHermiteKeyFrame)
                {
                    var slopeIn = keyFrame.SlopeIn;
                    var slopeOut = keyFrame.SlopeOut;
                    var tangentLength = 40f;

                    Vector2 cpIn = pos + Vector2.Normalize(new Vector2(scaleFactorX, slopeIn * scaleFactorY)) * -tangentLength;
                    Vector2 cpOut = pos + Vector2.Normalize(new Vector2(scaleFactorX, slopeOut * scaleFactorY)) * tangentLength;

                    min = new Vector2(cpIn.X - hitboxSize, cpIn.Y - hitboxSize);
                    max = new Vector2(cpIn.X + hitboxSize, cpIn.Y + hitboxSize);

                    //Select via mouse click
                    if (!IsMoving && clicked && ImGui.IsMouseHoveringRect(min, max))
                    {
                        if (!ImGui.GetIO().KeyCtrl && !keyFrame.SelectedByBox)
                        {
                            DeselectAll();
                            SelectionChanged();
                        }

                        SelectionBox.Enabled = false;

                        if (!SelectedKeys.Contains(keyFrame))
                        {
                            SelectedKeys.Add(keyFrame);
                            SelectionChanged();
                        }
                        keyFrame.IsTangentInSelected = true;
                        IsMoving = true;
                    }

                    min = new Vector2(cpOut.X - hitboxSize, cpOut.Y - hitboxSize);
                    max = new Vector2(cpOut.X + hitboxSize, cpOut.Y + hitboxSize);

                    //Select via mouse click
                    if (!IsMoving && clicked && ImGui.IsMouseHoveringRect(min, max))
                    {
                        if (!ImGui.GetIO().KeyCtrl && !keyFrame.SelectedByBox)
                        {
                            DeselectAll();
                            SelectionChanged();
                        }

                        SelectionBox.Enabled = false;

                        if (!SelectedKeys.Contains(keyFrame))
                        {
                            SelectedKeys.Add(keyFrame);
                            SelectionChanged();
                        }
                        keyFrame.IsTangentOutSelected = true;
                        IsMoving = true;
                    }

                    var color1 = keyFrame.IsTangentInSelected ? new Vector4(1, 1, 0, 1.0f) : new Vector4(1);

                    //Circle to represent a slope value
                    ImGui.GetWindowDrawList().AddCircleFilled(
                        new Vector2(cpIn.X, cpIn.Y), 5,
                        ImGui.ColorConvertFloat4ToU32(color1));

                    var color2 = keyFrame.IsTangentOutSelected ? new Vector4(1, 1, 0, 1.0f) : new Vector4(1);

                    //Circle to represent a slope value
                    ImGui.GetWindowDrawList().AddCircleFilled(
                        new Vector2(cpOut.X, cpOut.Y), 5,
                        ImGui.ColorConvertFloat4ToU32(color2));

                    //Draw lines connecting control points from the keyed position
                    ImGui.GetWindowDrawList().AddLine(pos, cpIn, lineColor);
                    ImGui.GetWindowDrawList().AddLine(pos, cpOut, lineColor);
                }
            }

            ImGui.SetCursorPos(curPos);

            var p = ImGui.GetCursorPos();
            ImGuiHelper.IncrementCursorPosX(30);
            ImGuiHelper.IncrementCursorPosY(30);
            var lblP = ImGui.GetCursorScreenPos();
            ImGui.GetWindowDrawList().AddRectFilled(lblP, lblP + ImGui.CalcTextSize(TrackName) + new Vector2(7, 1),
               ImGui.ColorConvertFloat4ToU32(new Vector4(0.0f, 0.0f, 0.0f, 1f)));

            ImGuiHelper.BoldText(TrackName);

            ImGui.SetCursorPos(p);
        }

        public void Undo()
        {
            UndoStack.Undo();
        }

        public void Redo()
        {
            UndoStack.Redo();
        }

        public void OnKeyDown(KeyEventInfo keyDown)
        {
            if (keyDown.IsKeyDown(InputSettings.INPUT.Scene.Delete)) 
                DeleteSelectedKeys();
            if (keyDown.IsKeyDown(InputSettings.INPUT.Scene.Undo)) 
                Undo();
            if (keyDown.IsKeyDown(InputSettings.INPUT.Scene.Redo))
                Redo();
            if (keyDown.IsKeyDown(InputSettings.INPUT.Scene.Copy))
                CopySelected();
            if (keyDown.IsKeyDown(InputSettings.INPUT.Scene.Paste))
                PasteSelected();
        }

        private void CopySelected()
        {
            CopiedKeys.Clear();
            foreach (var keyNode in this.SelectedKeys)
                CopiedKeys.Add(keyNode.Copy());
        }

        private void PasteSelected()
        {
            DeselectAll();
            foreach (var copy in CopiedKeys)
            {
                //Add to the parent track
                copy.TryAddFromTrack();
                //select new copy
                copy.IsSelected = true;
                SelectedKeys.Add(copy);
            }
        }

        public void OnMouseMove(MouseEventInfo mouseInfo)
        {
            const float edgeSelectionSize = 10;

            MouseEventInfo.MouseCursor = MouseEventInfo.Cursor.Arrow;
            if (_ResizeMode != ResizeMode.None)
                MouseEventInfo.MouseCursor = MouseEventInfo.Cursor.ResizeEW;

            if (SelectionBounding.Max.X - SelectionBounding.Min.X > 20)
            {
                //Selection box resizing (right side)
                if (ImGui.IsMouseHoveringRect(new Vector2(
                    SelectionBounding.Max.X - edgeSelectionSize, SelectionBounding.Min.Y),
                    new Vector2(SelectionBounding.Max.X, SelectionBounding.Max.Y)))
                {
                    MouseEventInfo.MouseCursor = MouseEventInfo.Cursor.ResizeEW;
                }
                //Selection box resizing (left side)
                if (ImGui.IsMouseHoveringRect(new Vector2(
                    SelectionBounding.Min.X, SelectionBounding.Min.Y),
                    new Vector2(SelectionBounding.Min.X + edgeSelectionSize, SelectionBounding.Max.Y)))
                {
                 //   MouseEventInfo.MouseCursor = MouseEventInfo.Cursor.ResizeEW;
                }
            }

            //Move selected
            if (IsMoving && mouseInfo.LeftButton == OpenTK.Input.ButtonState.Pressed)
            {
                float mouseFrame = Timeline.XToFrame(mouseInfo.X);
                float mouseValue = Timeline.YToValue(mouseInfo.Y);

                float currentFrame = Timeline.XToFrame(mouseInfo.X);
                float currentValue = Timeline.YToValue(mouseInfo.Y);
                float previousFrame = Timeline.XToFrame(mouseDownPos.X);
                float previousValue = Timeline.YToValue(mouseDownPos.Y);

                float diffX = (int)Math.Round(currentFrame - previousFrame);
                float diffY = currentValue - previousValue;

                if (movementChanged && (diffX != 0 || diffY != 0)) {
                    movementChanged = false;
                    UndoStack.AddToUndo(new KeyMoveOperation(SelectedKeys.ToList()));
                }

                //Check if the edge is clicked on or not
                if(_ResizeMode != ResizeMode.None && diffX != 0)
                {
                    if (ResizeOp == null)
                        ResizeOp = new ResizeOperation(previousFrame,
                            SelectedKeys.Min(x => x.Frame), 
                            SelectedKeys.Max(x => x.Frame), SelectedKeys);

                    ResizeOp.Resize(currentFrame, SelectedKeys, _ResizeMode);
                }
                else if (diffX != 0 || diffY != 0)
                {
                    //Move selected
                    for (int i = 0; i < SelectedKeys.Count; i++)
                    {
                        if (SelectedKeys[i].IsSelected)
                        {
                            SelectedKeys[i].Frame = PreDragFrames[i] + diffX;
                            SelectedKeys[i].KeyFrame.Value = PreDragValues[i] + diffY;
                        }
                        else if (SelectedKeys[i].IsTangentInSelected)
                        {
                            SelectedKeys[i].SlopeIn = PreDragInSlopes[i] - ((currentValue - previousValue) / (currentFrame - previousFrame));
                        }
                        else if (SelectedKeys[i].IsTangentOutSelected)
                        {
                            SelectedKeys[i].SlopeOut = PreDragOutSlopes[i] - ((currentValue - previousValue) / (currentFrame - previousFrame));
                        }
                    }
                }
            }
            lastMousePos = new Vector2(mouseInfo.X, mouseInfo.Y);
        }

        class ResizeOperation
        {
            private float PreviousFrame;
            private float LeftFrame;
            private float RightFrame;
            private List<float> previousFrames = new List<float>();

            public ResizeOperation(float previousFrame, float leftFrame, float rightFrame, List<AnimationTree.KeyNode> keys)
            {
                PreviousFrame = previousFrame;
                LeftFrame = leftFrame;
                RightFrame = rightFrame;
                previousFrames = keys.Select(x => x.Frame).ToList();
            }

            public void Resize(float currentFrame, List<AnimationTree.KeyNode> keys, ResizeMode mode)
            {
                float diff = (int)Math.Round(currentFrame - PreviousFrame);
                //Resize selected
                float newFrameCount = PreviousFrame + diff;

                if (mode == ResizeMode.Right)
                {
                    float ratio = (float)newFrameCount / (float)RightFrame;

                    for (int i = 0; i < keys.Count; i++)
                    {
                        if (keys[i].Frame != LeftFrame)
                        {
                            int newFrame = (int)MathF.Max(((float)previousFrames[i] * ratio + 0.5f), 0);
                            if (newFrame > LeftFrame)
                                keys[i].Frame = newFrame;
                        }
                    }
                }
                if (mode == ResizeMode.Left)
                {
                    float ratio = LeftFrame == 0 ? newFrameCount / (float)1.0f : (float)newFrameCount / (float)LeftFrame;

                    for (int i = 0; i < keys.Count; i++)
                    {
                        if (keys[i].Frame != RightFrame)
                        {
                            float value = previousFrames[i] == 0 ? newFrameCount : previousFrames[i] * ratio + 0.5f;
                            int newFrame = (int)MathF.Max((value), 0);
                            if (newFrame < RightFrame)
                                keys[i].Frame = newFrame;
                        }
                    }
                }
            }
        }

        public void OnMouseDown(MouseEventInfo mouseInfo)
        {
            mouseDownPos = new Vector2(mouseInfo.X, mouseInfo.Y);

            _mouseDown = true;
            //Selection box movement
            if(SelectedKeys.Count > 0)
            {
                if (ImGui.IsMouseHoveringRect(SelectionBounding.Min, SelectionBounding.Max))
                    IsMoving = true;

                const float edgeSelectionSize = 10;

                if (SelectionBounding.Max.X - SelectionBounding.Min.X > 20)
                {
                    //Selection box resizing (right side)
                    if (ImGui.IsMouseHoveringRect(new Vector2(
                    SelectionBounding.Max.X - edgeSelectionSize, SelectionBounding.Min.Y),
                    new Vector2(SelectionBounding.Max.X, SelectionBounding.Max.Y)))
                    {
                        _ResizeMode = ResizeMode.Right;
                    }
                    //Selection box resizing (left side)
                    if (ImGui.IsMouseHoveringRect(new Vector2(
                        SelectionBounding.Min.X, SelectionBounding.Min.Y),
                        new Vector2(SelectionBounding.Min.X + edgeSelectionSize, SelectionBounding.Max.Y)))
                    {
                     //   _ResizeMode = ResizeMode.Left;
                    }
                }
            }
        }

        private void SetupMoveOperation()
        {
            PreDragFrames.Clear();
            PreDragValues.Clear();
            PreDragInSlopes.Clear();
            PreDragOutSlopes.Clear();

            for (int i = 0; i < SelectedKeys.Count; i++)
            {
                PreDragFrames.Add(SelectedKeys[i].Frame);
                PreDragValues.Add(SelectedKeys[i].Value);
                PreDragInSlopes.Add(SelectedKeys[i].SlopeIn);
                PreDragOutSlopes.Add(SelectedKeys[i].SlopeOut);
            }
        }

        public void OnMouseUp(MouseEventInfo mouseInfo)
        {
            _ResizeMode = ResizeMode.None;
            IsMoving = false;
            _mouseDown = false;
            movementChanged = false;
            ResizeOp = null;
        }

        private void DeleteSelectedKeys()
        {
            UndoStack.AddToUndo(new KeyDeleteOperation(SelectedKeys.ToList()));

            foreach (var key in SelectedKeys)
                key.TryRemoveFromTrack();

            SelectedKeys.Clear();
        }

        public void SelectionChanged() 
        {
        }

        public void CalculateSelection()
        {
            if (SelectedKeys.Count == 0)
                return;

            SelectionBounding.Max = new Vector2(float.MinValue);
            SelectionBounding.Min = new Vector2(float.MaxValue);
            SelectedFrameMin = float.MaxValue;
            SelectedFrameMax = float.MinValue;

            //Calculate min/max
            foreach (var item in SelectedKeys)
            {
                SelectedFrameMin = MathF.Min(SelectedFrameMin, item.Frame);
                SelectedFrameMax = MathF.Max(SelectedFrameMax, item.Frame);

                SelectionBounding.Max = new Vector2(
                    MathF.Max(SelectionBounding.Max.X, item.Max.X),
                    MathF.Max(SelectionBounding.Max.Y, item.Max.Y));
                SelectionBounding.Min = new Vector2(
                    MathF.Min(SelectionBounding.Min.X, item.Min.X),
                    MathF.Min(SelectionBounding.Min.Y, item.Min.Y));
            }
        }

        class KeyMoveOperation : IRevertable
        {
            List<KeyEntry> Keys = new List<KeyEntry>();

            public KeyMoveOperation(List<KeyEntry> keys) {
                Keys = keys;
            }

            public KeyMoveOperation(List<AnimationTree.KeyNode> keyFrames)
            {
                foreach (var key in keyFrames)
                    Keys.Add(new KeyEntry()
                    {
                        KeyData = key,
                        Frame = key.Frame,
                        Value = key.Value,
                        SlopeIn = key.SlopeIn,
                        SlopeOut = key.SlopeOut,
                    });
            }

            public IRevertable Revert()
            {
                var prev = new KeyMoveOperation(Keys.ToList());

                //Add back to the tracks
                for (int i = 0; i < Keys.Count; i++)
                {
                    Keys[i].KeyData.Frame = Keys[i].Frame;
                    Keys[i].KeyData.Value = Keys[i].Value;
                    Keys[i].KeyData.SlopeIn = Keys[i].SlopeIn;
                    Keys[i].KeyData.SlopeOut = Keys[i].SlopeOut;
                }
                return prev;
            }

            public class KeyEntry
            {
                public AnimationTree.KeyNode KeyData;
                public float Frame;
                public float Value;
                public float SlopeIn;
                public float SlopeOut;
            }
        }

        enum ResizeMode
        {
            None,
            Left,
            Right,
        }

        class KeyDeleteOperation : IRevertable
        {
            List<AnimationTree.KeyNode> RemovedKeys;

            public KeyDeleteOperation(List<AnimationTree.KeyNode> keyFrames) {
                RemovedKeys = keyFrames;
            }

            public IRevertable Revert()
            {
                //Add back to the tracks
                foreach (var key in RemovedKeys)
                    key.TryAddFromTrack();

                return new KeyAddOperation(RemovedKeys);
            }
        }

        class KeyAddOperation : IRevertable
        {
            List<AnimationTree.KeyNode> RemovedKeys;

            public KeyAddOperation(List<AnimationTree.KeyNode> keyFrames) {
                RemovedKeys = keyFrames;
            }

            public IRevertable Revert()
            {
                //Remove from the tracks
                foreach (var key in RemovedKeys)
                    key.TryRemoveFromTrack();

                return new KeyAddOperation(RemovedKeys);
            }
        }
    }
}
