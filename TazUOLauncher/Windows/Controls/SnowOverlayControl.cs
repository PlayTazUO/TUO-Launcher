using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Collections.Generic;

namespace TazUOLauncher
{
    /// <summary>
    /// Static snow hills drawn along the bottom of the window, above the background image
    /// but below the rest of the UI.
    /// </summary>
    public class SnowOverlayControl : Control
    {
        private readonly Rect bounds;
        private readonly List<(StreamGeometry Geometry, IBrush Brush)> _hills = new();
        private bool _initialized;

        public SnowOverlayControl(Rect bounds)
        {
            this.bounds = bounds;
            IsHitTestVisible = false;
            ClipToBounds = true;

            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch;
        }

        protected override Size MeasureOverride(Size availableSize)
            => new Size(bounds.Width, bounds.Height);

        protected override Size ArrangeOverride(Size finalSize) => finalSize;

        private void InitHills()
        {
            if (_initialized || Bounds.Width <= 0 || Bounds.Height <= 0)
                return;

            _initialized = true;

            double w = Bounds.Width;
            double h = Bounds.Height;
            var rand = new Random(202512);

            // Back to front: distant hills are taller, lighter and bluer.
            _hills.Add((BuildHill(w, h, rand, 50, 7), new SolidColorBrush(Color.FromRgb(0xCB, 0xE2, 0xFF))));
            _hills.Add((BuildHill(w, h, rand, 34, 6), new SolidColorBrush(Color.FromRgb(0xE4, 0xF1, 0xFF))));
            _hills.Add((BuildHill(w, h, rand, 21, 5), Brushes.White));
        }

        private static StreamGeometry BuildHill(double width, double height, Random rand,
            double baseHeight, double amplitude)
        {
            double f1 = 0.006 + rand.NextDouble() * 0.004;
            double f2 = 0.018 + rand.NextDouble() * 0.012;
            double p1 = rand.NextDouble() * Math.PI * 2;
            double p2 = rand.NextDouble() * Math.PI * 2;

            double Surface(double x) =>
                height - baseHeight
                       - amplitude * Math.Sin(x * f1 + p1)
                       - amplitude * 0.5 * Math.Sin(x * f2 + p2);

            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(new Point(0, height), true);

                const double step = 6;
                for (double x = step; x < width; x += step)
                    ctx.LineTo(new Point(x, Surface(x)));

                ctx.LineTo(new Point(width, Surface(width)));
                ctx.LineTo(new Point(width, height));
                ctx.EndFigure(true);
            }

            return geometry;
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);
            InitHills();

            foreach (var (geometry, brush) in _hills)
                context.DrawGeometry(brush, null, geometry);
        }
    }
}
