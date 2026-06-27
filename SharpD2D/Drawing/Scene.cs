using System;

namespace SharpD2D.Drawing
{
    public class Scene : IDisposable
    {
        private readonly Graphics _graphics;

        internal Scene(Graphics graphics)
        {
            _graphics = graphics ?? throw new ArgumentNullException(nameof(graphics));
            _graphics.BeginScene();
        }

        public void Dispose()
        {
            _graphics.EndScene();
        }
    }
}
