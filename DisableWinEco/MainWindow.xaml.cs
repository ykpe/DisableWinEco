using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using IWshRuntimeLibrary;

namespace DisableWinEco
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private string[] targerProcessNames;
        private NotifyIcon trayIcon = new();
        private bool isRunning = true;
        private CancellationTokenSource cts = new();
        private readonly string shortcutPath = "";
        private readonly string listFilePath = "";

        public MainWindow()
        {
            InitializeComponent();

            InitialTaryIcon();

            string? workingDictPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrEmpty(workingDictPath) == false)
            {
                listFilePath = Path.Combine(workingDictPath, "processlist.txt");
            }

            LoadListFromTemp();

            targerProcessNames = myTextBox.Text.Split(',');

            SwitchButtonText();

            Task taskA = Task.Run(() => ScheduledCheck(), cts.Token);

            string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            shortcutPath = Path.Combine(startupFolder, "DisableWinRco.lnk");

            if (System.IO.File.Exists(shortcutPath))
            {
                myCheckBox.IsChecked = true;
                this.Visibility = Visibility.Hidden;
            }
            else
            {
                myCheckBox.IsChecked = false;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            isRunning = !isRunning;
            SwitchButtonText();
            if (isRunning)
            {
                cts = new CancellationTokenSource();
                targerProcessNames = myTextBox.Text.Split(',');
                Task taskA = Task.Run(() => ScheduledCheck(), cts.Token);
                SaveListToTemp();
            }
            else
            {
                cts.Cancel();
            }
        }

        private void SwitchButtonText()
        {
            if (isRunning)
            {
                myButton.Content = "Stop";
            }
            else
            {
                myButton.Content = "Start";
            }
        }

        private void ScheduledCheck()
        {
            while (isRunning)
            {
                CheckProcesssProirity();
                System.Threading.Thread.Sleep(60000);
            }
        }

        private void CheckProcesssProirity()
        {
            foreach (var process in Process.GetProcesses())
            {
                bool containsKeyword = targerProcessNames.Any(keyword => process.ProcessName.Contains(keyword, StringComparison.OrdinalIgnoreCase));

                if (containsKeyword)
                {
                    IntPtr processHandle = OpenProcess(PROCESS_ALL_ACCESS, false, process.Id);

                    if (processHandle == IntPtr.Zero ||
                        GetPriorityClass(processHandle) == HIGH_PRIORITY_CLASS)
                    {
                        continue;
                    }

                    try
                    {
                        Debug.WriteLine(process.ProcessName);
                        SetPriorityExample(processHandle);
                    }
                    finally
                    {
                        CloseHandle(processHandle);
                    }
                }
            }
        }

        #region taryIconAndCloseEvent
        private void InitialTaryIcon()
        {
            trayIcon = new NotifyIcon
            {
                Icon = new System.Drawing.Icon("app.ico"),
                Visible = true
            };

            ContextMenuStrip menu = new();
            menu.Items.Add("Show", null, ShowApp);
            menu.Items.Add("Exit", null, ExitApp);

            trayIcon.ContextMenuStrip = menu;
        }
        private void ShowApp(object? sender, EventArgs e)
        {
            this.Activate();
            this.Visibility = Visibility.Visible;
        }

        private void ExitApp(object? sender, EventArgs e)
        {
            trayIcon.Visible = false;
            trayIcon.Dispose();
            Environment.Exit(0);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;  
            this.Visibility = Visibility.Hidden;  
        }
        #endregion

        #region ListCache
        private void SaveListToTemp()
        {
            System.IO.File.WriteAllLines(listFilePath, targerProcessNames);
            Debug.WriteLine("List saved to temp file: " + listFilePath);
        }

        private void LoadListFromTemp()
        {
            if (System.IO.File.Exists(listFilePath))
            {
                targerProcessNames = System.IO.File.ReadAllLines(listFilePath);
                myTextBox.Text = string.Join(",", targerProcessNames);
            }
            else
            {
                //Defualt value
                myTextBox.Text = "chrome,edge,opera";
            }
        }
        #endregion

        #region SetProcessPriority

 
        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        public static extern uint GetPriorityClass(IntPtr hProcess);

        [DllImport("kernel32.dll")]
        public static extern bool SetPriorityClass(IntPtr hProcess, uint dwPriorityClass);

        [DllImport("kernel32.dll")]
        public static extern bool CloseHandle(IntPtr hObject);

        const uint PROCESS_ALL_ACCESS = 0x1F0FFF; 
        const uint HIGH_PRIORITY_CLASS = 0x00000080;  

        static void SetPriorityExample(IntPtr processHandle)
        {
            if (SetPriorityClass(processHandle, HIGH_PRIORITY_CLASS))
            {
                Debug.WriteLine("Success");
            }
            else
            {
                Debug.WriteLine("Fail");
            }
        }
        #endregion

        #region CheckBox_autoStart
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            CreateStartupShortcut();
            Debug.WriteLine("CheckBox_Checked");
        }
        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            RemoveStartupShortcut();
            Debug.WriteLine("CheckBox_Unchecked");
        }

        private void CreateStartupShortcut()
        {
            if (!System.IO.File.Exists(shortcutPath))
            {
                IWshShell wsh = new WshShell();
                IWshShortcut shortcut = (IWshShortcut)wsh.CreateShortcut(shortcutPath);

                shortcut.TargetPath = Environment.ProcessPath;

                shortcut.WorkingDirectory = Path.GetDirectoryName(shortcut.TargetPath);
                shortcut.Description = "DisableWinRco Startup Shortcut";
                shortcut.Save();
            }
        }

        private void RemoveStartupShortcut()
        {
            if (System.IO.File.Exists(shortcutPath))
            {
                System.IO.File.Delete(shortcutPath);
            }
        }
        #endregion
    }
}