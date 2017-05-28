using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace WindowTracker.PInvoke
{
    public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);
    public delegate bool HandlerRoutine(CtrlTypes ctrlType);

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

    // An enumerated type for the control messages
    // sent to the handler routine.
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum CtrlTypes
    {
        CTRL_C_EVENT = 0,
        CTRL_BREAK_EVENT,
        CTRL_CLOSE_EVENT,
        CTRL_LOGOFF_EVENT = 5,
        CTRL_SHUTDOWN_EVENT
    }

    public static class User32
    {
        public const uint WINEVENT_OUTOFCONTEXT = 0;
        public const uint WINEVENT_SKIPOWNTHREAD = 1;
        public const uint WINEVENT_SKIPOWNPROCESS = 2;
        public const uint WINEVENT_INCONTEXT = 4;

        public const uint EVENT_OBJECT_CREATE = 0x8000;
        public const uint EVENT_OBJECT_DESTROY = 0x8001;

        [DllImport("user32.dll")]
        public static extern int GetMessage(ref Message lpMesage, IntPtr hWind, uint wMsgFilterMin, uint wMsgFilterMax);

        [DllImport("user32.dll")]
        public static extern bool TranslateMessage(ref Message lpMessage);

        [DllImport("user32.dll")]
        public static extern IntPtr DispatchMessage(ref Message lpMessage);

        [DllImport("user32.dll")]
        public static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc,
            WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        public static extern bool UnhookWinEvent(IntPtr hook);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
    }

    public static class Kernel32
    {
        public const uint THREAD_ALL_ACCESS = 0x001F03FF;

        [DllImport("Kernel32.dll")]
        public static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);

        [DllImport("Kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenThread(uint dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [DllImport("Kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("Kernel32.dll", SetLastError = true)]
        public static extern uint SuspendThread(IntPtr hThread);

        [DllImport("Kernel32.dll", SetLastError = true)]
        public static extern uint ResumeThread(IntPtr hThread);
    }
}