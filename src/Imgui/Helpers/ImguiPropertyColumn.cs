using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;
using Toolbox.Core.Animations;

namespace MapStudio.UI
{
    public class ImguiPropertyColumn
    {
        static bool DoLabel = true;

        public static void Begin(string name, int numColums = 2, bool label = true)
        {
            DoLabel = label;
            ImGui.BeginColumns(name, numColums);
        }

        public static bool RadioButton(string label, ref bool value)
        {
            Label(label);

            ImGui.PushItemWidth(ImGui.GetColumnWidth(1) - 5);
            bool edit = ImGui.RadioButton($"##{label}", value);
            if (edit)
                value = true;

            ImGui.PopItemWidth();

            ImGui.NextColumn();

            return edit;
        }

        public static bool Bool(string label, ref bool value, string tool_tip)
        {
            if (!string.IsNullOrEmpty(tool_tip))
                LabelTip(label, tool_tip);
            else
                Label(label);

            ImGui.PushItemWidth(ImGui.GetColumnWidth(1) - 5);
            bool edit = ImGui.Checkbox($"##{label}", ref value);
            ImGui.PopItemWidth();

            ImGui.NextColumn();

            return edit;
        }

        public static bool Bool(string label, ref bool value)
        {
            Label(label);

            ImGui.PushItemWidth(ImGui.GetColumnWidth(1) - 5);
            bool edit = ImGui.Checkbox($"##{label}", ref value);
            ImGui.PopItemWidth();

            ImGui.NextColumn();

            return edit;
        }

        public static bool Text(string label, ref string value)
        {
            Label(label);

            ImGui.PushItemWidth(ImGui.GetColumnWidth(1) - 5);
            bool edit = ImGui.InputText($"##{label}", ref value, 0x100);
            ImGui.PopItemWidth();

            ImGui.NextColumn();

            return edit;
        }


        public static bool SliderInt(string label, ref int value, int min, int max)
        {
            Label(label);

            ImGui.PushItemWidth(ImGui.GetColumnWidth(1) - 5);
            bool edit = ImGui.SliderInt($"##{label}", ref value, min, max);
            ImGui.PopItemWidth();

            ImGui.NextColumn();

            return edit;
        }

        public static bool SliderFloat(string label, ref float value, float min, float max)
        {
            Label(label);

            ImGui.PushItemWidth(ImGui.GetColumnWidth(1) - 5);
            bool edit = ImGui.SliderFloat($"##{label}", ref value, min, max);
            ImGui.PopItemWidth();

            ImGui.NextColumn();

            return edit;
        }

        public static bool SliderBrightness(string label, ref float value, float min, float max)
        {
            Label(label);

            ImGui.PushItemWidth(ImGui.GetColumnWidth(1) - 5);
            bool edit = ImguiCustomSlider.Brightness($"##{label}", ref value, min, max);
            ImGui.PopItemWidth();

            ImGui.NextColumn();

            return edit;
        }

        public static bool DragDegreesFloat(string label, ref float value, float speed = 0.01f)
        {
            Label(label);

            float deg = value * STMath.Rad2Deg;

            ImGui.PushItemWidth(ImGui.GetColumnWidth(1) - 5);
            bool edit = ImGui.DragFloat($"##{label}", ref deg, speed);
            ImGui.PopItemWidth();

            if (edit)
                value = deg * STMath.Deg2Rad;

            ImGui.NextColumn();

            return edit;
        }

        public static bool DragDegreesFloat3(string label, ref Vector3 value, float speed = 0.01f)
        {
            Label(label);

            Vector3 deg = value * STMath.Rad2Deg;

            ImGui.PushItemWidth(ImGui.GetColumnWidth(1) - 5);
            bool edit = ImGui.DragFloat3($"##{label}", ref deg, speed);
            ImGui.PopItemWidth();

            if (edit)
                value = deg * STMath.Deg2Rad;

            ImGui.NextColumn();

            return edit;
        }

        public static bool InputInt(string label, ref int value, int speed = 1)
        {
            Label(label);

            ImGui.PushItemWidth(ImGui.GetColumnWidth(1) - 5);
            bool edit = ImGui.InputInt($"##{label}", ref value, speed);
            ImGui.PopItemWidth();

            ImGui.NextColumn();

            return edit;
        }

        public static bool InputHexByte(string label, ref byte value, int speed = 1)
        {
            Label(label);

            ImGui.PushItemWidth(ImGui.GetColumnWidth(1) - 5);
            string hexstring = value.ToString("X2");
            bool edit = ImGui.InputText($"##{label}", ref hexstring, 2, ImGuiInputTextFlags.CharsHexadecimal | ImGuiInputTextFlags.CharsUppercase);
            if (edit)
            {
                value = byte.Parse(hexstring, System.Globalization.NumberStyles.HexNumber);
            }
            ImGui.PopItemWidth();

            ImGui.NextColumn();

            return edit;
        }
        public static bool InputHexShort(string label, ref ushort value, int speed = 1)
        {
            Label(label);

            ImGui.PushItemWidth(ImGui.GetColumnWidth(1) - 5);
            string hexstring = value.ToString("X4");
            bool edit = ImGui.InputText($"##{label}", ref hexstring, 4, ImGuiInputTextFlags.CharsHexadecimal | ImGuiInputTextFlags.CharsUppercase);
            if (edit)
            {
                value = ushort.Parse(hexstring, System.Globalization.NumberStyles.HexNumber);
            }
            ImGui.PopItemWidth();

            ImGui.NextColumn();

            return edit;
        }

        public static bool InputHexInt(string label, ref uint value, int speed = 1)
        {
            Label(label);

            ImGui.PushItemWidth(ImGui.GetColumnWidth(1) - 5);
            string hexstring = value.ToString("X8");
            bool edit = ImGui.InputText($"##{label}", ref hexstring, 8, ImGuiInputTextFlags.CharsHexadecimal | ImGuiInputTextFlags.CharsUppercase);
            if (edit)
            {
                value = uint.Parse(hexstring, System.Globalization.NumberStyles.HexNumber);
            }            
            ImGui.PopItemWidth();

            ImGui.NextColumn();

            return edit;
        }

        public static bool DragSByte(string label, ref sbyte value, float speed = 0.01f, sbyte v_min = sbyte.MinValue, sbyte v_max = sbyte.MaxValue, string format = "%d")
        {
            int v = value;
            bool edit = DragInt(label, ref v, speed, v_min, v_max, format);
            if (edit)
                value = (sbyte)v;

            return edit;
        }

        public static bool DragByte(string label, ref byte value, float speed = 0.01f, byte v_min = byte.MinValue, byte v_max = byte.MaxValue, string format = "%d")
        {
            int v = value;
            bool edit = DragInt(label, ref v, speed, v_min, v_max, format);
            if (edit)
                value = (byte)v;

            return edit;
        }

        public static bool SliderByte(string label, ref byte value, int min, int max)
        {
            int v = value;
            bool edit = SliderInt(label, ref v, min, max);
            if (edit)
                value = (byte)v;

            return edit;
        }

        public static bool DragShort(string label, ref short value, float speed = 0.01f, short v_min = short.MinValue, short v_max = short.MaxValue, string format = "%d")
        {
            int v = value;
            bool edit = DragInt(label, ref v, speed, v_min, v_max, format);
            if (edit)
                value = (short)v;

            return edit;
        }

        public static bool DragUShort(string label, ref ushort value, float speed = 0.01f, ushort v_min = ushort.MinValue, ushort v_max = ushort.MaxValue, string format = "%d")
        {
            int v = value;
            bool edit = DragInt(label, ref v, speed, v_min, v_max, format);
            if (edit)
                value = (ushort)v;

            return edit;
        }

        public static bool DragInt(string label, ref int value, float speed = 0.01f, int v_min = int.MinValue, int v_max = int.MaxValue, string format = "%d")
        {
            Label(label);

            ImGui.PushItemWidth(ImGui.GetColumnWidth(1) - 5);
            bool edit = ImGui.DragInt($"##{label}", ref value, speed, v_min, v_max, format);
            ImGui.PopItemWidth();

            ImGui.NextColumn();

            return edit;
        }

        public static bool DragFloat(string label, ref float value, float speed = 0.01f)
        {
            Label(label);

            ImGui.PushItemWidth(ImGui.GetColumnWidth(1) - 5);
            bool edit = ImGui.DragFloat($"##{label}", ref value, speed);
            ImGui.PopItemWidth();

            ImGui.NextColumn();

            return edit;
        }

        public static bool DragFloat2(string label, ref Vector2 value, float speed = 0.01f)
        {
            Label(label);

            ImGui.PushItemWidth(ImGui.GetColumnWidth(1) - 5);
            bool edit = ImGui.DragFloat2($"##{label}", ref value, speed);
            ImGui.PopItemWidth();

            ImGui.NextColumn();

            return edit;
        }

        public static bool DragFloat3(string label, ref Vector3 value, float speed = 0.01f)
        {
            Label(label);

            ImGui.PushItemWidth(ImGui.GetColumnWidth(1) - 5);
            bool edit = ImGui.DragFloat3($"##{label}", ref value, speed);
            ImGui.PopItemWidth();

            ImGui.NextColumn();

            return edit;
        }

        public static bool DragFloat4(string label, ref Vector4 value, float speed = 0.01f)
        {
            Label(label);

            ImGui.PushItemWidth(ImGui.GetColumnWidth(1) - 5);
            bool edit = ImGui.DragFloat4($"##{label}", ref value, speed);
            ImGui.PopItemWidth();

            ImGui.NextColumn();

            return edit;
        }

        public static bool ColorEdit4(string label, ref OpenTK.Vector4 value, ImGuiColorEditFlags flags)
        {
            Label(label);

            var vec4 = new Vector4(value.X, value.Y, value.Z, value.W);

            ImGui.PushItemWidth(ImGui.GetColumnWidth(1) - 5);
            bool edit = ImGui.ColorEdit4($"##{label}", ref vec4, flags);
            ImGui.PopItemWidth();

            if (edit)
                value = new OpenTK.Vector4(vec4.X, vec4.Y, vec4.Z, vec4.W);

            ImGui.NextColumn();

            return edit;
        }

        public static bool ColorEdit4(string label, ref Vector4 value, ImGuiColorEditFlags flags)
        {
            Label(label);

            ImGui.PushItemWidth(ImGui.GetColumnWidth(1) - 5);
            bool edit = ImGui.ColorEdit4($"##{label}", ref value, flags);
            ImGui.PopItemWidth();

            ImGui.NextColumn();

            return edit;
        }

        public static bool Combo<T>(string label, ref T value, ImGuiComboFlags flags = ImGuiComboFlags.None, string tool_tip = null)
        {
            if (!string.IsNullOrEmpty(tool_tip))
                LabelTip(label, tool_tip);
            else
                Label(label);

            ImGui.PushItemWidth(ImGui.GetColumnWidth() - 15);
            bool edit = ImguiCustomWidgets.ComboScrollable<T>($"##{label}", value.ToString(), ref value);
            ImGui.PopItemWidth();

            ImGui.NextColumn();

            return edit;
        }

        public static bool Combo<T>(string label, object obj, string properyName, ImGuiComboFlags flags = ImGuiComboFlags.None)
        {
            Label(label);

            ImGui.PushItemWidth(ImGui.GetColumnWidth() - 15);
            bool edit = ImGuiHelper.ComboFromEnum<T>($"##{label}", obj, properyName, flags);
            ImGui.PopItemWidth();

            ImGui.NextColumn();

            return edit;
        }

        public static void End()
        {
            ImGui.EndColumns();
        }

        static void Label(string label)
        {
            if (!DoLabel)
                return;

            ImGui.Text(label);
            ImGui.NextColumn();
        }

        static void LabelTip(string label, string tool_tip)
        {
            if (!DoLabel)
                return;

            ImGui.Text(label);
            ImGui.SameLine();
            ImGui.Text("?");
            ImGuiHelper.Tooltip(tool_tip);

            ImGui.NextColumn();
        }
    }
}
