using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;

//Avoid directly importing the System.Windows.Forms library in a WPF application
using Point = System.Windows.Point;

namespace MouseBounds {
    public readonly struct Resolution : IEquatable<Resolution> {
        public readonly int Width;
        public readonly int Height;

        public readonly int Left;
        public readonly int Right;
        public readonly int Bottom;
        public readonly int Top;

        public Resolution(int Width, int Height) {
            this.Width = Width;
            this.Height = Height;

            Left = 0;
            Right = Width;
            Bottom = 0;
            Top = Height;
        }

        public Resolution(Rect Bounds) {
            Width = (int)Bounds.Width;
            Height = (int)Bounds.Height;

            Left = (int)Bounds.X;
            Right = Left + Width;
            Bottom = (int)Bounds.Y; //Rects are calculated from Top-Left corner; we want the Bottom-Left. (Flip Y-Axis)
            Top = Bottom + Height;
        }

        public Resolution(int Left, int Right, int Bottom, int Top) {
            Width = Math.Abs(Right - Left);
            Height = Math.Abs(Top - Bottom);

            this.Left = Left;
            this.Right = Right;
            this.Bottom = Bottom;
            this.Top = Top;
        }

        public Resolution(Rectangle Rectangle) : this(new Rect(Rectangle.X, Rectangle.Y, Rectangle.Width, Rectangle.Height)) { }

        public Resolution(Screen Screen) : this(Screen.Bounds) { }

        #region Contains

        public static bool ContainsHorizontal(Int32Point Point, int Left, int Right) => Contains(Point.X, Left, Right);
        public bool ContainsHorizontal(Int32Point Point) => ContainsHorizontal(Point, Left, Right);

        public static bool ContainsVertical(Int32Point Point, int Bottom, int Top) => Contains(Point.Y, Bottom, Top);

        public bool ContainsVertical(Int32Point Point) => ContainsVertical(Point, Bottom, Top);

        public static bool Contains(Int32Point Point, int Left, int Right, int Bottom, int Top) => ContainsHorizontal(Point, Left, Right) && ContainsVertical(Point, Bottom, Top);

        public bool Contains(Int32Point Point) => Contains(Point, Left, Right, Bottom, Top);

        public static bool Contains(int Val, int Min, int Max) => Val >= Min && Val <= Max;

        #endregion

        #region Clamp

        public Point Clamp(Int32Point Point) => Contains(Point) ? Point : new Point(Clamp(Point.X, Left, Right), Clamp(Point.Y, Bottom, Top));

        public static int Clamp(int Val, int Min, int Max) => Val < Min ? Min : Val > Max ? Max : Val;

        #endregion

        #region Determine Side
        public ScreenSide DetermineHorizontalSide(Int32Point Point, float Sensitivity = 0.1f) => DetermineHorizontalSide(this, Point, Sensitivity);

        public static ScreenSide DetermineHorizontalSide(Resolution Res, Int32Point Point, float Sensitivity = 0.1f) {
            float RelativeX = (Point.X - Res.Left) / (float)Res.Width;
            if (RelativeX <= Sensitivity) {
                return ScreenSide.Left;
            }

            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (RelativeX >= 1.0f - Sensitivity) {
                return ScreenSide.Right;
            }

            return ScreenSide.None;
        }

        public ScreenSide DetermineVerticalSide(Int32Point Point, float Sensitivity = 0.1f) => DetermineVerticalSide(this, Point, Sensitivity);

        public static ScreenSide DetermineVerticalSide(Resolution Res, Int32Point Point, float Sensitivity = 0.1f) {
            float RelativeY = (Point.Y - Res.Bottom) / (float)Res.Height;
            if (RelativeY <= Sensitivity) {
                return ScreenSide.Bottom;
            }

            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (RelativeY >= 1.0f - Sensitivity) {
                return ScreenSide.Top;
            }

            return ScreenSide.None;
        }

        #endregion

        public override string ToString() => $"{Width:N0}x{Height:N0} ({Left}≤𝑥≤{Right}, {Bottom}≤𝑦≤{Top})";

        public static explicit operator Rect(Resolution Res) => new Rect(Res.Left, Res.Top, Res.Width, Res.Height);

        #region Equality Members

        public override bool Equals(object Obj) => Obj is Resolution Resolution && Equals(Resolution);

        public override int GetHashCode() {
            unchecked {
                int HashCode = Width;
                HashCode = (HashCode * 397) ^ Height;
                HashCode = (HashCode * 397) ^ Left;
                HashCode = (HashCode * 397) ^ Right;
                HashCode = (HashCode * 397) ^ Bottom;
                HashCode = (HashCode * 397) ^ Top;
                return HashCode;
            }
        }

        public bool Equals(Resolution Other) =>
            Width == Other.Width &&
            Height == Other.Height &&
            Left == Other.Left &&
            Right == Other.Right &&
            Bottom == Other.Bottom &&
            Top == Other.Top;

        public static bool operator ==(Resolution Left, Resolution Right) => Left.Equals(Right);

        public static bool operator !=(Resolution Left, Resolution Right) => !(Left == Right);

        #endregion
    }

    public enum ScreenSide {
        None = 0,
        Left = 1,
        Right = 2,
        Bottom = 3,
        Top = 4,
        Center = 5
    }
}
