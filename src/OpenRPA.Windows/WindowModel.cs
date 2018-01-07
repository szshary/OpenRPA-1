﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Windows
{
    public class WindowModel
    {
        private IntPtr hWnd;

        public static WindowModel FindByPosition(int x, int y)
        {
            IntPtr hWnd = Win32.WindowFromPoint(new Point(x, y));
            if (hWnd == IntPtr.Zero)
            {
                throw new Exception($"Window at ({x}, {y}) not found");
            }
            return new WindowModel(hWnd);
        }

        private WindowModel(IntPtr hWnd)
        {
            this.hWnd = hWnd;
        }

        internal void TryBringToForeground()
        {
            // Reference: https://dobon.net/vb/dotnet/process/appactivate.html

            // Show window if minimized
            if (Win32.IsIconic(this.hWnd))
            {
                Win32.ShowWindowAsync(this.hWnd, Win32.SW_RESTORE);
            }

            IntPtr hWnd = Win32.GetForegroundWindow();
            if (hWnd == this.hWnd)
            {
                return;
            }

            uint foreThread = Win32.GetWindowThreadProcessId(hWnd, IntPtr.Zero);
            uint thisThread = Win32.GetCurrentThreadId();
            uint timeout = 200000;
            if (foreThread != thisThread)
            {
                // Remember current value
                Win32.SystemParametersInfoGet(Win32.SPI_GETFOREGROUNDLOCKTIMEOUT, 0, ref timeout, 0);

                // Set ForegroundLockTimeout to 0
                Win32.SystemParametersInfoSet(Win32.SPI_SETFOREGROUNDLOCKTIMEOUT, 0, 0, 0);
            }

            // Try bring window to foreground
            Win32.SetForegroundWindow(this.hWnd);
            Win32.SetWindowPos(this.hWnd, Win32.HWND_TOP, 0, 0, 0, 0,
                Win32.SWP_NOMOVE | Win32.SWP_NOSIZE | Win32.SWP_SHOWWINDOW | Win32.SWP_ASYNCWINDOWPOS);
            Win32.BringWindowToTop(this.hWnd);
            Win32.ShowWindowAsync(this.hWnd, Win32.SW_SHOW);
            Win32.SetFocus(this.hWnd);

            // Restore ForegroundLockTimeout
            if (foreThread != thisThread)
            {
                Win32.SystemParametersInfoSet(Win32.SPI_SETFOREGROUNDLOCKTIMEOUT, 0, timeout, 0);
            }
        }

        public Rectangle GetRectangle()
        {
            Win32.RECT r;
            if (!Win32.GetWindowRect(this.hWnd, out r))
            {
                throw new Exception("Failed to get window rect");
            }
            return new Rectangle(r.left, r.top, r.right - r.left, r.bottom - r.top);
        }

        public Bitmap CaptureWindow()
        {
            TryBringToForeground();

            System.Threading.Thread.Sleep(500);

            Rectangle rect = GetRectangle();

            var bmp = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(rect.X, rect.Y, 0, 0, rect.Size, CopyPixelOperation.SourceCopy);
            }

            return bmp;
        }
    }
}