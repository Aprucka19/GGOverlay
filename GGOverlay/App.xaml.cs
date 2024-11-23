using System;
using System.Configuration;
using System.Data;
using System.Threading;
using System.Windows;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace GGOverlay
{
    public partial class App : Application
    {
#if !DEBUG
        private static Mutex mutex = null;
        private static bool ownsMutex = false;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        private const int SW_RESTORE = 9;
#endif

        protected override void OnStartup(StartupEventArgs e)
        {
#if !DEBUG
            const string mutexName = "GGOverlay_Mutex";
            bool createdNew;

            // Attempt to create a global mutex
            mutex = new Mutex(true, mutexName, out createdNew);
            ownsMutex = createdNew;

            if (!createdNew)
            {
                // Another instance is already running
                BringExistingInstanceToForeground();
                Application.Current.Shutdown();
                return;
            }
#endif
            base.OnStartup(e);
            // Additional startup logic if needed
        }

        protected override void OnExit(ExitEventArgs e)
        {
#if !DEBUG
            // Release the mutex when the application exits if this instance owns it
            if (ownsMutex && mutex != null)
            {
                mutex.ReleaseMutex();
                mutex = null;
            }
#endif
            base.OnExit(e);
            // Additional cleanup logic if needed
        }

#if !DEBUG
        private void BringExistingInstanceToForeground()
        {
            // Get the current process
            Process currentProcess = Process.GetCurrentProcess();

            // Get all processes with the same name as the current process
            Process[] processes = Process.GetProcessesByName(currentProcess.ProcessName);

            foreach (Process process in processes)
            {
                if (process.Id != currentProcess.Id)
                {
                    IntPtr hWnd = process.MainWindowHandle;

                    if (hWnd != IntPtr.Zero)
                    {
                        // If the window is minimized, restore it
                        if (IsIconic(hWnd))
                        {
                            ShowWindowAsync(hWnd, SW_RESTORE);
                        }

                        // Bring the window to the foreground
                        SetForegroundWindow(hWnd);
                    }
                    break;
                }
            }
        }
#endif
    }
}
