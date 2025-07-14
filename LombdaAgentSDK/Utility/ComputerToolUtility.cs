using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
namespace LombdaAgentSDK
{
    public class ComputerToolUtility
    {
        /// <summary>
        /// Get screen size on windows pc
        /// </summary>
        /// <returns></returns>
        public static System.Drawing.Size GetScreenSize()
        {
            int width = GetSystemMetrics(SM_CXSCREEN);
            int height = GetSystemMetrics(SM_CYSCREEN);
            return new System.Drawing.Size(width, height);
        }
        // -----------------------
        // Cursor & Mouse Control
        // -----------------------
        /// <summary>
        /// Set cursor position
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        /// <summary>
        /// Get current cursor position
        /// </summary>
        /// <param name="lpPoint"></param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        /// <summary>
        /// Trigger mouse click event
        /// </summary>
        /// <param name="dwFlags"></param>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        /// <param name="dwData"></param>
        /// <param name="dwExtraInfo"></param>
        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        /// <summary>
        /// Get screen size
        /// </summary>
        /// <param name="nIndex"></param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        /// <summary>
        /// Width of screen in px
        /// </summary>
        private const int SM_CXSCREEN = 0;
        /// <summary>
        /// Height of screen in px
        /// </summary>
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

        /// <summary>
        /// Set cursor position [windows only]
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public static void SetCursorPosition(int x, int y)
        {
            SetCursorPos(x, y);
        }

        /// <summary>
        /// Get Cursor position [windows only]
        /// </summary>
        /// <returns></returns>
        public static POINT GetCursorPosition()
        {
            GetCursorPos(out POINT point);
            return point;
        }

        /// <summary>
        /// Smoothly move cursor to new position
        /// </summary>
        /// <param name="toX"></param>
        /// <param name="toY"></param>
        /// <param name="steps"></param>
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

        /// <summary>
        /// Trigger mouse right click event
        /// </summary>
        public static void RightClick()
        {
            mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, UIntPtr.Zero);
            mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, UIntPtr.Zero);
        }

        /// <summary>
        /// Simulates a middle mouse button click.
        /// </summary>
        /// <remarks>This method performs a middle mouse button click by simulating both the press and
        /// release actions. It is typically used for automation or testing scenarios where programmatic mouse input is
        /// required.</remarks>
        public static void MiddleClick()
        {
            mouse_event(MOUSEEVENTF_MIDDLEDOWN, 0, 0, 0, UIntPtr.Zero);
            mouse_event(MOUSEEVENTF_MIDDLEUP, 0, 0, 0, UIntPtr.Zero);
        }

        /// <summary>
        /// Simulates a left mouse button click at the current cursor position.
        /// </summary>
        /// <remarks>This method performs a left mouse button press followed by a release, effectively
        /// simulating a click. It uses the <c>mouse_event</c> function from the Windows API to generate the mouse
        /// events.</remarks>
        public static void Click()
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
        }

        /// <summary>
        /// Simulates a double-click action by performing two consecutive click operations with a short delay in
        /// between.
        /// </summary>
        /// <remarks>This method performs two click actions with a 100-millisecond delay between them to
        /// mimic a typical double-click behavior. Ensure that the environment where this method is used supports the
        /// concept of a double-click.</remarks>
        public static void DoubleClick()
        {
            Click();
            Thread.Sleep(100);
            Click();
        }

        /// <summary>
        /// Moves the cursor to the specified screen coordinates and performs a double-click action.
        /// </summary>
        /// <remarks>The cursor is moved smoothly to the specified position before performing the
        /// double-click. A short delay is introduced between the two clicks to ensure the double-click is
        /// registered.</remarks>
        /// <param name="toX">The X-coordinate of the target position on the screen.</param>
        /// <param name="toY">The Y-coordinate of the target position on the screen.</param>
        public static void MoveAndDoubleClick(int toX, int toY)
        {
            MoveCursorSmooth(toX, toY);
            Click();
            Thread.Sleep(100);
            Click();
        }

        /// <summary>
        /// Moves the cursor to the specified screen coordinates and performs a mouse click.
        /// </summary>
        /// <remarks>This method moves the cursor smoothly to the specified position, performs a click
        /// action,  and introduces a brief delay after the click. The delay ensures that subsequent operations  have
        /// time to process the click event.</remarks>
        /// <param name="toX">The X-coordinate, in pixels, to move the cursor to.</param>
        /// <param name="toY">The Y-coordinate, in pixels, to move the cursor to.</param>
        public static void MoveAndClick(int toX, int toY)
        {
            MoveCursorSmooth(toX, toY);
            Click();
            Thread.Sleep(100);
        }

        /// <summary>
        /// Moves the mouse cursor to the specified screen coordinates and performs a right-click.
        /// </summary>
        /// <remarks>The method moves the cursor smoothly to the specified position, performs a
        /// right-click,  and introduces a brief delay of 100 milliseconds after the click.</remarks>
        /// <param name="toX">The X-coordinate, in pixels, to move the cursor to.</param>
        /// <param name="toY">The Y-coordinate, in pixels, to move the cursor to.</param>
        public static void MoveAndRightClick(int toX, int toY)
        {
            MoveCursorSmooth(toX, toY);
            RightClick();
            Thread.Sleep(100);
        }

        /// <summary>
        /// Simulates a mouse scroll action by sending a scroll event with the specified amount.
        /// </summary>
        /// <remarks>The method uses the system's mouse event functionality to perform the scroll action. 
        /// The <paramref name="amount"/> parameter represents the scroll delta, typically in units  defined by the
        /// system's mouse wheel settings.</remarks>
        /// <param name="amount">The amount to scroll. Positive values scroll up, and negative values scroll down.</param>
        public static void Scroll(int amount)
        {
            mouse_event(MOUSEEVENTF_WHEEL, 0, 0, (uint)amount, UIntPtr.Zero);
        }

        /// <summary>
        /// Drag Items from current mouse position to new X,Y location
        /// </summary>
        /// <param name="toX"></param>
        /// <param name="toY"></param>
        public static void Drag(int toX, int toY)
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
            MoveCursorSmooth(toX, toY);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
        }

        // -------------------
        // Keyboard Control
        // -------------------
        /// <summary>
        /// 
        /// </summary>
        /// <param name="bVk"></param>
        /// <param name="bScan"></param>
        /// <param name="dwFlags"></param>
        /// <param name="dwExtraInfo"></param>
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
