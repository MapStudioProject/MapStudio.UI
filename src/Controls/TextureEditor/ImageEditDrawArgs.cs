using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MapStudio.UI
{
    public class ImageEditDrawArgs
    {
        //HSB Tool
        public float Brightness = 1.0f;
        public float Saturation = 1.0f;
        public float Hue = 0.0f;
        public float Contrast = 1.0f;

        public Vector4 Color = Vector4.One;

        public float NormalMapStrength = 1f;
    }
}
