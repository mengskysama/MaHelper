using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;

namespace MAH
{
    class SysMsg
    {
        [DllImport("User32.dll", EntryPoint = "FindWindow")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("User32.dll", EntryPoint = "FindWindowEx")]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool PostMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

        [DllImport("User32.dll", EntryPoint = "SetForegroundWindow")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int y, int Width, int Height, int flags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SetActiveWindow(IntPtr hWnD);

        [DllImport("user32.dll", EntryPoint = "ShowWindow", SetLastError = true)]
        static extern bool ShowWindow(IntPtr hWnd, uint nCmdShow);

        // 获得窗口矩形
        [DllImport("user32.dll")]
        public static extern int GetWindowRect(IntPtr hWnd, out RECT lpRect);

        // 获得客户区矩形
        [DllImport("user32.dll")]
        public static extern int GetClientRect(IntPtr hWnd, out RECT lpRect);




        // 矩形结构
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        //按下鼠标左键   
        const uint WM_LBUTTONDOWN = 0x201;
        //释放鼠标左键   
        const uint WM_LBUTTONUP = 0x202;
        //
        const uint WM_MOUSEMOVE = 0x200;

        const uint NormalWith = 1030;
        const uint NormalHeight = 652;

        static int DeviceWidth = 0;
        static int DeviceHeight = 0;

        static uint wndShow = 0;

        static public IntPtr WinhWnd = IntPtr.Zero;
        static public IntPtr CtlhWnd = IntPtr.Zero;
        static public IntPtr TasihWnd = IntPtr.Zero;
        static public IntPtr ControlWnd = IntPtr.Zero;

        public static Form1 frm;

        public static int Init()
        {
            WinhWnd = FindWindow(null, "BlueStacks App Player for Windows (beta-1)");
            if (WinhWnd != IntPtr.Zero)
            {
                //模拟器相关句柄
                CtlhWnd = FindWindowEx(WinhWnd, IntPtr.Zero, null, "");
                ControlWnd = FindWindowEx(CtlhWnd, IntPtr.Zero, null, "");

                RECT rect;
                GetWindowRect(WinhWnd, out rect);
                DeviceHeight = rect.bottom - rect.top;
                DeviceWidth = rect.right - rect.left;

                TasihWnd = FindWindow("TaksiMaster", null);
                if (TasihWnd != null)
                {
                        //重新运行
                        WMQuit();
                        Thread.Sleep(1000);
                }

                //运行Taksi
                Process.Start("Taksi.exe");

                

                Thread.Sleep(3000);

                PostMessage(TasihWnd, 0x0112, 0xf120, 0);
                SetForegroundWindow(TasihWnd);
                SetWindowPos(TasihWnd, 0, 0, 0, 0, 0, 0x0002 | 0x0001);
                SetActiveWindow(TasihWnd);

                Thread.Sleep(500);

                PostMessage(frm.Handle, 0x0112, 0xf120, 0);
                SetForegroundWindow(frm.Handle);
                SetWindowPos(frm.Handle, 0, 0, 0, 0, 0, 0x0002 | 0x0001);
                SetActiveWindow(frm.Handle);

                Thread.Sleep(500);

                PostMessage(SysMsg.WinhWnd, 0x0112, 0xf120, 0);
                SetForegroundWindow(SysMsg.WinhWnd);
                SetWindowPos(SysMsg.WinhWnd, 0, 0, 0, 0, 0, 0x0002 | 0x0001);
                SetActiveWindow(SysMsg.WinhWnd);

                Thread.Sleep(500);

                PostMessage(frm.Handle, 0x0112, 0xf120, 0);
                SetForegroundWindow(frm.Handle);
                SetWindowPos(frm.Handle, 0, 0, 0, 0, 0, 0x0002 | 0x0001);
                SetActiveWindow(frm.Handle);
                
                TasihWnd = FindWindow("TaksiMaster", null);

            }
            else
                return -1;
            if (WinhWnd != IntPtr.Zero)
            {
                return 0;
            }
            return -2;
        }

        public static int Init2()
        {
            WinhWnd = FindWindow(null, "BlueStacks App Player for Windows (beta-1)");
            if (WinhWnd != IntPtr.Zero)
            {
                //模拟器相关句柄
                CtlhWnd = FindWindowEx(WinhWnd, IntPtr.Zero, null, "");
                ControlWnd = FindWindowEx(CtlhWnd, IntPtr.Zero, null, "");


                TasihWnd = FindWindow("TaksiMaster", null);


                RECT rect;
                GetWindowRect(WinhWnd, out rect);
                DeviceHeight = rect.bottom - rect.top;
                DeviceWidth = rect.right - rect.left;
            }
            else
                return -1;
            if (WinhWnd != IntPtr.Zero)
            {
                return 0;
            }
            return -2;
        }

        public static void MouseControlEvent(int Event, int PosX, int PosY)
        {
            PosX = (int)((float)DeviceWidth / (float)NormalWith * PosX);
            PosY = (int)((float)DeviceHeight / (float)NormalHeight * PosY);

            PostMessage(ControlWnd, WM_MOUSEMOVE, 1, PosX | PosY << 16);

            if (Event == 0)
                PostMessage(ControlWnd, WM_LBUTTONDOWN, 1, PosX | PosY << 16);
            else
                PostMessage(ControlWnd, WM_LBUTTONUP, 1, PosX | PosY << 16);
        }

        public static void MouseEvent(int Event, int PosX, int PosY)
        {
            PosX = (int)((float)DeviceWidth / (float)NormalWith * PosX);
            PosY = (int)((float)DeviceHeight / (float)NormalHeight * PosY);

            

            if (Event == 0)
                PostMessage(WinhWnd, WM_LBUTTONDOWN, 1, PosX | PosY << 16);
            else if(Event == 1)
                PostMessage(WinhWnd, WM_LBUTTONUP, 1, PosX | PosY << 16);
            else
                PostMessage(WinhWnd, WM_MOUSEMOVE, 1, PosX | PosY << 16);
        }

        public static void ShotEvent()
        {
            PostMessage(TasihWnd, 0x0111, 206, 0);
        }

        public static void WMQuit()
        {
            //卸载钩子通知
            TasihWnd = FindWindow("TaksiMaster", null);
            PostMessage(TasihWnd, 0x0010, 0, 0);
        }

        public static void HideMA()
        {
            if (wndShow == 0)
            {
                ShowWindow(WinhWnd, wndShow);
                wndShow = 1;
            }
            else
            {
                ShowWindow(WinhWnd, wndShow);
                wndShow = 0;
            }
        }
    }
}
