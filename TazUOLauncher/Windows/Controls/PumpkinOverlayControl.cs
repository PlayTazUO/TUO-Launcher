using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;

namespace TazUOLauncher
{
    /// <summary>
    /// A static field of pumpkins sitting along the bottom of the window, above the
    /// background image but below the rest of the UI.
    /// </summary>
    public class PumpkinOverlayControl : Control
    {
        private readonly Rect bounds;
        private readonly List<Pumpkin> _pumpkins = new();
        private readonly Dictionary<int, RenderTargetBitmap> _sprites = new();
        private bool _initialized;

        private static readonly IBrush PumpkinBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x75, 0x18));
        private static readonly IPen PumpkinPen = new Pen(new SolidColorBrush(Color.FromRgb(0xC4, 0x4E, 0x0A)), 1);
        private static readonly IBrush StemBrush = new SolidColorBrush(Color.FromRgb(0x4A, 0x7A, 0x1E));
        private static readonly IBrush FaceBrush = new SolidColorBrush(Color.FromRgb(0x2B, 0x14, 0x00));

        public PumpkinOverlayControl(Rect bounds)
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

        private void InitField()
        {
            if (_initialized || Bounds.Width <= 0 || Bounds.Height <= 0)
                return;

            _initialized = true;

            double w = Bounds.Width;
            double h = Bounds.Height;
            var rand = new Random(202410);

            int columns = Math.Max(6, (int)(w / 70));
            double columnWidth = w / columns;

            // Back row: smaller and further up.
            for (int i = 0; i < columns; i++)
            {
                double size = 8 + rand.NextDouble() * 5;
                double x = (i + 0.5) * columnWidth + (rand.NextDouble() - 0.5) * columnWidth * 0.5;
                double lift = 10 + rand.NextDouble() * 8;
                AddPumpkin(x, h - size * 0.85 - lift, size, rand);
            }

            // Front row: larger and sitting on the bottom edge.
            for (int i = 0; i < columns; i++)
            {
                double size = 12 + rand.NextDouble() * 8;
                double x = (i + 0.35) * columnWidth + (rand.NextDouble() - 0.5) * columnWidth * 0.5;
                double lift = rand.NextDouble() * 5;
                AddPumpkin(x, h - size * 0.85 - lift, size, rand);
            }

            // Draw the highest (furthest back) pumpkins first.
            _pumpkins.Sort((a, b) => a.Y.CompareTo(b.Y));
        }

        private void AddPumpkin(double x, double y, double size, Random rand)
        {
            _pumpkins.Add(new Pumpkin
            {
                X = x,
                Y = y,
                Size = size,
                Rotation = (rand.NextDouble() - 0.5) * 0.2
            });
        }

        /// <summary>
        /// Renders a pumpkin of the given radius once into an offscreen bitmap, then reuses it.
        /// </summary>
        private RenderTargetBitmap GetSprite(int size)
        {
            if (_sprites.TryGetValue(size, out var cached))
                return cached;

            double r = size;
            // Padding so the outline and stem are never clipped; the sprite is square and
            // centered so it can simply be rotated around its middle at draw time.
            int pixels = (int)Math.Ceiling(2 * (r * 1.4 + 2));

            var bitmap = new RenderTargetBitmap(new PixelSize(pixels, pixels), new Vector(96, 96));
            using (var ctx = bitmap.CreateDrawingContext())
            using (ctx.PushTransform(Matrix.CreateTranslation(pixels / 2.0, pixels / 2.0)))
            {
                ctx.DrawEllipse(PumpkinBrush, PumpkinPen, new Point(0, 0), r, r * 0.85);
                ctx.DrawGeometry(StemBrush, null, BuildPolygon(
                    new Point(-0.12 * r, -0.9 * r),
                    new Point(0, -1.3 * r),
                    new Point(0.12 * r, -0.9 * r)));
                ctx.DrawGeometry(FaceBrush, null, BuildFace(r));
            }

            _sprites[size] = bitmap;
            return bitmap;
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);
            InitField();

            foreach (var p in _pumpkins)
            {
                var sprite = GetSprite((int)Math.Round(p.Size));
                double half = sprite.PixelSize.Width / 2.0;

                using (context.PushTransform(
                    Matrix.CreateRotation(p.Rotation) * Matrix.CreateTranslation(p.X, p.Y)))
                {
                    context.DrawImage(sprite, new Rect(-half, -half, half * 2, half * 2));
                }
            }
        }

        private static GeometryGroup BuildFace(double r)
        {
            var face = new GeometryGroup();
            face.Children.Add(new EllipseGeometry(new Rect(-0.5 * r, -0.35 * r, 0.3 * r, 0.3 * r)));
            face.Children.Add(new EllipseGeometry(new Rect(0.2 * r, -0.35 * r, 0.3 * r, 0.3 * r)));
            face.Children.Add(BuildPolygon(
                new Point(-0.45 * r, 0.35 * r),
                new Point(-0.2 * r, 0.55 * r),
                new Point(0, 0.35 * r),
                new Point(0.2 * r, 0.55 * r),
                new Point(0.45 * r, 0.35 * r),
                new Point(0.3 * r, 0.65 * r),
                new Point(-0.3 * r, 0.65 * r)));
            return face;
        }

        private static StreamGeometry BuildPolygon(params Point[] points)
        {
            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(points[0], true);
                for (int i = 1; i < points.Length; i++)
                    ctx.LineTo(points[i]);
                ctx.EndFigure(true);
            }
            return geometry;
        }

        private class Pumpkin
        {
            public double X, Y, Size, Rotation;
        }
    }
}
