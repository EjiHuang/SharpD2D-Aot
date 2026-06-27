using System;
using System.IO;
using DirectN;
using DirectN.Extensions;
using DirectN.Extensions.Com;

namespace SharpD2D.Drawing
{
    public class Image : IDisposable
    {
        public IComObject<ID2D1Bitmap> Bitmap { get; private set; }
        public float Width => Bitmap?.Object.GetSize().width ?? 0;
        public float Height => Bitmap?.Object.GetSize().height ?? 0;

        private Image() { }

        internal Image(IComObject<ID2D1RenderTarget> device, byte[] bytes)
        {
            ArgumentNullException.ThrowIfNull(device);
            ArgumentNullException.ThrowIfNull(bytes);
            var props = new D2D1_BITMAP_PROPERTIES
            {
                pixelFormat = new D2D1_PIXEL_FORMAT { format = DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM, alphaMode = D2D1_ALPHA_MODE.D2D1_ALPHA_MODE_PREMULTIPLIED }
            };
            Bitmap = device.CreateBitmap(new D2D_SIZE_U(1, 1), props);
        }

        internal Image(IComObject<ID2D1RenderTarget> device, string path)
        {
            ArgumentNullException.ThrowIfNull(device);
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            var props = new D2D1_BITMAP_PROPERTIES
            {
                pixelFormat = new D2D1_PIXEL_FORMAT { format = DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM, alphaMode = D2D1_ALPHA_MODE.D2D1_ALPHA_MODE_PREMULTIPLIED }
            };
            Bitmap = device.CreateBitmap(new D2D_SIZE_U(1, 1), props);
        }

        internal Image(IComObject<ID2D1RenderTarget> device) { }

        internal Image(IComObject<ID2D1RenderTarget> device, D2D_SIZE_U size, D2D1_PIXEL_FORMAT format)
        {
            ArgumentNullException.ThrowIfNull(device);
            var props = new D2D1_BITMAP_PROPERTIES { pixelFormat = format };
            Bitmap = device.CreateBitmap(size, props);
        }

        ~Image() => Dispose(false);

        private bool _disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                Bitmap?.Dispose();
                Bitmap = null;
            }
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
