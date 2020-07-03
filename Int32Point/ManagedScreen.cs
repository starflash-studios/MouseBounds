using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;

namespace MouseBounds {
    public readonly struct ManagedScreen : IEquatable<ManagedScreen> {
        public readonly Screen Screen;
        public readonly Rect WorkingArea;
        public readonly Resolution Resolution;
        public readonly string ScreenName;
        public readonly bool Primary;

        const string DisplayPrefix = @"\\.\";

        public ManagedScreen(Screen Screen) {
            this.Screen = Screen;
            ScreenName = Screen.DeviceName;
            if (ScreenName.StartsWith(DisplayPrefix)) {
                ScreenName = ScreenName.Substring(DisplayPrefix.Length);
            }
            Primary = Screen.Primary;

            WorkingArea = RectangleToRect(Screen.WorkingArea);
            Resolution = new Resolution(Screen);
        }

        public static Rect RectangleToRect(Rectangle R) => new Rect(R.X, R.Y, R.Width, R.Height);

        public override string ToString() => Primary ? ScreenName + " [Primary]" : ScreenName;

        #region Equality Members

        public override bool Equals(object Obj) => Obj is ManagedScreen ObjScreen && Equals(ObjScreen);

        public override int GetHashCode() {
            unchecked {
                int HashCode = (Screen != null ? Screen.GetHashCode() : 0);
                HashCode = (HashCode * 397) ^ WorkingArea.GetHashCode();
                HashCode = (HashCode * 397) ^ Resolution.GetHashCode();
                HashCode = (HashCode * 397) ^ (ScreenName != null ? ScreenName.GetHashCode() : 0);
                HashCode = (HashCode * 397) ^ Primary.GetHashCode();
                return HashCode;
            }
        }

        public bool Equals(ManagedScreen Other) =>
            EqualityComparer<Screen>.Default.Equals(Screen, Other.Screen) &&
            EqualityComparer<Rect>.Default.Equals(WorkingArea, Other.WorkingArea) &&
            Resolution.Equals(Other.Resolution) &&
            ScreenName == Other.ScreenName &&
            Primary == Other.Primary;

        public static bool operator ==(ManagedScreen Left, ManagedScreen Right) => Left.Equals(Right);

        public static bool operator !=(ManagedScreen Left, ManagedScreen Right) => !(Left == Right);

        #endregion
    }
}
