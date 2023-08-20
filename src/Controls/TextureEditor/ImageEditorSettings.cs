using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MapStudio.UI
{
    public class ImageEditorSettings
    {
        public bool UseChannelComponents = true;

        public bool DisplayProperties = true;
        public bool DisplayVertical = true;

        [JsonIgnore]
        public bool DisplayAlpha = true;

        public bool Zoom = false;

        public Vector4 BackgroundColor = Vector4.One;

        public BackgroundType SelectedBackground = BackgroundType.Checkerboard;

        public void Save()
        {
            GlobalSettings.Current.Save();
        }

        public enum BackgroundType
        {
            Checkerboard,
            Black,
            White,
            Custom,
        }
    }
}
