using System;
using System.Collections.Generic;
using DirectN;
using DirectN.Extensions;
using DirectN.Extensions.Com;

namespace SharpD2D.Drawing.Imaging
{
    internal static class ImageDecoder
    {
        private static readonly Guid[] _floatingPointFormats =
        {
            Constants.GUID_WICPixelFormat128bppRGBAFloat,
            Constants.GUID_WICPixelFormat128bppRGBAFixedPoint,
            Constants.GUID_WICPixelFormat128bppPRGBAFloat,
            Constants.GUID_WICPixelFormat128bppRGBFloat,
            Constants.GUID_WICPixelFormat128bppRGBFixedPoint,
            Constants.GUID_WICPixelFormat96bppRGBFixedPoint,
            Constants.GUID_WICPixelFormat96bppRGBFloat,
            Constants.GUID_WICPixelFormat64bppBGRAFixedPoint,
            Constants.GUID_WICPixelFormat64bppRGBAFixedPoint,
            Constants.GUID_WICPixelFormat64bppRGBFixedPoint,
            Constants.GUID_WICPixelFormat48bppRGBFixedPoint,
            Constants.GUID_WICPixelFormat48bppBGRFixedPoint,
            Constants.GUID_WICPixelFormat32bppGrayFixedPoint,
            Constants.GUID_WICPixelFormat32bppGrayFloat,
            Constants.GUID_WICPixelFormat16bppGrayFixedPoint
        };

        // Pixel formats sorted in a best compatibility and best color accuracy order
        private static readonly Guid[] _standardPixelFormats =
        {
            Constants.GUID_WICPixelFormat144bpp8ChannelsAlpha,
            Constants.GUID_WICPixelFormat128bpp8Channels,
            Constants.GUID_WICPixelFormat128bpp7ChannelsAlpha,
            Constants.GUID_WICPixelFormat112bpp7Channels,
            Constants.GUID_WICPixelFormat112bpp6ChannelsAlpha,
            Constants.GUID_WICPixelFormat96bpp6Channels,
            Constants.GUID_WICPixelFormat96bpp5ChannelsAlpha,
            Constants.GUID_WICPixelFormat80bpp5Channels,
            Constants.GUID_WICPixelFormat80bppCMYKAlpha,
            Constants.GUID_WICPixelFormat80bpp4ChannelsAlpha,
            Constants.GUID_WICPixelFormat72bpp8ChannelsAlpha,
            Constants.GUID_WICPixelFormat64bppBGRA,
            Constants.GUID_WICPixelFormat64bppRGBA,
            Constants.GUID_WICPixelFormat64bppPBGRA,
            Constants.GUID_WICPixelFormat64bppPRGBA,
            Constants.GUID_WICPixelFormat64bpp8Channels,
            Constants.GUID_WICPixelFormat64bpp4Channels,
            Constants.GUID_WICPixelFormat64bppRGBAHalf,
            Constants.GUID_WICPixelFormat64bppPRGBAHalf,
            Constants.GUID_WICPixelFormat64bpp7ChannelsAlpha,
            Constants.GUID_WICPixelFormat64bpp3ChannelsAlpha,
            Constants.GUID_WICPixelFormat64bppRGB,
            Constants.GUID_WICPixelFormat64bppCMYK,
            Constants.GUID_WICPixelFormat64bppRGBHalf,
            Constants.GUID_WICPixelFormat56bpp7Channels,
            Constants.GUID_WICPixelFormat56bpp6ChannelsAlpha,
            Constants.GUID_WICPixelFormat48bpp6Channels,
            Constants.GUID_WICPixelFormat48bppRGB,
            Constants.GUID_WICPixelFormat48bppBGR,
            Constants.GUID_WICPixelFormat48bpp3Channels,
            Constants.GUID_WICPixelFormat48bppRGBHalf,
            Constants.GUID_WICPixelFormat48bpp5ChannelsAlpha,
            Constants.GUID_WICPixelFormat40bpp5Channels,
            Constants.GUID_WICPixelFormat40bppCMYKAlpha,
            Constants.GUID_WICPixelFormat40bpp4ChannelsAlpha,
            Constants.GUID_WICPixelFormat32bppBGRA,
            Constants.GUID_WICPixelFormat32bppRGBA,
            Constants.GUID_WICPixelFormat32bppPBGRA,
            Constants.GUID_WICPixelFormat32bppPRGBA,
            Constants.GUID_WICPixelFormat32bppRGBA1010102,
            Constants.GUID_WICPixelFormat32bppRGBA1010102XR,
            Constants.GUID_WICPixelFormat32bppCMYK,
            Constants.GUID_WICPixelFormat32bpp4Channels,
            Constants.GUID_WICPixelFormat32bpp3ChannelsAlpha,
            Constants.GUID_WICPixelFormat32bppBGR,
            Constants.GUID_WICPixelFormat32bppRGB,
            Constants.GUID_WICPixelFormat32bppRGBE,
            Constants.GUID_WICPixelFormat32bppBGR101010,
            Constants.GUID_WICPixelFormat24bpp3Channels,
            Constants.GUID_WICPixelFormat24bppBGR,
            Constants.GUID_WICPixelFormat24bppRGB,
            Constants.GUID_WICPixelFormat16bppBGR555,
            Constants.GUID_WICPixelFormat16bppBGR565,
            Constants.GUID_WICPixelFormat16bppBGRA5551,
            Constants.GUID_WICPixelFormat16bppGray,
            Constants.GUID_WICPixelFormat16bppGrayHalf,
            Constants.GUID_WICPixelFormat16bppCbCr,
            Constants.GUID_WICPixelFormat16bppYQuantizedDctCoefficients,
            Constants.GUID_WICPixelFormat16bppCbQuantizedDctCoefficients,
            Constants.GUID_WICPixelFormat16bppCrQuantizedDctCoefficients,
            Constants.GUID_WICPixelFormat8bppIndexed,
            Constants.GUID_WICPixelFormat8bppAlpha,
            Constants.GUID_WICPixelFormat8bppY,
            Constants.GUID_WICPixelFormat8bppCb,
            Constants.GUID_WICPixelFormat8bppCr,
            Constants.GUID_WICPixelFormat8bppGray
        };

        private static readonly Guid[] _uncommonFormats =
        {
            Constants.GUID_WICPixelFormat4bppIndexed,
            Constants.GUID_WICPixelFormat2bppIndexed,
            Constants.GUID_WICPixelFormat1bppIndexed,
            Constants.GUID_WICPixelFormat4bppGray,
            Constants.GUID_WICPixelFormat2bppGray,
            Constants.GUID_WICPixelFormatDontCare,
            Constants.GUID_WICPixelFormatBlackWhite
        };

        private static IEnumerable<Guid> _pixelFormatEnumerator
        {
            get
            {
                foreach (var format in _standardPixelFormats) yield return format;

                foreach (var format in _floatingPointFormats) yield return format;

                foreach (var format in _uncommonFormats) yield return format;
            }
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

        public static IComObject<ID2D1Bitmap> Decode(IComObject<ID2D1RenderTarget> device, IComObject<IWICBitmapDecoder> decoder)
        {
            ArgumentNullException.ThrowIfNull(device);
            ArgumentNullException.ThrowIfNull(decoder);

            var frame = decoder.GetFrame(0);

            // Convert frame to IWICBitmap first ¡ª WIC handles format conversion internally
            IComObject<IWICBitmap> wicBmp;
            try
            {
                wicBmp = WicImagingFactory.CreateBitmapFromSource(frame);
                frame.Dispose();
            }
            catch
            {
                frame.Dispose();
                throw;
            }

            // Try direct WIC¡úD2D conversion
            var hr = device.Object.CreateBitmapFromWicBitmap(wicBmp.Object!, 0, out var d2dBmp);
            if (hr.IsSuccess)
            {
                wicBmp.Dispose();
                return new ComObject<ID2D1Bitmap>(d2dBmp);
            }

            wicBmp.Dispose();

            // Fallback: use format converter with common pixel formats
            return DecodeFallback(device, decoder);
        }

        private static IComObject<ID2D1Bitmap> DecodeFallback(IComObject<ID2D1RenderTarget> device, IComObject<IWICBitmapDecoder> decoder)
        {
            var frame = decoder.GetFrame(0);

            foreach (var format in _pixelFormatEnumerator)
            {
                var converter = WicImagingFactory.CreateFormatConverter();
                IComObject<IWICPalette> palette = null;

                try
                {
                    palette = WicImagingFactory.CreatePalette();
                    palette.Object.InitializePredefined(WICBitmapPaletteType.WICBitmapPaletteTypeFixedHalftone256, 0);

                    var initHr = converter.Object.Initialize(frame.Object!, format,
                        WICBitmapDitherType.WICBitmapDitherTypeNone, palette.Object!, 0.0,
                        WICBitmapPaletteType.WICBitmapPaletteTypeCustom);
                    if (initHr.IsError)
                        continue;

                    var bmpHr = device.Object.CreateBitmapFromWicBitmap(converter.Object!, 0, out var bmp);
                    if (bmpHr.IsError)
                        continue;

                    TryCatch(converter.Dispose);
                    TryCatch(palette.Dispose);
                    TryCatch(frame.Dispose);

                    return new ComObject<ID2D1Bitmap>(bmp);
                }
                finally
                {
                    TryCatch(() => converter?.Dispose());
                    TryCatch(() => palette?.Dispose());
                }
            }

            TryCatch(frame.Dispose);
            throw new Exception("Unsupported Image Format!");
        }
    }
}
