using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WindowTracker
{
    sealed class WinMessages
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct Point
        {
            public Int32 x;
            public Int32 y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Message
        {
            public IntPtr hWind;
            public uint message;
            public UIntPtr wParam;
            public IntPtr lParam;
            public UInt32 time;
            public Point pt;
        }

        [DllImport("user32.dll")]
        public static extern int GetMessage(ref Message lpMesage, IntPtr hWind, uint wMsgFilterMin, uint wMsgFilterMax);

        [DllImport("user32.dll")]
        public static extern bool TranslateMessage(ref Message lpMessage);

        [DllImport("user32.dll")]
        public static extern IntPtr DispatchMessage(ref Message lpMessage);
    }
    
    sealed class WinEvents
    {
        public const uint WINEVENT_OUTOFCONTEXT = 0;
        public const uint WINEVENT_SKIPOWNTHREAD = 1;
        public const uint WINEVENT_SKIPOWNPROCESS = 2;
        public const uint WINEVENT_INCONTEXT = 4;

        public const uint EVENT_OBJECT_CREATE = 0x8000;
        public const uint EVENT_OBJECT_DESTROY = 0x8001;

        public delegate void WinEventDelegate(
            IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        [DllImport("user32.dll")]
        public static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc,
            WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);
        
        [DllImport("user32.dll")]
        public static extern bool UnhookWinEvent(IntPtr hook);
    }

    sealed class Windows
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("Kernel32")]
        public static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);

        public delegate bool HandlerRoutine(CtrlTypes CtrlType);

        // An enumerated type for the control messages
        // sent to the handler routine.
        public enum CtrlTypes
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }
    }

    class Program
    {
        static WinEvents.WinEventDelegate procDelegate = new WinEvents.WinEventDelegate(EventHandler);

        static void Main(string[] args)
        {
            Console.WriteLine("Starting WindowTracker...");

            IntPtr hHook = WinEvents.SetWinEventHook(WinEvents.EVENT_OBJECT_CREATE, 
                WinEvents.EVENT_OBJECT_DESTROY,
                IntPtr.Zero,
                procDelegate, 
                0, 
                0, 
                WinEvents.WINEVENT_OUTOFCONTEXT | WinEvents.WINEVENT_SKIPOWNPROCESS);

            //Windows Event Loop
            int messageResult;
            WinMessages.Message m = new WinMessages.Message();
            while ((messageResult = WinMessages.GetMessage(ref m, IntPtr.Zero, 0, 0)) != 0)
            {
                if (messageResult < 0)
                {
                    Console.WriteLine("Error in the message loop: " + messageResult);
                }
                else
                {
                    WinMessages.TranslateMessage(ref m);
                    WinMessages.DispatchMessage(ref m);
                }
            }

            WinEvents.UnhookWinEvent(hHook);
        }

        static void EventHandler(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (idObject != 0 || idChild != 0)
            {
                return;
            }

            uint processId;
            Windows.GetWindowThreadProcessId(hwnd, out processId);

            if (processId == 0) return;

            try
            {
                Process p = Process.GetProcessById((int) processId);

                if (eventType == WinEvents.EVENT_OBJECT_CREATE)
                    Console.WriteLine("[{0:yyyy-MM-dd HH:mm:ss:fff}] '{1}' (PID: {2}; TID:{3}) created a Window.", DateTime.Now,
                        p.MainModule.ModuleName,
                        processId,
                        dwEventThread);
                else if (eventType == WinEvents.EVENT_OBJECT_DESTROY)
                    Console.WriteLine("[{0:yyyy-MM-dd HH:mm:ss:fff}] '{1}' (PID: {2}; TID:{3}) destroyed a Window.", DateTime.Now,
                        p.MainModule.ModuleName,
                        processId,
                        dwEventThread);

                Console.WriteLine("\tCPU - Total: {0} User: {1} Privileged: {2}", p.TotalProcessorTime,
                    p.UserProcessorTime, p.PrivilegedProcessorTime);

                foreach (ProcessModule pModule in p.Modules)
                {
                    Console.WriteLine("\tLoaded Module: {0}\t\tSize: {1}", pModule.ModuleName, pModule.ModuleMemorySize);
                }
            }
            catch (Win32Exception e)
            {
                Console.WriteLine("[{0:yyyy-MM-dd HH:mm:ss:fff}] Error getting process information. PID: {1} Event Type: {2} hWnd: {3}", 
                    DateTime.Now, processId, eventType, hwnd);
                Console.WriteLine("\tError Message: " + e.Message);
            }
            catch (ArgumentException e)
            {
                Console.WriteLine("[{0:yyyy-MM-dd HH:mm:ss:fff}] Error getting event '{1}' for process {2}: The process had terminated.", 
                    DateTime.Now,
                    eventType,
                    processId);
            }
        }
    }
}
