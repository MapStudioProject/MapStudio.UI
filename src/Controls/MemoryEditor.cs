﻿using System;
using System.Globalization;
using ImGuiNET;
using System.Numerics;

namespace MapStudio.UI
{
    // C# port of ocornut's imgui_memory_editor.h - https://gist.github.com/ocornut/0673e37e54aff644298b

    // Mini memory editor for ImGui (to embed in your game/tools)
    // v0.10
    // Animated gif: https://cloud.githubusercontent.com/assets/8225057/9028162/3047ef88-392c-11e5-8270-a54f8354b208.gif
    //
    // You can adjust the keyboard repeat delay/rate in ImGuiIO.
    // The code assume a mono-space font for simplicity! If you don't use the default font, use ImGui::PushFont()/PopFont() to switch to a mono-space font before caling this.
    //
    // Usage:
    //   static MemoryEditor memory_editor;                                                     // save your state somewhere
    //   memory_editor.Draw("Memory Editor", mem_block, mem_block_size, (size_t)mem_block);     // run

    public class MemoryEditor
    {
        bool AllowEdits;
        int Rows;
        int DataEditingAddr;
        bool DataEditingTakeFocus;
        byte[] DataInput = new byte[32];
        byte[] AddrInput = new byte[32];

        public MemoryEditor()
        {
            Rows = 16;
            DataEditingAddr = -1;
            DataEditingTakeFocus = false;
            AllowEdits = false;
        }

        private static string FixedHex(int v, int count)
        {
            return v.ToString("X").PadLeft(count, '0');
        }

        private static bool TryHexParse(byte[] bytes, out int result)
        {
            string input = System.Text.Encoding.UTF8.GetString(bytes).ToString();
            return int.TryParse(input, NumberStyles.AllowHexSpecifier, CultureInfo.CurrentCulture, out result);
        }

        private static void ReplaceChars(byte[] bytes, string input)
        {
            var address = System.Text.Encoding.ASCII.GetBytes(input);
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (i < address.Length) ? address[i] : (byte)0;
            }
        }

        public unsafe void Draw(byte[] mem_data, int mem_size, int base_display_addr = 0)
        {
            //Use default font as this requires monospae fonts
            var font = ImGuiController.DefaultFont;
          //  ImGui.PushFont(font);
            Render(mem_data, mem_size);
         //   ImGui.PopFont();
        }

        private unsafe void Render(byte[] mem_data, int mem_size, int base_display_addr = 0)
        {
            float line_height = ImGuiNative.igGetTextLineHeight();
            int line_total_count = (mem_size + Rows - 1) / Rows;
            float glyph_width = ImGui.CalcTextSize("F").X;
            float cell_width = glyph_width * 3; // "FF " we include trailing space in the width to easily catch clicks everywhere
            int addr_digits_count = 0;
            for (int n = base_display_addr + mem_size - 1; n > 0; n >>= 4)
                addr_digits_count++;

            DrawHeader(base_display_addr, cell_width, addr_digits_count);

            ImGuiNative.igSetNextWindowContentSize(new Vector2(0.0f, line_total_count * line_height));
            ImGui.BeginChild("##scrolling", new Vector2(0, -ImGuiNative.igGetFrameHeightWithSpacing() - 22), false, 0);

            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(0, 0));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));


            var clipper = new ImGuiListClipper2(line_total_count, line_height);
            int visible_start_addr = clipper.DisplayStart * Rows;
            int visible_end_addr = clipper.DisplayEnd * Rows;

            bool data_next = false;

            if (!AllowEdits || DataEditingAddr >= mem_size)
                DataEditingAddr = -1;

            int data_editing_addr_backup = DataEditingAddr;

            if (DataEditingAddr != -1)
            {
                if (ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.UpArrow)) && DataEditingAddr >= Rows) { DataEditingAddr -= Rows; DataEditingTakeFocus = true; }
                else if (ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.DownArrow)) && DataEditingAddr < mem_size - Rows) { DataEditingAddr += Rows; DataEditingTakeFocus = true; }
                else if (ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.LeftArrow)) && DataEditingAddr > 0) { DataEditingAddr -= 1; DataEditingTakeFocus = true; }
                else if (ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.RightArrow)) && DataEditingAddr < mem_size - 1) { DataEditingAddr += 1; DataEditingTakeFocus = true; }
            }
            if ((DataEditingAddr / Rows) != (data_editing_addr_backup / Rows))
            {
                // Track cursor movements
                float scroll_offset = ((DataEditingAddr / Rows) - (data_editing_addr_backup / Rows)) * line_height;
                bool scroll_desired = (scroll_offset < 0.0f && DataEditingAddr < visible_start_addr + Rows * 2) || (scroll_offset > 0.0f && DataEditingAddr > visible_end_addr - Rows * 2);
                if (scroll_desired)
                    ImGuiNative.igSetScrollYFloat(ImGuiNative.igGetScrollY() + scroll_offset);
            }

            for (int line_i = clipper.DisplayStart; line_i < clipper.DisplayEnd; line_i++) // display only visible items
            {
                int addr = line_i * Rows;

                ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled]);
                ImGui.Text(FixedHex(base_display_addr + addr, Math.Max(addr_digits_count, 6)) + ": ");
                ImGui.SameLine();
                ImGuiHelper.IncrementCursorPosX(15);
                ImGui.PopStyleColor();

                // Draw Hexadecimal
                float line_start_x = ImGuiNative.igGetCursorPosX();
                for (int n = 0; n < Rows && addr < mem_size; n++, addr++)
                {
                    ImGui.SameLine(line_start_x + cell_width * n);

                    if (DataEditingAddr == addr)
                    {
                        // Display text input on current byte
                        ImGui.PushID(addr);

                        // FIXME: We should have a way to retrieve the text edit cursor position more easily in the API, this is rather tedious.
                        ImGuiInputTextCallback callback = (data) =>
                        {
                            int* p_cursor_pos = (int*)data->UserData;

                            if (ImGuiNative.ImGuiInputTextCallbackData_HasSelection(data) == 0)
                                *p_cursor_pos = data->CursorPos;
                            return 0;
                        };
                        int cursor_pos = -1;
                        bool data_write = false;
                        if (DataEditingTakeFocus)
                        {
                            ImGui.SetKeyboardFocusHere();
                            ReplaceChars(DataInput, FixedHex(mem_data[addr], 2));
                            ReplaceChars(AddrInput, FixedHex(base_display_addr + addr, addr_digits_count));
                        }
                        ImGui.PushItemWidth(ImGui.CalcTextSize("FF").X);

                        var flags = ImGuiInputTextFlags.CharsHexadecimal | ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.NoHorizontalScroll | ImGuiInputTextFlags.AlwaysInsertMode | ImGuiInputTextFlags.CallbackAlways;

                        if (ImGui.InputText("##data", DataInput, 32, flags, (IntPtr)(&cursor_pos)))
                            data_write = data_next = true;
                        else if (!DataEditingTakeFocus && !ImGui.IsItemActive())
                            DataEditingAddr = -1;

                        DataEditingTakeFocus = false;
                        ImGui.PopItemWidth();
                        if (cursor_pos >= 2)
                            data_write = data_next = true;
                        if (data_write)
                        {
                            int data;
                            if (TryHexParse(DataInput, out data))
                                mem_data[addr] = (byte)data;
                        }
                        ImGui.PopID();
                    }
                    else
                    {
                        ImGui.Text(FixedHex(mem_data[addr], 2));
                        if (AllowEdits && ImGui.IsItemHovered() && ImGui.IsMouseClicked(0))
                        {
                            DataEditingTakeFocus = true;
                            DataEditingAddr = addr;
                        }
                    }
                }

                ImGui.SameLine(line_start_x + cell_width * Rows + glyph_width * 2);
                //separator line drawing replaced by printing a pipe char

                // Draw ASCII values
                addr = line_i * Rows;
                var asciiVal = new System.Text.StringBuilder(2 + Rows);
                for (int n = 0; n < Rows && addr < mem_size; n++, addr++)
                {
                    int c = mem_data[addr];
                    asciiVal.Append((c >= 32 && c < 128) ? Convert.ToChar(c) : '.');
                }
                var font = ImGuiController.DefaultFont;
                ImGui.PushFont(font);
                ImGui.TextUnformatted(asciiVal.ToString());  //use unformatted, so string can contain the '%' character
                ImGui.PopFont();
            }
            //clipper.End();  //not implemented
            ImGui.PopStyleVar(2);

            ImGui.EndChild();

            if (data_next && DataEditingAddr < mem_size)
            {
                DataEditingAddr = DataEditingAddr + 1;
                DataEditingTakeFocus = true;
            }

            ImGui.Separator();

            ImGuiNative.igAlignTextToFramePadding();
            ImGui.PushItemWidth(50);
            ImGui.PushAllowKeyboardFocus(true);
            int rows_backup = Rows;
            if (ImGui.DragInt("##rows", ref Rows, 0.2f, 4, 32, "%.0f rows"))
            {
                if (Rows <= 0) Rows = 4;
                Vector2 new_window_size = ImGui.GetWindowSize();
                new_window_size.X += (Rows - rows_backup) * (cell_width + glyph_width);
                ImGui.SetWindowSize(new_window_size);
            }
            ImGui.PopAllowKeyboardFocus();
            ImGui.PopItemWidth();
            ImGui.SameLine();
            ImGui.Text(string.Format(" Range {0}..{1} ", FixedHex(base_display_addr, addr_digits_count),
                FixedHex(base_display_addr + mem_size - 1, addr_digits_count)));
            ImGui.SameLine();
            ImGui.PushItemWidth(70);
            if (ImGui.InputText("##addr", AddrInput, 32, ImGuiInputTextFlags.CharsHexadecimal | ImGuiInputTextFlags.EnterReturnsTrue))
            {
                int goto_addr;
                if (TryHexParse(AddrInput, out goto_addr))
                {
                    goto_addr -= base_display_addr;
                    if (goto_addr >= 0 && goto_addr < mem_size)
                    {
                        ImGui.BeginChild("##scrolling");
                        ImGui.SetScrollFromPosY(ImGui.GetCursorStartPos().Y + (goto_addr / Rows) * ImGuiNative.igGetTextLineHeight());
                        ImGui.EndChild();
                        DataEditingAddr = goto_addr;
                        DataEditingTakeFocus = true;
                    }
                }
            }
            ImGui.PopItemWidth();
        }

        private void DrawHeader(int base_display_addr, float cell_width, int addr_digits_count)
        {
            var spacing = ImGui.CalcTextSize(FixedHex(base_display_addr + 0, Math.Max(addr_digits_count, 6)) + ": ").X;
            ImGuiHelper.IncrementCursorPosX(spacing + 15);
            float pos = ImGuiNative.igGetCursorPosX();

            ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled]);
            for (int column = 0; column < Rows; column++)
            {
                ImGui.SameLine(pos + cell_width * column);

                ImGui.PushItemWidth(ImGui.CalcTextSize("FF").X);
                ImGui.Text(FixedHex(column, 2));
                ImGui.PopItemWidth();
            }
            ImGui.PopStyleColor();
        }
    }

    //Not a proper translation, because ImGuiListClipper uses imgui's internal api.
    //Thus SetCursorPosYAndSetupDummyPrevLine isn't reimplemented, but SetCursorPosY + SetNextWindowContentSize seems to be working well instead.
    //TODO expose clipper through newer cimgui version
    public class ImGuiListClipper2
    {
        public float StartPosY;
        public float ItemsHeight;
        public int ItemsCount, StepNo, DisplayStart, DisplayEnd;

        public ImGuiListClipper2(int items_count = -1, float items_height = -1.0f)
        {
            Begin(items_count, items_height);
        }

        public unsafe void Begin(int count, float items_height = -1.0f)
        {
            StartPosY = ImGuiNative.igGetCursorPosY();
            ItemsHeight = items_height;
            ItemsCount = count;
            StepNo = 0;
            DisplayEnd = DisplayStart = -1;
            if (ItemsHeight > 0.0f)
            {
                int dispStart, dispEnd;
                ImGuiNative.igCalcListClipping(ItemsCount, ItemsHeight, &dispStart, &dispEnd);
                DisplayStart = dispStart;
                DisplayEnd = dispEnd;
                if (DisplayStart > 0)
                    //SetCursorPosYAndSetupDummyPrevLine(StartPosY + DisplayStart * ItemsHeight, ItemsHeight); // advance cursor
                    ImGuiNative.igSetCursorPosY(StartPosY + DisplayStart * ItemsHeight);
                StepNo = 2;
            }
        }
    }
}