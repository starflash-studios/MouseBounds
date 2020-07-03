using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SharpDX.DirectInput;

namespace MouseBounds {
    public class DirectHotkey {
        public DirectInput DirectInput;
        public KeyboardState KeyboardState;

        public Key MonitoredKey;

        internal Keyboard MonitoredKeyboard;
        const int PollBufferSize = 128;

        public delegate void DlgOnKeyPolled();
        public DlgOnKeyPolled OnKeyPolled;
        
        public DirectHotkey() {
            DirectInput = new DirectInput();
            KeyboardState = new KeyboardState();

            MonitoredKeyboard = new Keyboard(DirectInput);
            MonitoredKeyboard.Properties.BufferSize = PollBufferSize;
            MonitoredKeyboard.Acquire();

        }

        public static Key FromSystemKey(System.Windows.Input.Key SystemKey) {
            if (Enum.TryParse(SystemKey.ToString(), out Key DirectKey)) {
                return DirectKey;
            }

            Debug.WriteLine($"Warning; SystemKey '{SystemKey}' does not have a direct {nameof(Key)} conversion.");
            return(Key)SystemKey;
        }

        //public void Poll(int BufferSize = 128) => Poll(MonitoredKeyboard, OnKeyPolled, BufferSize, MonitoredKey);

        //public static void Poll(Keyboard Keyboard, DlgOnKeyPolled OnKeyPolled, int BufferSize = 128, Key RequestedKey = Key.F6) {
        //    Keyboard.Properties.BufferSize = BufferSize;
        //    while (true) {
        //        Keyboard.Poll();
        //        KeyboardUpdate[] Data = Keyboard.GetBufferedData();
        //        // ReSharper disable once LoopCanBePartlyConvertedToQuery
        //        foreach (KeyboardUpdate State in Data) {
        //            if (State.Key == RequestedKey && State.IsPressed) {
        //                OnKeyPolled?.Invoke();
        //            }
        //        }
        //    }
        //    // ReSharper disable once FunctionNeverReturns
        //}

        public KeyboardState GetKeyboardState() => GetKeyboardState(MonitoredKeyboard);

        public KeyboardState GetKeyboardState(Keyboard Keyboard) {
            Keyboard.GetCurrentState(ref KeyboardState);
            return KeyboardState;
        }

        public static Guid AcquireGuid(DirectInput DirectInput, DeviceType RequestedType = DeviceType.Gamepad) {
            foreach (DeviceInstance DeviceInstance in DirectInput.GetDevices(RequestedType, DeviceEnumerationFlags.AllDevices).Where(DeviceInstance => DeviceInstance?.InstanceGuid != null)) {
                return DeviceInstance.InstanceGuid;
            }

            return Guid.Empty;
        }

        #region Threaded Processes

        #region Async Poll-Based

        public async Task WaitForPolledKeypressAsync(int PollRate = 30) {
            while (true) {
                await Task.Delay(PollRate).ConfigureAwait(false);
                MonitoredKeyboard.Poll();
                KeyboardUpdate[] Data = MonitoredKeyboard.GetBufferedData();
                // ReSharper disable once LoopCanBeConvertedToQuery
                foreach (KeyboardUpdate State in Data) {
                    if (State.Key == MonitoredKey && State.IsPressed) {
                        return;
                    }
                }
            }
        }

        internal async Task PollThreadAsync(int PollRate = 30, DlgOnKeyPolled OnKeyPolled = null) {
            while (true) {
                await WaitForPolledKeypressAsync(PollRate).ConfigureAwait(false);
                OnKeyPolled?.Invoke();
            }
            // ReSharper disable once FunctionNeverReturns
        }

        // ReSharper disable once AsyncConverter.AsyncAwaitMayBeElidedHighlighting
        public Thread GetAsyncPollThread(int PollRate = 30, DlgOnKeyPolled OnKeyPolled = null) => new Thread(async () => await PollThreadAsync(PollRate, OnKeyPolled).ConfigureAwait(false));

        #endregion

        #region Async State-Based

        public async Task<(List<Key>, bool)> WaitForStateKeypressAsync(List<Key> PreviousPressedKeys, int PollRate = 30) {
            await Task.Delay(PollRate).ConfigureAwait(false);
            KeyboardState State = GetKeyboardState();
            List<Key> CurrentPressedKeys = State.PressedKeys;
            if (CurrentPressedKeys.Contains(MonitoredKey)) {
                Debug.WriteLine("Contained!");
                if (!(PreviousPressedKeys?.SequenceEqual(CurrentPressedKeys) ?? CurrentPressedKeys == null)) {
                    //Ensures value is only returned if sequence is different from last attempt (only called once per keypress of monitored key)
                    //PressedKeys contains keys that are currently down, held and up; we only want down.
                    return(CurrentPressedKeys, true);
                }
            }
            return (null, false);
        }

        internal async Task StateThreadAsync(int PollRate = 30, DlgOnKeyPolled OnKeyPolled = null) {
            List<Key> PressedKeys = new List<Key>();
            while (true) {
                bool Success;
                (PressedKeys, Success) = await WaitForStateKeypressAsync(PressedKeys, PollRate).ConfigureAwait(false);
                if (Success) {
                    OnKeyPolled?.Invoke();
                }
            }
            // ReSharper disable once FunctionNeverReturns
        }

        // ReSharper disable once AsyncConverter.AsyncAwaitMayBeElidedHighlighting
        public Thread GetAsyncStateThread(int PollRate = 30, DlgOnKeyPolled OnKeyPolled = null) => new Thread(async () => await StateThreadAsync(PollRate, OnKeyPolled).ConfigureAwait(false));

        #endregion

        #region Manual

        public Thread GetManuallyPolledThread(int PollRate = 30, DlgOnKeyPolled OnKeyPolled = null) => new Thread(() => ManuallyPolledThread(PollRate, OnKeyPolled));

        internal void ManuallyPolledThread(int PollRate = 30, DlgOnKeyPolled OnKeyPolled = null) {
            while (true) {
                Thread.Sleep(PollRate);
                KeyboardState State = GetKeyboardState();
                if (State.PressedKeys.Contains(MonitoredKey)) {
                    OnKeyPolled?.Invoke();
                }
            }
            // ReSharper disable once FunctionNeverReturns
        }

        #endregion

        #endregion

    }
}