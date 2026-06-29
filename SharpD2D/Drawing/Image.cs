using System;
using System.Globalization;
using System.IO;
using DirectN;
using DirectN.Extensions;
using DirectN.Extensions.Com;
using SharpD2D.Drawing.Imaging;

namespace SharpD2D.Drawing
{
    /// <summary>
    ///     Represents an Image which can be drawn using a Graphics surface.
    /// </summary>
    public class Image : IDisposable
    {
        private IComObject<ID2D1RenderTarget> _device;

        /// <summary>
        ///     The Direct2D Bitmap.
        /// </summary>
        public IComObject<ID2D1Bitmap> Bitmap { get; private set; }

        /// <summary>
        ///     Gets the width of this Image.
        /// </summary>
        public float Width => Bitmap?.Object.GetSize().width ?? 0;

        /// <summary>
        ///     Gets the height of this Image.
        /// </summary>
        public float Height => Bitmap?.Object.GetSize().height ?? 0;

        /// <summary>
        ///     Initializes a new Image without a bitmap.
        /// </summary>
        /// <param name="device">The Direct2D device.</param>
        public Image(IComObject<ID2D1RenderTarget> device)
        {
            _device = device;
        }

        /// <summary>
        ///     Initializes a new Image for the given device by using a byte[].
        /// </summary>
        /// <param name="device">The Direct2D device.</param>
        /// <param name="bytes">A byte[] containing image data.</param>
        public Image(IComObject<ID2D1RenderTarget> device, byte[] bytes)
        {
            Bitmap = LoadBitmapFromMemory(device, bytes);
        }

        /// <summary>
        ///     Initializes a new Image for the given device by using a file on disk.
        /// </summary>
        /// <param name="device">The Direct2D device.</param>
        /// <param name="path">The path to an image file on disk.</param>
        public Image(IComObject<ID2D1RenderTarget> device, string path)
        {
            Bitmap = LoadBitmapFromFile(device, path);
        }

        /// <summary>
        ///     Initializes a new Image for the given Graphics device by using a byte[].
        /// </summary>
        /// <param name="device">The Graphics device.</param>
        /// <param name="bytes">A byte[] containing image data.</param>
        public Image(Graphics device, byte[] bytes) : this(device.GetRenderTarget(), bytes)
        {
        }

        /// <summary>
        ///     Initializes a new Image for the given Graphics device by using a file on disk.
        /// </summary>
        /// <param name="device">The Graphics device.</param>
        /// <param name="path">The path to an image file on disk.</param>
        public Image(Graphics device, string path) : this(device.GetRenderTarget(), path)
        {
        }

        /// <summary>
        ///     Initializes an empty placeholder bitmap with the specified size and format.
        /// </summary>
        /// <param name="device">The Direct2D device.</param>
        /// <param name="size">The pixel size of the bitmap.</param>
        /// <param name="format">The pixel format of the bitmap.</param>
        public Image(IComObject<ID2D1RenderTarget> device, D2D_SIZE_U size, D2D1_PIXEL_FORMAT format)
        {
            ArgumentNullException.ThrowIfNull(device);
            var props = new D2D1_BITMAP_PROPERTIES { pixelFormat = format };
            Bitmap = device.CreateBitmap(size, props);
            _device = device;
        }

        /// <summary>
        ///     Allows an object to try to free resources and perform other cleanup operations before it is reclaimed by garbage
        ///     collection.
        /// </summary>
        ~Image()
        {
            Dispose(false);
        }

        /// <summary>
        ///     Returns a value indicating whether this instance and a specified <see cref="object" /> represent the same
        ///     type and value.
        /// </summary>
        /// <param name="obj">The object to compare with this instance.</param>
        /// <returns>
        ///     <see langword="true" /> if <paramref name="obj" /> is an Image and equal to this instance; otherwise,
        ///     <see langword="false" />.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj is Image image)
                return ReferenceEquals(Bitmap?.Object, image.Bitmap?.Object);
            return false;
        }

        /// <summary>
        ///     Returns a value indicating whether two specified instances of Image represent the same value.
        /// </summary>
        /// <param name="value">An object to compare to this instance.</param>
        /// <returns>
        ///     <see langword="true" /> if <paramref name="value" /> is equal to this instance; otherwise,
        ///     <see langword="false" />.
        /// </returns>
        public bool Equals(Image value)
        {
            return value is not null
                   && ReferenceEquals(Bitmap?.Object, value.Bitmap?.Object);
        }

        /// <summary>
        ///     Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return OverrideHelper.HashCodes(
                (Bitmap?.Object?.GetHashCode() ?? 0));
        }

        /// <summary>
        ///     Converts this Image instance to a human-readable string.
        /// </summary>
        /// <returns>A string representation of this Image.</returns>
        public override string ToString()
        {
            return OverrideHelper.ToString(
                "Image", "Bitmap",
                "Width", Width.ToString(CultureInfo.InvariantCulture),
                "Height", Height.ToString(CultureInfo.InvariantCulture),
                "PixelFormat", Bitmap?.Object.GetPixelFormat().format.ToString());
        }

        /// <summary>
        ///     Updates the underlying bitmap by copying data from the specified memory region.
        /// </summary>
        /// <param name="width">The width of the bitmap in pixels.</param>
        /// <param name="height">The height of the bitmap in pixels.</param>
        /// <param name="pitch">The size of bytes each scan line, aka stride in GDI+.</param>
        /// <param name="scan0">Pointer to the start of the first pixel in the bitmap data.</param>
        /// <param name="format">Format of the bitmap data.</param>
        /// <param name="destination">The destination to copy the data to, only effective if the size and format are the same.</param>
        /// <remarks>If the size or format doesn't match the existing one, a new one is created and the old one will be disposed.</remarks>
        public void Update(int width, int height, int pitch, IntPtr scan0, D2D1_PIXEL_FORMAT format, D2D_RECT_U destination)
        {
            var pixelSize = Bitmap?.Object.GetPixelSize();
            if (Bitmap is null || pixelSize?.width != width || pixelSize?.height != height || format.format != Bitmap.Object.GetPixelFormat().format)
            {
                Bitmap?.Dispose();
                var props = new D2D1_BITMAP_PROPERTIES { pixelFormat = format };
                Bitmap = _device.CreateBitmap(new D2D_SIZE_U((uint)width, (uint)height), scan0, (uint)pitch, props);
            }
            else
            {
                Bitmap.Object.CopyFromMemory(scan0, (uint)pitch, destination);
            }
        }

        /// <summary>
        ///     Updates the underlying bitmap by copying data from the specified memory region.
        /// </summary>
        /// <param name="width">The width of the bitmap in pixels.</param>
        /// <param name="height">The height of the bitmap in pixels.</param>
        /// <param name="pitch">The size of bytes each scan line, aka stride in GDI+.</param>
        /// <param name="scan0">Pointer to the start of the first pixel in the bitmap data.</param>
        /// <param name="format">Format of the bitmap data.</param>
        /// <remarks>If the size or format doesn't match the existing one, a new one is created and the old one will be disposed.</remarks>
        public void Update(int width, int height, int pitch, IntPtr scan0, D2D1_PIXEL_FORMAT format)
        {
            var pixelSize = Bitmap?.Object.GetPixelSize();
            Update(width, height, pitch, scan0, format,
                new D2D_RECT_U(0, 0, pixelSize?.width ?? 0, pixelSize?.height ?? 0));
        }

        /// <summary>
        ///     Returns a value indicating whether two specified instances of Image represent the same value.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns>
        ///     <see langword="true" /> if <paramref name="left" /> and <paramref name="right" /> are equal; otherwise,
        ///     <see langword="false" />.
        /// </returns>
        public static bool Equals(Image left, Image right)
        {
            return left?.Equals(right) == true;
        }

        private IComObject<ID2D1Bitmap> LoadBitmapFromMemory(IComObject<ID2D1RenderTarget> device, byte[] bytes)
        {
            ArgumentNullException.ThrowIfNull(device);
            ArgumentNullException.ThrowIfNull(bytes);
            if (bytes.Length == 0) throw new ArgumentOutOfRangeException(nameof(bytes));

            _device = device;
            IComObject<ID2D1Bitmap> bmp = null;
            MemoryStream stream = null;
            IComObject<IWICBitmapDecoder> decoder = null;

            try
            {
                stream = new MemoryStream(bytes);
                decoder = WicImagingFactory.CreateDecoderFromStream(stream);
                bmp = ImageDecoder.Decode(device, decoder);

                decoder.Dispose();
                stream.Dispose();

                return bmp;
            }
            catch
            {
                if (decoder?.IsDisposed == false) decoder.Dispose();
                if (stream is not null) TryCatch(stream.Dispose);
                if (bmp?.IsDisposed == false) bmp.Dispose();

                throw;
            }
        }

        private IComObject<ID2D1Bitmap> LoadBitmapFromFile(IComObject<ID2D1RenderTarget> device, string path)
        {
            return LoadBitmapFromMemory(device, File.ReadAllBytes(path));
        }

        private static void TryCatch(Action action)
        {
            ArgumentNullException.ThrowIfNull(action);

            try
            {
                action();
            }
            catch
            {
            }
        }

        #region IDisposable Support

        private bool _disposed;

        /// <summary>
        ///     Releases all resources used by this Image.
        /// </summary>
        /// <param name="disposing">A Boolean value indicating whether this is called from the destructor.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            Bitmap?.Dispose();
            Bitmap = null;
            _disposed = true;
        }

        /// <summary>
        ///     Releases all resources used by this Image.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
