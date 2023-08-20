using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using UIFramework;

namespace MapStudio.UI
{
    public class ImageNormalMapGenEditor : Window
    {
        private ImageEditDrawArgs Parameters;

        private ImageEditorHelper Editor;

        public ImageNormalMapGenEditor(ImageEditorHelper editorHelper, ImageEditDrawArgs args)
            : base("Normal Map Generator", new Vector2(400, 250))
        {
            Editor = editorHelper;
            Parameters = args;
        }

        public override void Render()
        {
            bool update = false;

            ImGuiHelper.BoldText("Normal Map Strength");

            update |= ImGui.SliderFloat("##Strength", ref Parameters.NormalMapStrength, 0, 5);

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

        private bool SliderBrightnessColor(ref float brightness)
        {
            var draw_list = ImGui.GetWindowDrawList();
            var pos = ImGui.GetCursorScreenPos();
            float w = ImGui.GetWindowWidth() * 0.6f;
            float h = 23;

            float bar_height = h / 4;
            var sliderSize = new Vector2(w, h);

            var colors = new uint[3]
            {
                ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 1)),
                ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1)),
                ImGui.ColorConvertFloat4ToU32(new Vector4(2, 2, 2, 2)),
            };

            draw_list.AddRectFilledMultiColor(
                new Vector2(pos.X, pos.Y + (sliderSize.Y / 2) + bar_height),
                new Vector2(pos.X + sliderSize.X / 2, pos.Y + bar_height),
                colors[0],
                colors[1],
                colors[1],
                colors[0]);

            draw_list.AddRectFilledMultiColor(
                new Vector2(pos.X + sliderSize.X / 2, pos.Y + (sliderSize.Y / 2) + bar_height),
                new Vector2(pos.X + sliderSize.X, pos.Y + bar_height),
                colors[1],
                colors[2],
                colors[2],
                colors[1]);

            bool update = false;

            ImGui.PushStyleColor(ImGuiCol.FrameBg, 0);
            ImGui.PushStyleColor(ImGuiCol.FrameBgActive, 0);
            ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, 0);

            ImGui.PushItemWidth(w);
            update |= ImGui.SliderFloat("##Brightness", ref brightness, 0, 2, "");
            ImGui.PopItemWidth();

            ImGui.PopStyleColor(3);

            ImGui.SameLine();

            ImGui.PushItemWidth(80);
            update |= ImGui.DragFloat($"##BrightnessI", ref brightness);
            ImGui.PopItemWidth();

            return update;
        }

        private bool SliderContrast(ref float contrast)
        {
            var draw_list = ImGui.GetWindowDrawList();
            var pos = ImGui.GetCursorScreenPos();
            float w = ImGui.GetWindowWidth() * 0.6f;
            float h = 23;

            float bar_height = h / 4;
            var sliderSize = new Vector2(w, h);

            var colors = new uint[3]
            {
                ImGui.ColorConvertFloat4ToU32(new Vector4(0.5f, 0.5f, 0.5f, 1)),
                ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1)),
                ImGui.ColorConvertFloat4ToU32(new Vector4(0.2F, 0.2F, 0.2F, 1)),
            };

            draw_list.AddRectFilledMultiColor(
                new Vector2(pos.X, pos.Y + (sliderSize.Y / 2) + bar_height),
                new Vector2(pos.X + sliderSize.X / 2, pos.Y + bar_height),
                colors[0],
                colors[1],
                colors[1],
                colors[0]);

            draw_list.AddRectFilledMultiColor(
                new Vector2(pos.X + sliderSize.X / 2, pos.Y + (sliderSize.Y / 2) + bar_height),
                new Vector2(pos.X + sliderSize.X, pos.Y + bar_height),
                colors[1],
                colors[2],
                colors[2],
                colors[1]);

            bool update = false;

            ImGui.PushStyleColor(ImGuiCol.FrameBg, 0);
            ImGui.PushStyleColor(ImGuiCol.FrameBgActive, 0);
            ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, 0);

            ImGui.PushItemWidth(w);
            update |= ImGui.SliderFloat("##Contrast", ref contrast, 0, 2, "");
            ImGui.PopItemWidth();

            ImGui.PopStyleColor(3);

            ImGui.SameLine();

            ImGui.PushItemWidth(80);
            update |= ImGui.DragFloat($"##ContrastI", ref contrast);
            ImGui.PopItemWidth();

            return update;
        }

        private bool SliderHueColor(ref float hueValue)
        {
            var draw_list = ImGui.GetWindowDrawList();
            var pos = ImGui.GetCursorScreenPos();
            float w = ImGui.GetWindowWidth() * 0.6f;
            float h = 23;

            float bar_height = h / 4;

            var sliderSize = new Vector2(w, h);

            var colors = new uint[7]
            {
                ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0, 0, 1)),
                ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 0, 1)),
                ImGui.ColorConvertFloat4ToU32(new Vector4(0, 1, 0, 1)),
                ImGui.ColorConvertFloat4ToU32(new Vector4(0, 1, 1, 1)),
                ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 1, 1)),
                ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0, 1, 1)),
                ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0, 0, 1)),
            };

            for (int i = 0; i < 6; ++i)
            {
                draw_list.AddRectFilledMultiColor(
                    new System.Numerics.Vector2(pos.X + i * (sliderSize.X / 6), pos.Y + (sliderSize.Y / 2) + bar_height),
                    new System.Numerics.Vector2(pos.X + (i + 1) * (sliderSize.X / 6), pos.Y + bar_height),
                    colors[i],
                    colors[i + 1],
                    colors[i + 1],
                    colors[i]);
            }

            ImGui.PushStyleColor(ImGuiCol.FrameBg, 0);
            ImGui.PushStyleColor(ImGuiCol.FrameBgActive, 0);
            ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, 0);

            bool update = false;

            float hue = 6.0f - hueValue;

            ImGui.PushItemWidth(w);
            update |= ImGui.SliderFloat("##Hue", ref hue, 0, 6, "");
            ImGui.PopItemWidth();

            if (update)
                hueValue = 6.0f - hue;

            ImGui.PopStyleColor(3);

            ImGui.SameLine();

            ImGui.PushItemWidth(80);
            update |= ImGui.DragFloat($"##HueI", ref hueValue);
            ImGui.PopItemWidth();

            return update;
        }

        private bool SliderSaturationColor(ref float saturation)
        {
            var draw_list = ImGui.GetWindowDrawList();
            var pos = ImGui.GetCursorScreenPos();
            float w = ImGui.GetWindowWidth() * 0.6f;
            float h = 23;

            float bar_height = h / 4;

            var sliderSize = new Vector2(w, h);

            Vector4 SetSaturation(float r, float g, float b, float adjustment)
            {
                Vector3 color = new Vector3(r, g, b);
                Vector3 W = new Vector3(0.2125f, 0.7154f, 0.0721f);
                Vector3 intensity = new Vector3(Vector3.Dot(color, W));
                return new Vector4(Vector3.Lerp(intensity, color, adjustment), 1);
            }

            var colors = new uint[7]
            {
                ImGui.ColorConvertFloat4ToU32(SetSaturation(1, 0, 0, 0)), //0
                ImGui.ColorConvertFloat4ToU32(SetSaturation(1, 0, 0, 0.33f)), //0.33
                ImGui.ColorConvertFloat4ToU32(SetSaturation(1, 0, 0, 0.66f)), //0.66
                ImGui.ColorConvertFloat4ToU32(SetSaturation(1, 0, 0, 1f)), //1
                ImGui.ColorConvertFloat4ToU32(SetSaturation(1, 0, 0, 1.33f)), //1.33
                ImGui.ColorConvertFloat4ToU32(SetSaturation(1, 0, 0, 1.66f)), //1.66
                ImGui.ColorConvertFloat4ToU32(SetSaturation(1, 0, 0, 2f)), //2
            };

            for (int i = 0; i < 6; ++i)
            {
                draw_list.AddRectFilledMultiColor(
                    new System.Numerics.Vector2(pos.X + i * (sliderSize.X / 6), pos.Y + (sliderSize.Y / 2) + bar_height),
                    new System.Numerics.Vector2(pos.X + (i + 1) * (sliderSize.X / 6), pos.Y + bar_height),
                    colors[i],
                    colors[i + 1],
                    colors[i + 1],
                    colors[i]);
            }

            ImGui.PushStyleColor(ImGuiCol.FrameBg, 0);
            ImGui.PushStyleColor(ImGuiCol.FrameBgActive, 0);
            ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, 0);

            bool update = false;

            ImGui.PushItemWidth(w);
            update |= ImGui.SliderFloat("##Saturation", ref saturation, 0, 2, "");
            ImGui.PopItemWidth();

            ImGui.PopStyleColor(3);

            ImGui.SameLine();

            ImGui.PushItemWidth(80);
            update |= ImGui.DragFloat($"##SaturationI", ref saturation);
            ImGui.PopItemWidth();

            return update;
        }
    }
}
