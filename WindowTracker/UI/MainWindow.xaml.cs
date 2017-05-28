using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WindowTracker.PInvoke;

namespace WindowTracker.UI
{

    public class DataEvent
    {
        public DateTime Time;
        public String Name;
        public uint ThreadId;
    }

    public class DataProcess
    {
        public uint CurrentPid { get; set; }
        public string CurrentName { get; set;  }
        public List<DataEvent> Events = new List<DataEvent>();
        public List<String> DllList = new List<string>();
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<DataProcess> Processes { get; } = new ObservableCollection<DataProcess>();

        public readonly WinEventDelegate EventDelegate;

        public MainWindow()
        {
            InitializeComponent();
            EventDelegate = new WinEventDelegate(EventHandler);
        }

        void EventHandler(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            //Only want Window Events
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

                DataProcess d = Processes.FirstOrDefault(i => i.CurrentPid == processId);
                if (d != null)
                {
                    d.CurrentName = p.MainModule.ModuleName;
                }
                else
                {
                    Processes.Add(new DataProcess{
                        CurrentPid = processId,
                        CurrentName = p.MainModule.ModuleName
                    });
                }
            }
            catch (ArgumentException e)
            {
                
            }
        }
    }
}
