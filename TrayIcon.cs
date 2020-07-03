using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;

namespace MouseBounds {
    public struct TrayIcon : IEquatable<TrayIcon> {
        public string Title;
        public string Description;
        public Icon Icon;

        public TrayIcon(string Title = null, string Description = null, Icon Icon = null) {
            this.Title = Title ?? "";
            this.Description = Description ?? "";
            this.Icon = Icon;
        }

        public static implicit operator NotifyIcon(TrayIcon TrayIcon) => new NotifyIcon {
            Icon = TrayIcon.Icon,
            Text = TrayIcon.Description,
            BalloonTipText = TrayIcon.Description, //Vista
            BalloonTipTitle = TrayIcon.Title, //Vista
            Visible = true
        };
        
        public static explicit operator TrayIcon(NotifyIcon NotifyIcon) => new TrayIcon {
            Title = NotifyIcon.BalloonTipTitle,
            Description = NotifyIcon.Text,
            Icon = NotifyIcon.Icon
        };

        #region Equality members

        public override bool Equals(object Obj) => Obj is TrayIcon ObjIcon && Equals(ObjIcon);

        public override int GetHashCode() {
            // ReSharper disable NonReadonlyMemberInGetHashCode
            unchecked {
                int HashCode = (Title != null ? Title.GetHashCode() : 0);
                HashCode = (HashCode * 397) ^ (Description != null ? Description.GetHashCode() : 0);
                HashCode = (HashCode * 397) ^ (Icon != null ? Icon.GetHashCode() : 0);
                return HashCode;
            }
            // ReSharper restore NonReadonlyMemberInGetHashCode
        }

        public bool Equals(TrayIcon Other) =>
            Title == Other.Title &&
            Description == Other.Description &&
            EqualityComparer<Icon>.Default.Equals(Icon, Other.Icon);

        public static bool operator ==(TrayIcon Left, TrayIcon Right) => Left.Equals(Right);

        public static bool operator !=(TrayIcon Left, TrayIcon Right) => !(Left == Right);

        #endregion
    }

    public class Tray {
        public Window ActiveWindow;
        public NotifyIcon Notif;
        public bool MinimiseToTray;
        internal bool MaximiseOnDoubleClick;

        internal EventHandler Click;
        internal EventHandler DoubleClick;

        internal MouseEventHandler MouseClick;
        internal MouseEventHandler MouseDoubleClick;
        internal MouseEventHandler MouseDown;
        internal MouseEventHandler MouseMove;
        internal MouseEventHandler MouseUp;

        public delegate void DlgOnHideWindow();
        public delegate void DlgOnShowWindow();
        public DlgOnHideWindow OnHideWindow;
        public DlgOnShowWindow OnShowWindow;
        
        public Tray(Window ActiveWindow = null, NotifyIcon Notif = default, bool MinimiseToTray = false, bool MaximiseOnDoubleClick = true, EventHandler Click = null, EventHandler DoubleClick = null, MouseEventHandler MouseClick = null, MouseEventHandler MouseDoubleClick = null, MouseEventHandler MouseDown = null, MouseEventHandler MouseMove = null, MouseEventHandler MouseUp = null, DlgOnHideWindow OnHideWindow = null, DlgOnShowWindow OnShowWindow = null) {
            this.ActiveWindow = ActiveWindow;
            this.Notif = Notif;
            this.MinimiseToTray = MinimiseToTray;
            this.MaximiseOnDoubleClick = MaximiseOnDoubleClick;

            this.Click = Click;
            this.DoubleClick = DoubleClick;

            this.MouseClick = MouseClick;
            this.MouseDoubleClick = MouseDoubleClick;
            this.MouseDown = MouseDown;
            this.MouseMove = MouseMove;
            this.MouseUp = MouseUp;

            this.OnHideWindow = OnHideWindow;
            this.OnShowWindow = OnShowWindow;
        }

        public Icon Icon {
            get => Notif.Icon;
            set => Notif.Icon = value;
        }

        public void Setup() {
            if (ActiveWindow != null) { ActiveWindow.StateChanged += ActiveWindow_StateChanged; }
            if (MaximiseOnDoubleClick) {
                AddMouseDoubleClickHandler(Notif_ManagedMouseEvent);
            } else {
                AddMouseClickHandler(Notif_ManagedMouseEvent);
            }

            AddClickHandler(Click);
            AddDoubleClickHandler(DoubleClick);
            AddMouseClickHandler(MouseClick);
            AddMouseDoubleClickHandler(MouseDoubleClick);
            AddMouseDownHandler(MouseDown);
            AddMouseMoveHandler(MouseMove);
            AddMouseUpHandler(MouseUp);
        }
        
        public void Remove() {
            Debug.WriteLine("Removing...");
            if (Notif != null) {
                RemoveMouseClickHandler(Notif_ManagedMouseEvent);
                RemoveMouseDoubleClickHandler(Notif_ManagedMouseEvent);

                Notif.Visible = false;
                Notif.Dispose();
            }
            if (ActiveWindow != null) { ActiveWindow.StateChanged -= ActiveWindow_StateChanged; }
        }

        #region Handlers

        public void AddClickHandler(EventHandler Click) { if (Notif != null && Click != null) { Notif.Click += Click; } }

        public void RemoveClickHandler(EventHandler Click) { if (Notif != null && Click != null) { Notif.Click -= Click; } }

        public void AddDoubleClickHandler(EventHandler DoubleClick) { if (Notif != null && DoubleClick != null) { Notif.DoubleClick += DoubleClick; } }

        public void RemoveDoubleClickHandler(EventHandler DoubleClick) { if (Notif != null && DoubleClick != null) { Notif.DoubleClick -= DoubleClick; } }

        public void AddMouseClickHandler(MouseEventHandler MouseClick) { if (Notif != null && MouseClick != null) { Notif.MouseClick += MouseClick; } }

        public void RemoveMouseClickHandler(MouseEventHandler MouseClick) { if (Notif != null && MouseClick != null) { Notif.MouseClick -= MouseClick; } }

        public void AddMouseDoubleClickHandler(MouseEventHandler MouseDoubleClick) { if (Notif != null && MouseDoubleClick != null) { Notif.MouseDoubleClick += MouseDoubleClick; } }

        public void RemoveMouseDoubleClickHandler(MouseEventHandler MouseDoubleClick) { if (Notif != null && MouseDoubleClick != null) { Notif.MouseDoubleClick -= MouseDoubleClick; } }

        public void AddMouseDownHandler(MouseEventHandler MouseDown) { if (Notif != null && MouseDown != null) { Notif.MouseDown += MouseDown; } }

        public void RemoveMouseDownHandler(MouseEventHandler MouseDown) { if (Notif != null && MouseDown != null) { Notif.MouseDown -= MouseDown; } }

        public void AddMouseMoveHandler(MouseEventHandler MouseMove) { if (Notif != null && MouseMove != null) { Notif.MouseMove += MouseMove; } }

        public void RemoveMouseMoveHandler(MouseEventHandler MouseMove) { if (Notif != null && MouseMove != null) { Notif.MouseMove -= MouseMove; } }

        public void AddMouseUpHandler(MouseEventHandler MouseUp) { if (Notif != null && MouseUp != null) { Notif.MouseUp += MouseUp; } }

        public void RemoveMouseUpHandler(MouseEventHandler MouseUp) { if (Notif != null && MouseUp != null) { Notif.MouseUp -= MouseUp; } }

        #endregion

        #region Visibility Managers

        internal (double Left, double Top) ActiveWindowPosition;

        public void ShowJustTray() => Notif.Visible = true;

        public void ShowTray() {
            Notif.Visible = true;
            if (ActiveWindow != null) {
                ActiveWindowPosition = (ActiveWindow.Left, ActiveWindow.Top);
                Debug.WriteLine("WindowPos: " + ActiveWindowPosition.Left + ", " + ActiveWindowPosition.Top);
                ActiveWindow.WindowState = WindowState.Minimized;
                ActiveWindow.Hide();

                OnShowWindow?.Invoke();
            }
        }

        public void HideJustTray() => Notif.Visible = false;

        public void HideTray() {
            Notif.Visible = false;
            if (ActiveWindow != null) {
                ActiveWindow.WindowState = WindowState.Maximized; //Moves window to top
                ActiveWindow.Show();

                ActiveWindow.Left = ActiveWindowPosition.Left;
                ActiveWindow.Top = ActiveWindowPosition.Top;
                ActiveWindow.WindowState = WindowState.Normal; //Returns window to previous size

                OnHideWindow?.Invoke();
            }
        }

        #endregion

        #region 'Minimise to Tray' Managers


        //When the window's state changes to 'minimised' (user minimises the window); Ensure the window is properly hidden and show the TrayIcon
        internal void ActiveWindow_StateChanged(object Sender, EventArgs E) {
            if (MinimiseToTray) {
                // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
                switch (ActiveWindow?.WindowState) {
                    case WindowState.Minimized:
                        Debug.WriteLine("Window hidden; Hiding tray...");
                        //ActiveWindow.Hide();
                        ShowTray();
                        //OnHideWindow?.Invoke();
                        break;
                }
            }
        }

        //TrayIcon only visible when window hidden. Therefore, show TrayIcon
        internal void Notif_ManagedMouseEvent(object Sender, MouseEventArgs E) {
            if (MinimiseToTray) {
                // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
                switch (E.Button) {
                    case MouseButtons.Left:
                        Debug.WriteLine("Left-clicked NotifyIcon; Showing window...");

                        HideTray();
                        //ActiveWindow.Show();
                        //HideTray();
                        //// ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
                        //switch (ActiveWindow.WindowState) {
                        //    case WindowState.Minimized:
                        //        ActiveWindow.WindowState = WindowState.Maximized;
                        //        break;
                        //}

                        //OnShowWindow?.Invoke();
                        break;
                }
            }
        }

        #endregion
    }
}
