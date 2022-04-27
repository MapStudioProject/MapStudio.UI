using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;

namespace MapStudio.UI
{
    internal class WindowsThemeUtil
    {
        #region WINAPI
        [DllImport("dwmapi.dll", SetLastError = true)]
        private static extern bool DwmSetWindowAttribute(IntPtr handle, int param, in int value, int size);


        [DllImport("uxtheme.dll", SetLastError = true)]
        private static extern bool SetWindowTheme(IntPtr handle, string? subAppName, string? subIDList);
        #endregion

        public static void Init(IntPtr handle)
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                //Support dark mode on Windows
                //ported from https://github.com/libsdl-org/SDL/issues/4776#issuecomment-926976455

                SetWindowTheme(handle, "DarkMode_Explorer", null);
                ToggleDarkmode(handle, true);
            }
        }

        /// <summary>
        /// Enables/disables darkmode for this window (works only on Windows 10+)
        /// </summary>
        /// <param name="value"></param>
        public static void ToggleDarkmode(IntPtr handle, bool value)
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                if (!DwmSetWindowAttribute(handle, 20, value ? 1 : 0, sizeof(int)))
                    DwmSetWindowAttribute(handle, 19, value ? 1 : 0, sizeof(int));
            }
        }
    }
}
