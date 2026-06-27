using System;
using DirectN;
using DirectN.Extensions;
using DirectN.Extensions.Com;

namespace SharpD2D.Drawing
{
    /// <summary>
    ///     Represents a Geometry which can be drawn by a Graphics device.
    /// </summary>
    public class Geometry : IDisposable
    {
        private IComObject<ID2D1PathGeometry> _geometry;
        private IComObject<ID2D1SimplifiedGeometrySink> _sink;

        private Geometry()
        {
        }

        /// <summary>
        ///     Initializes a new Geometry using a Graphics device.
        /// </summary>
        /// <param name="device"></param>
        public Geometry(Graphics device)
        {
            if (device == null) throw new ArgumentNullException(nameof(device));
            if (!device.IsInitialized)
                throw new InvalidOperationException("The render target needs to be initialized first");

            _geometry = device.GetFactory().CreatePathGeometry();
            _sink = _geometry.Open();

            IsOpen = true;
        }

        /// <summary>
        ///     Determines whether this Geometry is open.
        /// </summary>
        public bool IsOpen { get; private set; }

        /// <summary>
        ///     Gets the native geometry.
        /// </summary>
        public IComObject<ID2D1PathGeometry> NativeGeometry => _geometry;

        /// <summary>
        ///     Allows an object to try to free resources and perform other cleanup operations before it is reclaimed by garbage
        ///     collection.
        /// </summary>
        ~Geometry()
        {
            Dispose(false);
        }

        /// <summary>
        ///     Starts a new figure within this Geometry using a starting point.
        /// </summary>
        /// <param name="point">The start point for this figure.</param>
        /// <param name="fill">A Boolean value determining whether this figure can be filled by a Graphics device.</param>
        public void BeginFigure(PointF point, bool fill = false)
        {
            _sink.Object.BeginFigure(point, fill ? D2D1_FIGURE_BEGIN.D2D1_FIGURE_BEGIN_FILLED : D2D1_FIGURE_BEGIN.D2D1_FIGURE_BEGIN_HOLLOW);
        }

        /// <summary>
        ///     Starts a new figure within this Geometry using a starting line.
        /// </summary>
        /// <param name="line">The first line within this figure.</param>
        /// <param name="fill">A Boolean value determining whether this figure can be filled by a Graphics device.</param>
        public void BeginFigure(Line line, bool fill = false)
        {
            _sink.Object.BeginFigure(line.Start, fill ? D2D1_FIGURE_BEGIN.D2D1_FIGURE_BEGIN_FILLED : D2D1_FIGURE_BEGIN.D2D1_FIGURE_BEGIN_HOLLOW);
            _sink.Object.AddLine(line.End);
        }

        /// <summary>
        ///     Ends the currently started figure.
        /// </summary>
        /// <param name="closed">
        ///     A Boolean value indicating whether this figure should automatically be closed by the Graphics
        ///     device.
        /// </param>
        public void EndFigure(bool closed = true)
        {
            _sink.Object.EndFigure(closed ? D2D1_FIGURE_END.D2D1_FIGURE_END_CLOSED : D2D1_FIGURE_END.D2D1_FIGURE_END_OPEN);
        }

        /// <summary>
        ///     Adds a new PointF within the current figure.
        /// </summary>
        /// <param name="point">A PointF which will be added to this figure</param>
        public void AddPoint(PointF point)
        {
            _sink.Object.AddLine(point);
        }

        /// <summary>
        ///     Creates a new figure from a RectangleF.
        /// </summary>
        /// <param name="rectangle">The RectangleF used to create a new figure.</param>
        /// <param name="fill">A Boolean value determining whether this figure can be filled by a Graphics device.</param>
        public void AddRectangle(RectangleF rectangle, bool fill = false)
        {
            _sink.Object.BeginFigure(new D2D_POINT_2F(rectangle.Left, rectangle.Top),
                fill ? D2D1_FIGURE_BEGIN.D2D1_FIGURE_BEGIN_FILLED : D2D1_FIGURE_BEGIN.D2D1_FIGURE_BEGIN_HOLLOW);
            _sink.Object.AddLine(new D2D_POINT_2F(rectangle.Right, rectangle.Top));
            _sink.Object.AddLine(new D2D_POINT_2F(rectangle.Right, rectangle.Bottom));
            _sink.Object.AddLine(new D2D_POINT_2F(rectangle.Left, rectangle.Bottom));
            _sink.Object.EndFigure(D2D1_FIGURE_END.D2D1_FIGURE_END_CLOSED);
        }

        /// <summary>
        ///     Closes this Geometry.
        /// </summary>
        public void Close()
        {
            if (!IsOpen) return;

            _sink.Object.Close();
            _sink.Dispose();
            _sink = null;

            IsOpen = false;
        }

        private bool _disposed;

        /// <summary>
        ///     Releases all resources used by this Geometry.
        /// </summary>
        /// <param name="disposing">A Boolean value indicating whether this is called from the destructor.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                if (IsOpen) Close();
                _geometry?.Dispose();
                _geometry = null;
            }
            _disposed = true;
        }

        /// <summary>
        ///     Releases all resources used by this Geometry.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
