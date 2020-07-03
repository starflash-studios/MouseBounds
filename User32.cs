using System.Runtime.InteropServices;

namespace MouseBounds {
    internal class User32 {
        [DllImport("User32.dll")]
        internal static extern bool SetCursorPos(int X, int Y);

        [DllImport("User32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Int32Point Pt);

        public static Int32Point Cursor {
            get {
                Int32Point Point = new Int32Point(0, 0);
                GetCursorPos(ref Point);
                return Point;
            }
            set => SetCursorPos(value.X, value.Y);
        }
    }
}
