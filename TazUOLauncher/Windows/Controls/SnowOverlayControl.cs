using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Generic;

namespace TazUOLauncher
{
    public class SnowOverlayControl : Control
    {
        private readonly Rect bounds;
        private readonly List<Snowflake> _flakes = new();
        private readonly Random _rand = new();
        private readonly DispatcherTimer _timer;
        private bool _initialized;

        public SnowOverlayControl(Rect bounds)
        {
            this.bounds = bounds;
            IsHitTestVisible = false;
            ClipToBounds = true;

            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
            _timer.Tick += (_, _) =>
            {
                UpdateFlakes();
                InvalidateVisual();
            };
            _timer.Start();
        }

        // ✅ Safe measure handling: replace Infinity with finite values
        protected override Size MeasureOverride(Size availableSize)
        {
            double width = bounds.Width;
            double height = bounds.Height;
            return new Size(width, height);
        }

        protected override Size ArrangeOverride(Size finalSize) => finalSize;

        private void InitFlakes()
        {
            if (_initialized || Bounds.Width <= 0 || Bounds.Height <= 0)
                return;

            _flakes.Clear();
            for (int i = 0; i < 35; i++)
            {
                _flakes.Add(new Snowflake
                {
                    X = _rand.NextDouble() * Bounds.Width,
                    Y = _rand.NextDouble() * Bounds.Height,
                    Size = _rand.Next(2, 6),
                    Speed = _rand.NextDouble() * 1.2 + 0.4,
                    Drift = _rand.NextDouble() * 0.6 - 0.3
                });
            }

            _initialized = true;
        }

        private void UpdateFlakes()
        {
            if (!_initialized)
                return;

            foreach (var f in _flakes)
            {
                f.Y += f.Speed;
                f.X += f.Drift;

                if (f.Y > Bounds.Height)
                {
                    f.Y = -f.Size;
                    f.X = _rand.NextDouble() * Bounds.Width;
                }

                if (f.X < -f.Size) f.X = Bounds.Width + f.Size;
                if (f.X > Bounds.Width + f.Size) f.X = -f.Size;
            }
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);
            InitFlakes();

            foreach (var f in _flakes)
                context.DrawEllipse(Brushes.White, null, new Point(f.X, f.Y), f.Size, f.Size);
        }

        private class Snowflake
        {
            public double X, Y, Size, Speed, Drift;
        }
    }
    }