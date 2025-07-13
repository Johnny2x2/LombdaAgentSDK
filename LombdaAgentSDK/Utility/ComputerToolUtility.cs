using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
namespace LombdaAgentSDK
{
    public class ComputerToolUtility
    {
        public static System.Drawing.Size GetScreenSize()
        {
            int width = GetSystemMetrics(SM_CXSCREEN);
            int height = GetSystemMetrics(SM_CYSCREEN);
            return new System.Drawing.Size(width, height);
        }
        // -----------------------
        // Cursor & Mouse Control
        // -----------------------
        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        private const int SM_CXSCREEN = 0;
        private const int SM_CYSCREEN = 1;

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        private const uint MOUSEEVENTF_MOVE = 0x0001;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_WHEEL = 0x0800;

        public static void SetCursorPosition(int x, int y)
        {
            SetCursorPos(x, y);
        }

        public static POINT GetCursorPosition()
        {
            GetCursorPos(out POINT point);
            return point;
        }

        public static void MoveCursorSmooth(int toX, int toY, int steps = 50)
        {
            var start = GetCursorPosition();
            for (int i = 1; i <= steps; i++)
            {
                int x = start.X + (toX - start.X) * i / steps;
                int y = start.Y + (toY - start.Y) * i / steps;
                SetCursorPos(x, y);
                Thread.Sleep(5);
            }
        }

        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;

        public static void RightClick()
        {
            mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, UIntPtr.Zero);
            mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, UIntPtr.Zero);
        }

        public static void MiddleClick()
        {
            mouse_event(MOUSEEVENTF_MIDDLEDOWN, 0, 0, 0, UIntPtr.Zero);
            mouse_event(MOUSEEVENTF_MIDDLEUP, 0, 0, 0, UIntPtr.Zero);
        }
        public static void Click()
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
        }

        public static void DoubleClick()
        {
            Click();
            Thread.Sleep(100);
            Click();
        }

        public static void MoveAndDoubleClick(int toX, int toY)
        {
            MoveCursorSmooth(toX, toY);
            Click();
            Thread.Sleep(100);
            Click();
        }

        public static void MoveAndClick(int toX, int toY)
        {
            MoveCursorSmooth(toX, toY);
            Click();
            Thread.Sleep(100);
        }

        public static void MoveAndRightClick(int toX, int toY)
        {
            MoveCursorSmooth(toX, toY);
            RightClick();
            Thread.Sleep(100);
        }

        public static void Scroll(int amount)
        {
            mouse_event(MOUSEEVENTF_WHEEL, 0, 0, (uint)amount, UIntPtr.Zero);
        }

        public static void Drag(int toX, int toY)
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
            MoveCursorSmooth(toX, toY);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
        }

        // -------------------
        // Keyboard Control
        // -------------------
        [DllImport("user32.dll", SetLastError = true)]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private const int KEYEVENTF_KEYDOWN = 0x0000;
        private const int KEYEVENTF_KEYUP = 0x0002;

        [DllImport("user32.dll")]
        private static extern short VkKeyScan(char ch);

        public static void PressKey(byte keyCode)
        {
            keybd_event(keyCode, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
            Thread.Sleep(50);
            keybd_event(keyCode, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        public static void PressKeyDown(string keyCode)
        {
            short vkCode = VkKeyScan(keyCode[0]); // Get virtual key code for the character
            byte keyCod = (byte)(vkCode & 0xFF); // Extract the virtual key code (low-order byte)
            keybd_event(keyCod, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
            Thread.Sleep(50);
            keybd_event(keyCod, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        public static void Type(List<string> texts)
        {
            foreach (string text in texts)
            {
                foreach (char ch in text)
                {
                    short vkCode = VkKeyScan(ch); // Get virtual key code for the character
                    byte keyCode = (byte)(vkCode & 0xFF); // Extract the virtual key code (low-order byte)

                    // Check if a shift state is required (high-order byte)
                    bool needsShift = ((vkCode >> 8) & 1) != 0;

                    if (needsShift)
                    {
                        PressKey((byte)0xA0); // Simulate Left Shift press
                    }

                    PressKey(keyCode);

                    if (needsShift)
                    {
                        PressKey((byte)0xA0); // Simulate Left Shift release
                    }
                    Thread.Sleep(50);
                }
            }
        }

        public static void Type(string text)
        {
            foreach (char ch in text)
            {
                short vkCode = VkKeyScan(ch); // Get virtual key code for the character
                byte keyCode = (byte)(vkCode & 0xFF); // Extract the virtual key code (low-order byte)

                // Check if a shift state is required (high-order byte)
                bool needsShift = ((vkCode >> 8) & 1) != 0;

                if (needsShift)
                {
                    PressKey((byte)0xA0); // Simulate Left Shift press
                }

                PressKey(keyCode);

                if (needsShift)
                {
                    PressKey((byte)0xA0); // Simulate Left Shift release
                }
                Thread.Sleep(50);
            }
        }

        // -------------------
        // Screenshot using GDI
        // -------------------
        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight,
                                          IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        private const int SRCCOPY = 0x00CC0020;

        public static Bitmap TakeScreenshot()
        {
            int width = GetSystemMetrics(SM_CXSCREEN);
            int height = GetSystemMetrics(SM_CYSCREEN);

            Bitmap bmp = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                IntPtr hdcDest = g.GetHdc();
                IntPtr hdcSrc = GetWindowDC(GetDesktopWindow());
                BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, SRCCOPY);
                g.ReleaseHdc(hdcDest);
            }
            return bmp;
        }

        public static byte[] TakeScreenshotByteArray(ImageFormat format = null)
        {
            int width = GetSystemMetrics(SM_CXSCREEN);
            int height = GetSystemMetrics(SM_CYSCREEN);

            format ??= ImageFormat.Png; // default to PNG

            using (var ms = new MemoryStream())
            {
                Bitmap bmp = new Bitmap(width, height);

                using (Graphics g = Graphics.FromImage(bmp))
                {
                    IntPtr hdcDest = g.GetHdc();
                    IntPtr hdcSrc = GetWindowDC(GetDesktopWindow());
                    BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, SRCCOPY);
                    g.ReleaseHdc(hdcDest);
                }

                bmp.Save("screenshot.png");

                bmp.Save(ms, format);

                bmp.Dispose();

                return ms.ToArray();
            }
        }
    }
}
