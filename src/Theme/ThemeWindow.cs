using ImGuiNET;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;
using UIFramework;

namespace MapStudio.UI
{
    public class ThemeWindow : Window
    {
        public override string Name => "Theme Editor";

        private string ThemeText = "";

        public ThemeWindow()
        {
            ThemeText = ThemeHandler.Theme.Name;
        }

        public override void Render()
        {
            var theme = ThemeHandler.Theme;

            if (ImGui.Button("Save", new Vector2(ImGui.GetWindowWidth(), 23)))
            {
                theme.Name = ThemeText;

                string path = Path.Combine(Runtime.ExecutableDir, "Lib", "Themes", $"{ThemeText}.json");
                theme.Export(path);

                //Reload
                MapStudio.UI.ThemeHandler.Load();
            }

            ImguiPropertyColumn.Begin("themeSelector", 2);
            ImguiPropertyColumn.Text("Theme", ref ThemeText);
            ImguiPropertyColumn.End();

            if (ImGui.BeginChild("themePropertiesChild"))
            {
                ImguiPropertyColumn.Begin("themeProperties", 2);

                var properties = theme.GetType().GetProperties();
                foreach (var prop in properties)
                {
                    if (prop.PropertyType == typeof(Vector4))
                    {
                        var color = (Vector4)prop.GetValue(theme);
                        if (ImguiPropertyColumn.ColorEdit4(prop.Name, ref color, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaPreviewHalf))
                        {
                            prop.SetValue(theme, color);
                            ThemeHandler.UpdateTheme(theme);
                        }
                    }
                }

                ImguiPropertyColumn.End();
            }
            ImGui.EndChild();
        }

        private void DrawThemeDropdownm()
        {
            string currentThemeName = GlobalSettings.Current.Program.Theme.ToString();
            if (ImGui.BeginCombo(TranslationSource.GetText("THEME"), TranslationSource.GetText(currentThemeName)))
            {
                foreach (var colorTheme in ThemeHandler.ThemeFilePaths)
                {
                    string themeName = Path.GetFileNameWithoutExtension(colorTheme);
                    string name = TranslationSource.GetText(themeName);
                    bool selected = themeName == currentThemeName;
                    if (ImGui.Selectable(name, selected))
                    {
                        //Set the current theme instance
                        ThemeHandler.UpdateTheme(colorTheme);

                        GlobalSettings.Current.Program.Theme = themeName;
                        GlobalSettings.Current.Save();
                    }
                    if (selected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }
        }
    }
}
