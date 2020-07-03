using System;
using System.Diagnostics;
using System.Windows;

namespace MouseBounds {
    public struct Int32Point : IEquatable<Int32Point>, IComparable<Int32Point>, IComparable {

        public int X;
        public int Y;

        public Int32Point(int X = 0, int Y = 0) {
            this.X = X;
            this.Y = Y;
        }

        public Int32Point(double X = 0, double Y = 0) {
            this.X = (int)X;
            this.Y = (int)Y;
        }

        #region Mathematical Functions

        public Int32Point Abs() => new Int32Point(Math.Abs(X), Math.Abs(Y));

        public Int32Point Clamp(Rect Bounds) => Clamp((int)Bounds.Left, (int)Bounds.Right, (int)Bounds.Bottom, (int)Bounds.Top);

        public Int32Point Clamp(Int32Rect Bounds) {
            int Left = Bounds.X;
            int Top = Bounds.Y;

            return Clamp(Left, Left + Bounds.Width, Top - Bounds.Height, Top);
        }

        public Int32Point Clamp(double Left, double Right, double Bottom, double Top) => new Int32Point(Clamp(X, (int)Left, (int)Right), Clamp(Y, (int)Bottom, (int)Top));

        public Int32Point Clamp(int Left, int Right, int Bottom, int Top) => new Int32Point(Clamp(X, Left, Right), Clamp(Y, Bottom, Top));

        internal static int Clamp(int Value, int Min, int Max) => Value < Min ? Min : Value > Max ? Max : Value;

        internal static bool Exceeds(int Value, int Min, int Max) => Value < Min || Value > Max;

        #endregion

        #region Operators

        #region Vector-Based methods

        public static Int32Point operator +(Int32Point A, Int32Point B) => new Int32Point(A.X + B.X, A.Y + B.Y);
        
        public static Int32Point operator -(Int32Point A, Int32Point B) => new Int32Point(A.X - B.X, A.Y - B.Y);
        
        public static Int32Point operator *(Int32Point A, Int32Point B) => new Int32Point(A.X * B.X, A.Y * B.Y);
        
        public static Int32Point operator /(Int32Point A, Int32Point B) => new Int32Point(A.X / B.X, A.Y / B.Y);

        #endregion

        #region Scalar-Based methods

        public static Int32Point operator +(Int32Point A, int B) => new Int32Point(A.X + B, A.Y + B);

        public static Int32Point operator -(Int32Point A, int B) => new Int32Point(A.X - B, A.Y - B);

        public static Int32Point operator *(Int32Point A, int B) => new Int32Point(A.X * B, A.Y * B);

        public static Int32Point operator /(Int32Point A, int B) => new Int32Point(A.X / B, A.Y / B);

        #endregion

        #region Conversions

        public static implicit operator Point(Int32Point IntPoint) => new Point(IntPoint.X, IntPoint.Y);

        public static explicit operator Int32Point(Point Point) => new Int32Point((int)Point.X, (int)Point.Y);

        public static implicit operator Int32Point((int X, int Y) Pos) => new Int32Point(Pos.X, Pos.Y);

        public static explicit operator (int X, int Y)(Int32Point IntPoint) => (IntPoint.X, IntPoint.Y);

        #endregion

        #endregion

        public override string ToString() => $"({X}, {Y})";

        #region Equality members

        public override int GetHashCode() {
            // ReSharper disable NonReadonlyMemberInGetHashCode
            unchecked { return(X * 397) ^ Y; }
            // ReSharper restore NonReadonlyMemberInGetHashCode
        }

        public override bool Equals(object Obj) => Obj is Int32Point Point && Equals(Point);

        public bool Equals(Int32Point Other) =>
            X == Other.X &&
            Y == Other.Y;

        public static bool operator ==(Int32Point Left, Int32Point Right) => Left.Equals(Right);

        public static bool operator !=(Int32Point Left, Int32Point Right) => !(Left == Right);

        #endregion

        #region Relational members

        /// <summary>Compares the current instance with another object of the same type and returns an integer that indicates whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object. </summary>
        /// <param name="Other">An object to compare with this instance. </param>
        /// <returns>A value that indicates the relative order of the objects being compared. The return value has these meanings: Value Meaning Less than zero This instance precedes <paramref name="Other" /> in the sort order.  Zero This instance occurs in the same position in the sort order as <paramref name="Other" />. Greater than zero This instance follows <paramref name="Other" /> in the sort order. </returns>
        public int CompareTo(Int32Point Other) {
            int XComparison = X.CompareTo(Other.X);
            return XComparison != 0 ? XComparison : Y.CompareTo(Other.Y);
        }

        /// <summary>Compares the current instance with another object of the same type and returns an integer that indicates whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object.</summary>
        /// <param name="Obj">An object to compare with this instance. </param>
        /// <returns>A value that indicates the relative order of the objects being compared. The return value has these meanings: Value Meaning Less than zero This instance precedes <paramref name="Obj" /> in the sort order. Zero This instance occurs in the same position in the sort order as <paramref name="Obj" />. Greater than zero This instance follows <paramref name="Obj" /> in the sort order. </returns>
        /// <exception cref="T:System.ArgumentException">
        /// <paramref name="Obj" /> is not the same type as this instance. </exception>
        public int CompareTo(object Obj) => Obj is null ? 1 : Obj is Int32Point Other ? CompareTo(Other) : throw new ArgumentException($"Object must be of type {nameof(Int32Point)}");

        /// <summary>Returns a value that indicates whether a <see cref="T:MouseBounds.Int32Point" /> value is less than another <see cref="T:MouseBounds.Int32Point" /> value.</summary>
        /// <param name="Left">The first value to compare.</param>
        /// <param name="Right">The second value to compare.</param>
        /// <returns>true if <paramref name="Left" /> is less than <paramref name="Right" />; otherwise, false.</returns>
        public static bool operator <(Int32Point Left, Int32Point Right) => Left.CompareTo(Right) < 0;

        /// <summary>Returns a value that indicates whether a <see cref="T:MouseBounds.Int32Point" /> value is greater than another <see cref="T:MouseBounds.Int32Point" /> value.</summary>
        /// <param name="Left">The first value to compare.</param>
        /// <param name="Right">The second value to compare.</param>
        /// <returns>true if <paramref name="Left" /> is greater than <paramref name="Right" />; otherwise, false.</returns>
        public static bool operator >(Int32Point Left, Int32Point Right) => Left.CompareTo(Right) > 0;

        /// <summary>Returns a value that indicates whether a <see cref="T:MouseBounds.Int32Point" /> value is less than or equal to another <see cref="T:MouseBounds.Int32Point" /> value.</summary>
        /// <param name="Left">The first value to compare.</param>
        /// <param name="Right">The second value to compare.</param>
        /// <returns>true if <paramref name="Left" /> is less than or equal to <paramref name="Right" />; otherwise, false.</returns>
        public static bool operator <=(Int32Point Left, Int32Point Right) => Left.CompareTo(Right) <= 0;

        /// <summary>Returns a value that indicates whether a <see cref="T:MouseBounds.Int32Point" /> value is greater than or equal to another <see cref="T:MouseBounds.Int32Point" /> value.</summary>
        /// <param name="Left">The first value to compare.</param>
        /// <param name="Right">The second value to compare.</param>
        /// <returns>true if <paramref name="Left" /> is greater than <paramref name="Right" />; otherwise, false.</returns>
        public static bool operator >=(Int32Point Left, Int32Point Right) => Left.CompareTo(Right) >= 0;

#endregion

    };
}
