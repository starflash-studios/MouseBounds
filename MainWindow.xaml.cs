using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;

using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace MouseBounds {
    public partial class MainWindow {
        public bool LoopActive;

        public bool BoundsClamp;
        public bool AlwaysOnTop;
        
        public MainWindow() {
            InitializeComponent();

            UpdateScreens(out int PrimaryScreenIndex);
            ComboActiveScreen.SelectedIndex = PrimaryScreenIndex;

            BoundsClamp = CheckClamp.IsChecked ?? false;
            AlwaysOnTop = CheckAoT.IsChecked ?? false;

            SelectorHotKey.HotKey = new MahApps.Metro.Controls.HotKey(Key.F6);
            DirectHotkey = new DirectHotkey();
            ChangeHotkey(Key.F6);

            SetupTray();
            //TODO Initialise late piping of DirectX Key inputs during games such as F:NV
        }
        
        public void SafelyClose() {
            HotkeyThread?.Abort();
            TrayIcon.Remove();
        }

        #region Always On Top
        
        void MetroWindow_Deactivated(object Sender, EventArgs E) {
            if (Sender is Window SenderWindow) {
                Debug.WriteLine("Lost focus; (Deactivated) Setting as topmost...");
                SenderWindow.Topmost = AlwaysOnTop;
            }
        }

        #endregion

        #region Hotkey Detection/Thread Management

        public DirectHotkey DirectHotkey;
        public Key RequestedKey;

        internal Thread HotkeyThread;

        public void OnReceivedHotkey() {
            Debug.WriteLine("Received Hotkey.");
            ToggleLoop();
        }

        public void ChangeHotkey(Key NewKey) {
            Debug.WriteLine("Registering Hotkey...");

            HotkeyThread?.Abort();
            DirectHotkey.MonitoredKey = DirectHotkey.FromSystemKey(NewKey);
            Debug.WriteLine($"\tMonitoring: {NewKey} (SharpDX: {DirectHotkey.MonitoredKey})");

            //HotkeyThread = DirectHotkey.GetAsyncStateThread(30, OnReceivedHotkey);
            HotkeyThread = DirectHotkey.GetAsyncPollThread(30, OnReceivedHotkey);
            HotkeyThread.Start();
            Debug.WriteLine($"\tThread: {HotkeyThread} | Alive: {HotkeyThread.IsAlive}");

            Debug.WriteLine("\tHotkey registered.");
        }

        #endregion

        #region Tray Icon Management

        public Tray TrayIcon;

        void SetupTray() {
            TrayIcon = new Tray {
                ActiveWindow = this,
                Notif = new NotifyIcon {
                    Text = "Left Click to Open; Right Click to Toggle.",
                    Icon = Properties.Resources.Icon,
                    Visible = false
                },
                MouseClick = TrayIcon_MouseClick,
                MinimiseToTray = true,
                MaximiseOnDoubleClick = false
            };
            TrayIcon.Setup();
        }

        void TrayIcon_MouseClick(object Sender, System.Windows.Forms.MouseEventArgs E) {
            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (E.Button) {
                case MouseButtons.Right:
                    Debug.WriteLine("Right Clicked NotifyIcon; Toggling Loop...");
                    ToggleLoop();
                    break;
            }
        }

        #endregion

        #region Screen Resolution Management
        
        public Resolution ActiveResolution;
        public Resolution SafeResolution;

        public List<ManagedScreen> DisplayScreens;

        public void UpdateScreens(out int PrimaryIndex) {
            PrimaryIndex = 0;

            ComboActiveScreen.Items.Clear();
            DisplayScreens = new List<ManagedScreen>();
            Screen[] Screens = Screen.AllScreens;

            for (int S = 0; S < Screens.Length; S++) {
                ManagedScreen ManagedScreen = new ManagedScreen(Screens[S]);
                DisplayScreens.Add(ManagedScreen);
                ComboActiveScreen.Items.Add(ManagedScreen.ScreenName);
                if (ManagedScreen.Primary) {
                    PrimaryIndex = S;
                }
            }
        }

        public void UpdateResolution(Resolution NewResolution) {
            ActiveResolution = NewResolution;
            SafeResolution = new Resolution(
                ActiveResolution.Left + DetectionRange,
                ActiveResolution.Right - DetectionRange,
                ActiveResolution.Bottom + DetectionRange,
                ActiveResolution.Top - DetectionRange);
        }

        public void UpdateScreen(int RequestedScreen) {
            if (RequestedScreen >= 0 && RequestedScreen < DisplayScreens.Count) {
                UpdateResolution(DisplayScreens[RequestedScreen].Resolution);
                Debug.WriteLine($"Changed Screen. New Resolution: {ActiveResolution}\n\tSafe Resolution: {SafeResolution}");
            } else {
                Debug.WriteLine($"Screen @ '{RequestedScreen}' doesn't exist");
                //If selected screen doesn't exist in list anymore, regenerate list.
                UpdateScreens(out RequestedScreen);
                ComboActiveScreen.SelectedIndex = RequestedScreen;
            }
        }

        #endregion

        #region Main Loop

        public void ToggleLoop() {
            LoopActive = !LoopActive;
            TrayIcon.Icon = LoopActive ? Properties.Resources.IconActive : Properties.Resources.Icon;
            if (LoopActive) {
                Task.Run(MainLoop);
            }

            Dispatcher.Invoke(() => {
                StateToggle.IsChecked = LoopActive;
                StateToggle.Content = LoopActive ? "<<Active>>" : $"<<Inactive [{DirectHotkey.MonitoredKey}]>>";
                Debug.WriteLine($"Toggled; {StateToggle.Content}");
            }, System.Windows.Threading.DispatcherPriority.Normal);
        }

        const int DetectionRange = 5;
        const int BounceFactor = 15;
        const float Sensitivity = 0.01f;

        public void MainLoop() {
            Debug.WriteLine("Entered Loop.");
            while (LoopActive) {
                Int32Point MousePoint = User32.Cursor;

                //Debug.WriteLine($"AR: {ActiveResolution}\tMouse: {MousePoint}");
                if (!SafeResolution.Contains(MousePoint)) {
                    Debug.WriteLine($"{MousePoint} exceeds {SafeResolution}");
                    Debug.WriteLine($"{MousePoint} => {ActiveResolution.Clamp(MousePoint)}");
                    Int32Point NewPoint;
                    if (BoundsClamp) {
                        Debug.WriteLine("\tBouncing...");
                        NewPoint = Bounce(MousePoint, BounceFactor, Sensitivity);
                    } else {
                        Debug.WriteLine("\tMirroring...");
                        NewPoint = Flip(MousePoint, BounceFactor, Sensitivity);
                    }

                    User32.Cursor = NewPoint;
                }
            }
            Debug.WriteLine("Exited Loop.");
        }

        public Int32Point Bounce(Int32Point Point, int Factor, float Sensitivity) {
            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (ActiveResolution.DetermineHorizontalSide(Point, Sensitivity)) {
                case ScreenSide.Left:
                    Point.X = ActiveResolution.Left + Factor;
                    Debug.WriteLine("\t\t@Left; Bounce Right");
                    break;
                case ScreenSide.Right:
                    Point.X = ActiveResolution.Right - Factor;
                    Debug.WriteLine("\t\t@Right; Bounce Left");
                    break;
            }

            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (ActiveResolution.DetermineVerticalSide(Point, Sensitivity)) {
                case ScreenSide.Bottom:
                    Point.Y = ActiveResolution.Bottom + Factor;
                    Debug.WriteLine("\t\t@Bottom; Bounce Up");
                    break;
                case ScreenSide.Top:
                    Point.Y = ActiveResolution.Top - Factor;
                    Debug.WriteLine("\t\t@Top; Bounce Down");
                    break;
            }

            return Point;
        }

        public Int32Point Flip(Int32Point Point, int Factor, float Sensitivity) {
            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (ActiveResolution.DetermineHorizontalSide(Point, Sensitivity)) {
                case ScreenSide.Left:
                    Point.X = ActiveResolution.Right - Factor;
                    Debug.WriteLine("\t\t@Left; Move Right");
                    break;
                case ScreenSide.Right:
                    Point.X = ActiveResolution.Left + Factor;
                    Debug.WriteLine("\t\t@Right; Move Left");
                    break;
            }

            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (ActiveResolution.DetermineVerticalSide(Point, Sensitivity)) {
                case ScreenSide.Bottom:
                    Point.Y = ActiveResolution.Top - Factor;
                    Debug.WriteLine("\t\t@Bottom; Move Top");
                    break;
                case ScreenSide.Top:
                    Point.Y = ActiveResolution.Bottom + Factor;
                    Debug.WriteLine("\t\t@Top; Move Bottom");
                    break;
            }

            return Point;
        }

        #endregion

        #region XAML Handlers

        void CheckClamp_Click(object Sender, RoutedEventArgs E) => BoundsClamp = CheckClamp.IsChecked ?? false;
        
        void SelectorHotKey_KeyUp(object Sender, KeyEventArgs E) {
            Debug.WriteLine("New HotKey: " + SelectorHotKey.HotKey);
            ChangeHotkey(SelectorHotKey.HotKey.Key);
        }

        void MetroWindow_Closed(object Sender, EventArgs E) => SafelyClose();

        void MetroWindow_Closing(object Sender, System.ComponentModel.CancelEventArgs E) => SafelyClose();

        void StateToggle_Click(object Sender, RoutedEventArgs E) => ToggleLoop();
        
        void ComboActiveScreen_SelectionChanged(object Sender, System.Windows.Controls.SelectionChangedEventArgs E) => UpdateScreen(ComboActiveScreen.SelectedIndex);

        void CheckAoT_Click(object Sender, RoutedEventArgs E) => AlwaysOnTop = CheckAoT.IsChecked ?? false;

        #endregion
    }
}
