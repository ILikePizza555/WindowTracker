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
            IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread,
            uint dwmsEventTime);

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
    }

    class Program
    {
        static WinEvents.WinEventDelegate procDelegate = new WinEvents.WinEventDelegate(EventHandler);

        static void Main(string[] args)
        {
            //Logging
            Trace.Listeners.Clear();

            TextWriterTraceListener twtl =
                new TextWriterTraceListener(Path.Combine(Path.GetTempPath(), AppDomain.CurrentDomain.FriendlyName))
                {
                    Name = $"WindowTracker-{DateTime.Now}",
                    TraceOutputOptions = TraceOptions.ThreadId | TraceOptions.DateTime
                };

            ConsoleTraceListener ctl = new ConsoleTraceListener(false) {TraceOutputOptions = TraceOptions.DateTime};

            Trace.Listeners.Add(twtl);
            Trace.Listeners.Add(ctl);
            Trace.AutoFlush = true;

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
                    Trace.WriteLine("Error in the message loop: " + messageResult);
                }
                else
                {
                    WinMessages.TranslateMessage(ref m);
                    WinMessages.DispatchMessage(ref m);
                }
            }

            WinEvents.UnhookWinEvent(hHook);
        }

        static void EventHandler(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild,
            uint dwEventThread,
            uint dwmsEventTime)
        {
            if (idObject != 0 || idChild != 0)
            {
                return;
            }

            uint processId;
            Windows.GetWindowThreadProcessId(hwnd, out processId);

            Process p = Process.GetProcessById((int) processId);

            try
            {
                if (eventType == WinEvents.EVENT_OBJECT_CREATE)
                    Trace.WriteLine($"Process {processId} '{p.MainModule.ModuleName}' created a Window.");
                else if (eventType == WinEvents.EVENT_OBJECT_DESTROY)
                    Trace.WriteLine($"Process {processId} '{p.MainModule.ModuleName}' destroyed a Window.");

                Console.WriteLine(
                    $"\tCPU - Total: {p.TotalProcessorTime} User: {p.UserProcessorTime} Privileged: {p.PrivilegedProcessorTime}");
            } catch (Win32Exception e)
            {
                Console.WriteLine(
                    $"Error getting process information. PID: {processId} Event Type: {eventType} hWnd: {hwnd}");
            }
        }
    }
}
