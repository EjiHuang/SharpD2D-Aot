using System;
using DirectN;
using DirectN.Extensions;
using DirectN.Extensions.Com;

namespace SharpD2D.Drawing
{
    /// <summary>
    ///     Defines a particular format for text, including font family name, size, and style attributes.
    /// </summary>
    public class Font : IDisposable
    {
        /// <summary>
        ///     A DirectWrite TextFormat.
        /// </summary>
        public IComObject<IDWriteTextFormat> TextFormat;

        private Font()
        {
        }

        /// <summary>
        ///     Initializes a new Font by using the given text format.
        /// </summary>
        /// <param name="textFormat"></param>
        public Font(IComObject<IDWriteTextFormat> textFormat)
        {
            TextFormat = textFormat ?? throw new ArgumentNullException(nameof(textFormat));
        }

        /// <summary>
        ///     Initializes a new Font by using the specified name and style.
        /// </summary>
        /// <param name="factory">The IDWriteFactory from your Graphics device.</param>
        /// <param name="fontFamilyName">The name of the font family.</param>
        /// <param name="size">The size of this Font.</param>
        /// <param name="bold">A Boolean value indicating whether this Font is bold.</param>
        /// <param name="italic">A Boolean value indicating whether this Font is italic.</param>
        /// <param name="wordWrapping">A Boolean value indicating whether this Font uses word wrapping.</param>
        public Font(IComObject<IDWriteFactory> factory, string fontFamilyName, float size, bool bold = false, bool italic = false,
            bool wordWrapping = false)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (string.IsNullOrEmpty(fontFamilyName)) throw new ArgumentNullException(nameof(fontFamilyName));

            var weight = bold ? DWRITE_FONT_WEIGHT.DWRITE_FONT_WEIGHT_BOLD : DWRITE_FONT_WEIGHT.DWRITE_FONT_WEIGHT_NORMAL;
            var style = italic ? DWRITE_FONT_STYLE.DWRITE_FONT_STYLE_ITALIC : DWRITE_FONT_STYLE.DWRITE_FONT_STYLE_NORMAL;
            var wrapping = wordWrapping ? DWRITE_WORD_WRAPPING.DWRITE_WORD_WRAPPING_WRAP : DWRITE_WORD_WRAPPING.DWRITE_WORD_WRAPPING_NO_WRAP;

            TextFormat = factory.CreateTextFormat(fontFamilyName, size, null, weight, style, DWRITE_FONT_STRETCH.DWRITE_FONT_STRETCH_NORMAL);
            TextFormat.Object.SetWordWrapping(wrapping);
        }

        /// <summary>
        ///     Gets a value that indicates whether this Font is bold.
        /// </summary>
        public bool Bold => TextFormat.Object.GetFontWeight() == DWRITE_FONT_WEIGHT.DWRITE_FONT_WEIGHT_BOLD;

        /// <summary>
        ///     Gets a value that indicates whether this Font is italic.
        /// </summary>
        public bool Italic => TextFormat.Object.GetFontStyle() == DWRITE_FONT_STYLE.DWRITE_FONT_STYLE_ITALIC;

        /// <summary>
        ///     Enables or disables word wrapping for this Font.
        /// </summary>
        public bool WordWeapping
        {
            get => TextFormat.Object.GetWordWrapping() == DWRITE_WORD_WRAPPING.DWRITE_WORD_WRAPPING_WRAP;
            set => TextFormat.Object.SetWordWrapping(value ? DWRITE_WORD_WRAPPING.DWRITE_WORD_WRAPPING_WRAP : DWRITE_WORD_WRAPPING.DWRITE_WORD_WRAPPING_NO_WRAP);
        }

        /// <summary>
        ///     Gets the size of this Font measured in pixels.
        /// </summary>
        public float FontSize => TextFormat.Object.GetFontSize();

        /// <summary>
        ///     Gets the name of this Fonts family
        /// </summary>
        public string FontFamilyName
        {
            get
            {
                var len = TextFormat.Object.GetFontFamilyNameLength();
                var chars = new char[len + 1];
                unsafe
                {
                    fixed (char* p = chars)
                    {
                        TextFormat.Object.GetFontFamilyName(new PWSTR((nint)p), len + 1);
                    }
                }
                return new string(chars, 0, (int)len);
            }
        }

        /// <summary>
        ///     Allows an object to try to free resources and perform other cleanup operations before it is reclaimed by garbage
        ///     collection.
        /// </summary>
        ~Font()
        {
            Dispose(false);
        }

        /// <summary>
        ///     Returns a value indicating whether this instance and a specified <see cref="T:System.Object" /> represent the same
        ///     type and value.
        /// </summary>
        /// <param name="obj">The object to compare with this instance.</param>
        /// <returns>
        ///     <see langword="true" /> if <paramref name="obj" /> is a Font and equal to this instance; otherwise,
        ///     <see langword="false" />.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj is Font value)
                return value.TextFormat == TextFormat;
            return false;
        }

        /// <summary>
        ///     Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return TextFormat.GetHashCode();
        }

        /// <summary>
        ///     Converts this Font to a human-readable string.
        /// </summary>
        /// <returns>A string representation of this Font.</returns>
        public override string ToString()
        {
            return OverrideHelper.ToString(
                "Font", "Font",
                "FontFamilyName", FontFamilyName);
        }

        #region IDisposable Support

        private bool disposedValue;

        /// <summary>
        ///     Releases all resources used by this Font.
        /// </summary>
        /// <param name="disposing">A Boolean value indicating whether this is called from the destructor.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (TextFormat != null)
                {
                    TextFormat.Dispose();
                    TextFormat = null;
                }

                disposedValue = true;
            }
        }

        /// <summary>
        ///     Releases all resources used by this Font.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
