using GLFrameworkEngine;
using System;
using System.Collections.Generic;
using System.Text;

namespace MapStudio.UI
{
    public interface ISceneRender
    {
        void Render(GLContext context, Framebuffer materialPass);
        void Resize(int width, int height);
    }
}
