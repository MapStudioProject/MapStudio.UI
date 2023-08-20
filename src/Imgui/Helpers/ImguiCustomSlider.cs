using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MapStudio.UI
{
    public class ImguiCustomSlider
    {
        public static bool Brightness(string label, ref float brightness, float min = 0, float max = 2, SliderFlags flags = SliderFlags.InputLeftSide)
        {
            ImGui.Text(label);
            ImGui.NextColumn();

            bool update = false;

            if (flags.HasFlag(SliderFlags.InputLeftSide))
            {
                ImGui.PushItemWidth(80);
                update |= ImGui.DragFloat($"##{label}Input", ref brightness, 0.01f);
                ImGui.PopItemWidth();

                ImGui.SameLine();
            }

            var draw_list = ImGui.GetWindowDrawList();
            var pos = ImGui.GetCursorScreenPos();
            float w = ImGui.GetColumnWidth() * 0.6f;
            float h = 23;

            float bar_height = h / 4;
            var sliderSize = new Vector2(w, h);

            float middle = max / 2f;

            var colors = new uint[3]
            {
                ImGui.ColorConvertFloat4ToU32(new Vector4(min, min, min, 1)),
                ImGui.ColorConvertFloat4ToU32(new Vector4(middle, middle, middle, 1)),
                ImGui.ColorConvertFloat4ToU32(new Vector4(max, max, max, 1)),
            };
            var count = colors.Length - 1;

            draw_list.AddRectFilledMultiColor(
                new Vector2(pos.X, pos.Y + (sliderSize.Y / count) + bar_height),
                new Vector2(pos.X + sliderSize.X / count, pos.Y + bar_height),
                colors[0],
                colors[1],
                colors[1],
                colors[0]);

            draw_list.AddRectFilledMultiColor(
                new Vector2(pos.X + sliderSize.X / count, pos.Y + (sliderSize.Y / count) + bar_height),
                new Vector2(pos.X + sliderSize.X, pos.Y + bar_height),
                colors[1],
                colors[2],
                colors[2],
                colors[1]);

            ImGui.PushStyleColor(ImGuiCol.FrameBg, 0);
            ImGui.PushStyleColor(ImGuiCol.FrameBgActive, 0);
            ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, 0);

            ImGui.PushItemWidth(w);
            update |= ImGui.SliderFloat($"##{label}", ref brightness, min, max, "");
            ImGui.PopItemWidth();

            ImGui.PopStyleColor(3);

            if (flags.HasFlag(SliderFlags.InputRightSide))
            {
                ImGui.SameLine();

                ImGui.PushItemWidth(80);
                update |= ImGui.DragFloat($"##{label}Input", ref brightness, 0.01f);
                ImGui.PopItemWidth();
            }

            ImGui.NextColumn();

            return update;
        }

        public enum SliderFlags
        {
            NoInput,
            InputRightSide,
            InputLeftSide,
        }
    }
}
