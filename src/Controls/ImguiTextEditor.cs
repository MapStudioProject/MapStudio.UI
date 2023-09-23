using ImGuiNET;
using System;
using ImGuiColorTextEditNet;
using System.Drawing;

namespace MapStudio.UI
{
    public class ImguiTextEditor
    {
        public string Text
        {
            get { return editor.AllText; }
        }

        public float FontScale = 0.5f;

        private float PreviousScroll = 0;

        TextEditor editor;

        public ImguiTextEditor()
        {
            Clear();
        }

        private bool update_text = false;

        public void Load(string text) {
            editor.AllText = text;
        }

        public void Clear()
        {
            editor = new TextEditor
            {
                AllText = "",
                SyntaxHighlighter = new YamlHighlighter()
            };
            editor.Renderer.IsShowingWhitespace = false;

            editor.SetColor(PaletteIndex.Background, 0xff151515);

            editor.SetColor(PaletteIndex.CurrentLineFillInactive, 0x00ffffff);

            editor.SetColor(PaletteIndex.Default, 0xffD69C56);
            editor.SetColor(PaletteIndex.KnownIdentifier, 0xffffffff);
            editor.SetColor(PaletteIndex.Number, 0xff859DD6);
            editor.SetColor(PaletteIndex.Selection, 0xff784F26);

            editor.SetColor(PaletteIndex.Custom, 0xff0000ff);
            editor.SetColor(PaletteIndex.Custom + 1, 0xff00ffff);
            editor.SetColor(PaletteIndex.Custom + 2, 0xffffffff);
            editor.SetColor(PaletteIndex.Custom + 3, 0xff808080);
        }

        public void Render()
        {
            if (ImGui.BeginChild("textEditor"))
            {
                var defaultFontScale = ImGui.GetIO().FontGlobalScale;

                var font = ImGuiController.DefaultFont;

                FontScale = 1f;

                ImGui.SetWindowFontScale(FontScale);
               // ImGui.PushFont(ImGuiController.FontZoomed);
               // ImGui.PushFont(ImGuiController.FontJKZoomed);

               // ImGui.PushFont(font);

                editor.Render("EditWindow");

             //   ImGui.PopFont();
            //    ImGui.PopFont();

                if (ImGui.BeginPopupContextItem("##TextMenu", ImGuiPopupFlags.MouseButtonRight))
                {
                    if (ImGui.MenuItem("Copy"))
                    {
                        editor.Modify.Copy();
                    }
                    if (ImGui.MenuItem("Paste"))
                    {
                        editor.Modify.Paste();
                    }
                    ImGui.EndPopup();
                }

                var mouseInfo = ImGuiHelper.CreateMouseState();
                var scrollY = PreviousScroll - mouseInfo.WheelPrecise;
                PreviousScroll = mouseInfo.WheelPrecise;

                if (ImGui.GetIO().KeyCtrl && ImGui.IsItemHovered() && scrollY != 0)
                {
                    if (scrollY > 0)
                        FontScale -= 0.1F;
                    else
                        FontScale += 0.1F;
                    FontScale = Math.Clamp(FontScale, 0.1f, 8f);
                }

                ImGui.SetWindowFontScale(defaultFontScale);
            }
            ImGui.EndChild();
        }
    }
}
