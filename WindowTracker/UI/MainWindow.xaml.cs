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
        public DateTime Time { get; set; }
        public String Name { get; set; }
        public uint ThreadId { get; set; }
    }

    public class DataProcess
    {
        public uint CurrentPid { get; set; }
        public string CurrentName { get; set;  }
        public ObservableCollection<DataEvent> Events = new ObservableCollection<DataEvent>();
        public ObservableCollection<String> DllList = new ObservableCollection<String>();
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
            EventDelegate = new WinEventDelegate(Win32EventHandler);
        }

        private void ProcessList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DataGrid dataGrid = sender as DataGrid;
            DataProcess selectedDataProcess = dataGrid?.SelectedItem as DataProcess;

            if (selectedDataProcess == null)
            {
                EventList.ItemsSource = null;
                DllList.ItemsSource = null;
            }
            else
            {
                EventList.ItemsSource = selectedDataProcess.Events;
                DllList.ItemsSource = selectedDataProcess.DllList;
            }

            EventList.Items.Refresh();
            DllList.Items.Refresh();
        }

        void Win32EventHandler(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
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

                //Check to see if a process with the same name exists. If it does update it. 
                //If it doesn't, add it.
                //TODO: Get a better way of getting the processes.
                DataProcess d = Processes.FirstOrDefault(i => i.CurrentPid == processId);
                if (d != null)
                {
                    d.CurrentName = p.MainModule.ModuleName;
                }
                else
                {
                    d = new DataProcess
                    {
                        CurrentPid = processId,
                        CurrentName = p.MainModule.ModuleName
                    };
                    Processes.Add(d);
                }

                if (eventType == User32.EVENT_OBJECT_DESTROY)
                    d.Events.Add(new DataEvent{Name = "EVENT_OBJECT_DESTROY", ThreadId = dwEventThread, Time = DateTime.Now});
                else if (eventType == User32.EVENT_OBJECT_CREATE)
                    d.Events.Add(new DataEvent { Name = "EVENT_OBJECT_CREATE", ThreadId = dwEventThread, Time = DateTime.Now });

                d.DllList.Clear();
                foreach (ProcessModule pModule in p.Modules) d.DllList.Add(pModule.ModuleName);
            }
            catch (ArgumentException e)
            {
                
            }
        }
    }
}
