using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MapStudio.UI
{
    public static class Clipboard
    {
        public static void SetText(string text)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Shell.Run("powershell", $"-command \"Set-Clipboard -Value \\\"{text}\\\"\"");
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                if ("echo $XDG_SESSION_TYPE".Bash() == "wayland")
                    $"echo {text} | wl-copy".Bash();
                else
                    $"echo {text} | xclip -selection clipboard".Bash();
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                $"echo \"{text}\" | pbcopy".Bash();
            }
        }
        
        public static string GetText()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var text = Shell.Run("powershell", "-command \"Get-Clipboard\"");
                return text.TrimEnd();
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                if ("echo $XDG_SESSION_TYPE".Bash() == "wayland")
                    return "wl-paste".Bash();
                else
                    return "xclip -o -selection clipboard".Bash();
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return "pbpaste".Bash();
            }

            return null;
        }
    }
}
