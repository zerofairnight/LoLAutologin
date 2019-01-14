using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace LOLAutologin
{
    public class LolAutoLogger : DispatcherObject
    {
        private Task loginTask;

        private CancellationTokenSource _CancellationTokenSource;

        public void Login(string user, string pass, string lolLauncherPath)
        {
            base.CheckAccess();
            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass) || string.IsNullOrEmpty(lolLauncherPath))
            {
                throw new ArgumentException();
            }
            this.SuspendOldTask();
            this.loginTask = Task.Factory.StartNew(delegate
            {
                LolAutoLogger.LoginAsync(user, pass, lolLauncherPath);
            });
        }

        private void SuspendOldTask()
        {
            if (this.loginTask != null)
            {
                this._CancellationTokenSource.Cancel();
                this.loginTask.Wait();
                this.loginTask = null;
            }
        }

        private static void LoginAsync(string user, string pass, string lolLauncherPath)
        {
            LolAutoLogger.OpenTheGame(lolLauncherPath);
            LolAutoLogger.AutoClickPatcher();
            LolAutoLogger.AutoClickClient(user, pass);
        }

        private static void OpenTheGame(string launcherPath)
        {
            if (Process.GetProcessesByName("LoLLauncher").Length == 0)
            {
                Mutex mutex;
                bool flag = Mutex.TryOpenExisting("RADS_USER_KERNEL_MUTEX_NAME", out mutex);
                if (flag)
                {
                    try
                    {
                        mutex.WaitOne();
                    }
                    catch (AbandonedMutexException)
                    {
                    }
                }
                Process.Start(new ProcessStartInfo
                {
                    FileName = launcherPath,
                    WorkingDirectory = Path.GetDirectoryName(launcherPath)
                }).WaitForExit();
                while (!Mutex.TryOpenExisting("RADS_USER_KERNEL_MUTEX_NAME", out mutex))
                {
                    Thread.Sleep(100);
                }
                ProgramThread.WaitForProcessWindow("LolPatcherUx");
            }
        }

        private static void AutoClickPatcher()
        {
            Process process = Process.GetProcessesByName("LolPatcherUx").FirstOrDefault((Process p) => p.MainWindowHandle != IntPtr.Zero && p.MainWindowTitle == "LoL Patcher");
            while (!process.HasExited && process.MainWindowHandle != IntPtr.Zero)
            {
                Color pixelColor = ProgramThread.GetPixelColor(process, 645, 35);
                if (pixelColor.R == 218 && pixelColor.G == 170 && pixelColor.B == 124)
                {
                    break;
                }
                Thread.Sleep(100);
            }
            ProgramThread.SetForegroundWindow(process.MainWindowHandle);
            ProgramThread.ForceForegroundWindow(process.MainWindowHandle);
            ProgramThread.BringWindowToTop(process.MainWindowHandle);
            ProgramThread.ShowWindow(process.MainWindowHandle, ProgramThread.ShowWindowCommands.Show);
            ProgramThread.RECT windowRect = ProgramThread.GetWindowRect(process.MainWindowHandle);
            ProgramThread.MousePoint mousePoint;
            ProgramThread.GetCursorPos(out mousePoint);
            ProgramThread.SetCursorPos(windowRect.Left + 645, windowRect.Top + 35);
            ProgramThread.mouse_event(6u, 645u, 35u, 0u, 0u);
            ProgramThread.SetCursorPos(mousePoint.X, mousePoint.Y);
        }

        private static void AutoClickClient(string user, string pass)
        {
            Process process;
            if (ProgramThread.WaitForProcessWindow("LolClient", out process))
            {
                while (ProgramThread.IsWindowVisible(process.MainWindowHandle))
                {
                    if (process.HasExited)
                    {
                        return;
                    }
                    if (LolAutoLogger.PixelColorIsWite(process, 195, 320) && LolAutoLogger.PixelColorIsWite(process, 195, 380))
                    {
                        break;
                    }
                    Thread.Sleep(100);
                }
                ProgramThread.SimulateClick(process, 195, 320);
                ProgramThread.SimulateClick(process, 195, 320);
                ProgramThread.SimulateText(process, user);
                ProgramThread.SimulateClick(process, 195, 380);
                ProgramThread.SimulateClick(process, 195, 380);
                ProgramThread.SimulateText(process, pass);
                ProgramThread.SimulateClick(process, 375, 420);
            }
        }

        private static int MakeLParam(int LoWord, int HiWord)
        {
            return HiWord << 16 | (LoWord & 65535);
        }

        private static bool PixelColorIsWite(Process process, int x, int y)
        {
            IntPtr mainWindowHandle = process.MainWindowHandle;
            bool result;
            if (mainWindowHandle == IntPtr.Zero || process.HasExited)
            {
                result = false;
            }
            else
            {
                IntPtr dC = ProgramThread.GetDC(mainWindowHandle);
                uint pixel = ProgramThread.GetPixel(dC, x, y);
                ProgramThread.ReleaseDC(mainWindowHandle, dC);
                Color color = Color.FromArgb((int)(pixel & 255u), (int)(pixel & 65280u) >> 8, (int)(pixel & 16711680u) >> 16);
                result = (color.R > 240 && color.G > 240 && color.B > 240);
            }
            return result;
        }
    }

    public static class ProgramThread
    {
        public struct MousePoint
        {
            public int X;

            public int Y;

            public MousePoint(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }
        }

        public struct RECT
        {
            public int Left;

            public int Top;

            public int Right;

            public int Bottom;

            public int Width
            {
                get
                {
                    return this.Right - this.Left;
                }
            }

            public int Height
            {
                get
                {
                    return this.Bottom - this.Top;
                }
            }

            public bool Contains(int x, int y)
            {
                return this.Left <= x && x < this.Left + this.Width && this.Top <= y && y < this.Top + this.Height;
            }

            public bool Contains(Point position)
            {
                return this.Contains(position.X, position.Y);
            }

            public Rectangle ToRectangle()
            {
                return new Rectangle(this.Left, this.Top, this.Right - this.Left, this.Bottom - this.Top);
            }
        }

        private struct WINDOWPLACEMENT
        {
            public int length;

            public int flags;

            public ProgramThread.ShowWindowCommands showCmd;

            public Point ptMinPosition;

            public Point ptMaxPosition;

            public Rectangle rcNormalPosition;
        }

        public enum ShowWindowCommands
        {
            Hide,
            Normal,
            ShowMinimized,
            Maximize,
            ShowMaximized = 3,
            ShowNoActivate,
            Show,
            Minimize,
            ShowMinNoActive,
            ShowNA,
            Restore,
            ShowDefault,
            ForceMinimize
        }

        public const int MOUSEEVENTF_LEFTDOWN = 2;

        public const int MOUSEEVENTF_LEFTUP = 4;

        public const int MOUSEEVENTF_RIGHTDOWN = 8;

        public const int MOUSEEVENTF_RIGHTUP = 16;

        public static ushort WM_KEYDOWN = 256;

        public static ushort WM_KEYUP = 257;

        public static ProgramThread.RECT GetWindowRect(IntPtr handle)
        {
            ProgramThread.RECT result;
            ProgramThread.GetWindowRect(handle, out result);
            return result;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetCursorPos(out ProgramThread.MousePoint lpMousePoint);

        [DllImport("user32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SendMessage(IntPtr hwnd, int wMsg, int wParam, uint lParam);

        public static bool WaitForProcess(string processName)
        {
            Process process;
            return ProgramThread.WaitForProcess(processName, out process);
        }

        public static bool WaitForProcess(string processName, out Process process)
        {
            return ProgramThread.WaitForProcess(processName, -1, out process);
        }

        public static bool WaitForProcess(string processName, int millisecondsTimeout, out Process process)
        {
            return ProgramThread.WaitForProcess(processName, TimeSpan.FromMilliseconds((double)millisecondsTimeout), out process);
        }

        public static bool WaitForProcess(string processName, TimeSpan timeout, out Process process)
        {
            return ProgramThread.WaitForProcess(processName, timeout, default(CancellationToken), out process);
        }

        public static bool WaitForProcess(string processName, TimeSpan timeout, CancellationToken cancellationToken, out Process process)
        {
            process = null;
            bool flag = timeout < TimeSpan.Zero;
            Stopwatch stopwatch = new Stopwatch();
            if (!flag)
            {
                stopwatch.Start();
            }
            Process[] processesByName;
            bool result;
            while ((processesByName = Process.GetProcessesByName(processName)).Length == 0)
            {
                Thread.Sleep(100);
                if ((!flag && stopwatch.Elapsed > timeout) || cancellationToken.IsCancellationRequested)
                {
                    result = false;
                    return result;
                }
            }
            if (processesByName[0].HasExited)
            {
                result = false;
                return result;
            }
            process = processesByName[0];
            result = true;
            return result;
        }

        public static bool WaitForProcessWindow(string processName)
        {
            Process process;
            return ProgramThread.WaitForProcessWindow(processName, out process);
        }

        public static bool WaitForProcessWindow(string processName, out Process process)
        {
            return ProgramThread.WaitForProcessWindow(processName, -1, out process);
        }

        public static bool WaitForProcessWindow(string processName, int millisecondsTimeout, out Process process)
        {
            return ProgramThread.WaitForProcessWindow(processName, TimeSpan.FromMilliseconds((double)millisecondsTimeout), out process);
        }

        public static bool WaitForProcessWindow(string processName, TimeSpan timeout, out Process process)
        {
            return ProgramThread.WaitForProcessWindow(processName, timeout, default(CancellationToken), out process);
        }

        public static bool WaitForProcessWindow(string processName, TimeSpan timeout, CancellationToken cancellationToken, out Process process)
        {
            bool result;
            if (!ProgramThread.WaitForProcess(processName, out process))
            {
                result = false;
            }
            else
            {
                bool flag = timeout < TimeSpan.Zero;
                Stopwatch stopwatch = new Stopwatch();
                if (!flag)
                {
                    stopwatch.Start();
                }
                while (process.MainWindowHandle == IntPtr.Zero)
                {
                    if (process.HasExited)
                    {
                        process = null;
                        result = false;
                        return result;
                    }
                    Thread.Sleep(100);
                    if ((!flag && stopwatch.Elapsed > timeout) || cancellationToken.IsCancellationRequested)
                    {
                        process = null;
                        result = false;
                        return result;
                    }
                }
                while (!ProgramThread.IsWindowVisible(process.MainWindowHandle))
                {
                    if (process.HasExited)
                    {
                        process = null;
                        result = false;
                        return result;
                    }
                    Thread.Sleep(100);
                    if ((!flag && stopwatch.Elapsed > timeout) || cancellationToken.IsCancellationRequested)
                    {
                        process = null;
                        result = false;
                        return result;
                    }
                }
                result = true;
            }
            return result;
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("gdi32.dll")]
        public static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect([In] IntPtr hWnd, out ProgramThread.RECT lpRect);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PostMessage(IntPtr hWnd, uint msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShowWindow(IntPtr hWnd, ProgramThread.ShowWindowCommands nCmdShow);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowPlacement(IntPtr hWnd, ref ProgramThread.WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool BringWindowToTop(HandleRef hWnd);

        [DllImport("user32.dll")]
        public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        public static void ForceForegroundWindow(IntPtr hWnd)
        {
            uint windowThreadProcessId = ProgramThread.GetWindowThreadProcessId(ProgramThread.GetForegroundWindow(), IntPtr.Zero);
            uint currentThreadId = ProgramThread.GetCurrentThreadId();
            if (windowThreadProcessId != currentThreadId)
            {
                ProgramThread.AttachThreadInput(windowThreadProcessId, currentThreadId, true);
                ProgramThread.BringWindowToTop(hWnd);
                ProgramThread.ShowWindow(hWnd, ProgramThread.ShowWindowCommands.Show);
                ProgramThread.AttachThreadInput(windowThreadProcessId, currentThreadId, false);
            }
            else
            {
                ProgramThread.BringWindowToTop(hWnd);
                ProgramThread.ShowWindow(hWnd, ProgramThread.ShowWindowCommands.Show);
            }
        }

        public static bool SimulateClick(Process process, int x, int y)
        {
            IntPtr mainWindowHandle = process.MainWindowHandle;
            bool result;
            if (mainWindowHandle == IntPtr.Zero || process.HasExited)
            {
                result = false;
            }
            else
            {
                bool flag = ProgramThread.PostMessage(mainWindowHandle, 513u, 1, y << 16 | (x & 65535));
                Thread.Sleep(120);
                bool flag2 = ProgramThread.PostMessage(mainWindowHandle, 514u, 1, y << 16 | (x & 65535));
                result = (flag && flag2);
            }
            return result;
        }

        public static bool SimulateText(Process process, string value)
        {
            IntPtr mainWindowHandle = process.MainWindowHandle;
            bool result;
            if (mainWindowHandle == IntPtr.Zero || process.HasExited)
            {
                result = false;
            }
            else
            {
                char[] array = value.ToCharArray();
                for (int i = 0; i < array.Length; i++)
                {
                    char wParam = array[i];
                    if (!ProgramThread.PostMessage(mainWindowHandle, 258u, (int)wParam, 0))
                    {
                        result = false;
                        return result;
                    }
                }
                result = true;
            }
            return result;
        }

        public static bool WindowHasMouseOver(Process process)
        {
            IntPtr mainWindowHandle = process.MainWindowHandle;
            bool result;
            if (mainWindowHandle == IntPtr.Zero || process.HasExited)
            {
                result = false;
            }
            else if (ProgramThread.GetForegroundWindow() != mainWindowHandle)
            {
                result = false;
            }
            else
            {
                ProgramThread.WINDOWPLACEMENT wINDOWPLACEMENT = default(ProgramThread.WINDOWPLACEMENT);
                if (!ProgramThread.GetWindowPlacement(mainWindowHandle, ref wINDOWPLACEMENT))
                {
                    result = false;
                }
                else if (wINDOWPLACEMENT.showCmd != ProgramThread.ShowWindowCommands.Normal)
                {
                    result = false;
                }
                else
                {
                    ProgramThread.RECT rECT = default(ProgramThread.RECT);
                    if (!ProgramThread.GetWindowRect(mainWindowHandle, out rECT))
                    {
                        result = false;
                    }
                    else
                    {
                        ProgramThread.MousePoint mousePoint;
                        ProgramThread.GetCursorPos(out mousePoint);
                        Point position = new Point(mousePoint.X, mousePoint.Y);
                        result = rECT.Contains(position);
                    }
                }
            }
            return result;
        }

        public static Color GetPixelColor(Process process, int x, int y)
        {
            IntPtr mainWindowHandle = process.MainWindowHandle;
            Color result;
            if (mainWindowHandle == IntPtr.Zero || process.HasExited)
            {
                result = Color.Empty;
            }
            else
            {
                IntPtr dC = ProgramThread.GetDC(mainWindowHandle);
                uint pixel = ProgramThread.GetPixel(dC, x, y);
                ProgramThread.ReleaseDC(mainWindowHandle, dC);
                result = Color.FromArgb((int)(pixel & 255u), (int)(pixel & 65280u) >> 8, (int)(pixel & 16711680u) >> 16);
            }
            return result;
        }

        public static bool PixelColorIsWite(Process process, int x, int y)
        {
            IntPtr mainWindowHandle = process.MainWindowHandle;
            bool result;
            if (mainWindowHandle == IntPtr.Zero || process.HasExited)
            {
                result = false;
            }
            else
            {
                IntPtr dC = ProgramThread.GetDC(mainWindowHandle);
                uint pixel = ProgramThread.GetPixel(dC, x, y);
                ProgramThread.ReleaseDC(mainWindowHandle, dC);
                Color color = Color.FromArgb((int)(pixel & 255u), (int)(pixel & 65280u) >> 8, (int)(pixel & 16711680u) >> 16);
                result = (color.R > 240 && color.G > 240 && color.B > 240);
            }
            return result;
        }
    }

}
