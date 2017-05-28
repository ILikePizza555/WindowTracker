using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using WindowTracker.PInvoke;

namespace WindowTracker
{
    class Program
    {
        static WinEventDelegate procDelegate = new WinEventDelegate(EventHandler);

        static void Main(string[] args)
        {
            Console.WriteLine("Starting WindowTracker...");

            IntPtr hHook = User32.SetWinEventHook(User32.EVENT_OBJECT_CREATE, 
                User32.EVENT_OBJECT_DESTROY,
                IntPtr.Zero,
                procDelegate, 
                0, 
                0, 
                User32.WINEVENT_OUTOFCONTEXT | User32.WINEVENT_SKIPOWNPROCESS);

            //Windows Event Loop
            int messageResult;
            Message m = new Message();
            while ((messageResult = User32.GetMessage(ref m, IntPtr.Zero, 0, 0)) != 0)
            {
                if (messageResult < 0)
                {
                    Console.WriteLine("Error in the message loop: " + messageResult);
                }
                else
                {
                    User32.TranslateMessage(ref m);
                    User32.DispatchMessage(ref m);
                }
            }

            User32.UnhookWinEvent(hHook);
        }

        static void EventHandler(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (idObject != 0 || idChild != 0)
            {
                return;
            }

            uint processId;
            User32.GetWindowThreadProcessId(hwnd, out processId);

            if (processId == 0) return;

            try
            {
                Process p = Process.GetProcessById((int) processId);

                if (eventType == User32.EVENT_OBJECT_CREATE)
                    Console.WriteLine("[{0:yyyy-MM-dd HH:mm:ss:fff}] '{1}' (PID: {2}; TID:{3}) created a Window.",
                        DateTime.Now,
                        p.MainModule.ModuleName,
                        processId,
                        dwEventThread);
                else if (eventType == User32.EVENT_OBJECT_DESTROY)
                    Console.WriteLine("[{0:yyyy-MM-dd HH:mm:ss:fff}] '{1}' (PID: {2}; TID:{3}) destroyed a Window.",
                        DateTime.Now,
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
                Console.WriteLine(
                    "[{0:yyyy-MM-dd HH:mm:ss:fff}] Error getting process information. PID: {1} Event Type: {2} hWnd: {3}",
                    DateTime.Now, processId, eventType, hwnd);
                Console.WriteLine("\tError Message: " + e.Message);
            }
            catch (ArgumentException e)
            {
                Console.WriteLine(
                    "[{0:yyyy-MM-dd HH:mm:ss:fff}] Error getting event '{1}' for process {2}: The process had terminated.",
                    DateTime.Now,
                    eventType,
                    processId);
            }
        }
    }
}
