using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using IWshRuntimeLibrary;

namespace DisableWinEco
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private string[] targerProcessNames;
        private NotifyIcon trayIcon;
        private bool isRunning = true;
        private CancellationTokenSource cts = new CancellationTokenSource();
        private string startupFolder;
        private string shortcutPath;
        private string processListFilePath;

        public MainWindow()
        {
            InitializeComponent();
            trayIcon = new NotifyIcon
            {
                Icon = new System.Drawing.Icon("app.ico"),
                Visible = true
            };


            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("Show", null, ShowApp);
            menu.Items.Add("Exit", null, ExitApp);

            trayIcon.ContextMenuStrip = menu;

            string workingDictPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);


            processListFilePath = Path.Combine(workingDictPath, "processlist.txt");

            LoadListFromTemp();


            targerProcessNames = myTextBox.Text.Split(',');
            SwitchButtonText();
            Task taskA = Task.Run(() => ScheduledCheck(), cts.Token);

            startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            shortcutPath = Path.Combine(startupFolder, "DisableWinRco.lnk");
            if (System.IO.File.Exists(shortcutPath))
            {
                myCheckBox.IsChecked = true;
            }
            else
            {
                myCheckBox.IsChecked = false;

            }
        }
        // 顯示主畫面
        private void ShowApp(object sender, EventArgs e)
        {
            this.Activate();
            this.Visibility = Visibility.Visible;
        }

        // 結束程式
        private void ExitApp(object sender, EventArgs e)
        {
            trayIcon.Visible = false;
            trayIcon.Dispose();
            Environment.Exit(0);
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
                Debug.WriteLine("Task A Running.");
                System.Threading.Thread.Sleep(1000); // 每秒檢查一次
            }
            Debug.WriteLine("Task A has stopped.");

        }

        private void CheckProcesssProirity()
        {
            foreach (var process in Process.GetProcesses())
            {
                bool containsKeyword = targerProcessNames.Any(keyword => process.ProcessName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);

                if (containsKeyword)
                {
                    // 打開進程
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true; // 取消關閉事件
            this.Visibility = Visibility.Hidden; // 隱藏窗口
        }


        private void SaveListToTemp()
        {
            System.IO.File.WriteAllLines(processListFilePath, targerProcessNames);
            Debug.WriteLine("List saved to temp file: " + processListFilePath);
        }

        private void LoadListFromTemp()
        {
            if (System.IO.File.Exists(processListFilePath))
            {
                targerProcessNames = System.IO.File.ReadAllLines(processListFilePath);
                myTextBox.Text = string.Join(",", targerProcessNames);
                Debug.WriteLine("List loaded from temp file: " + processListFilePath);
            }
            else
            {
                myTextBox.Text = "chrome,edge,opera";
                Debug.WriteLine("Temp file not found.");
            }
        }

        #region SetProcessPriority

        // 定義 Windows API 函數
        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        public static extern uint GetPriorityClass(IntPtr hProcess);

        [DllImport("kernel32.dll")]
        public static extern bool SetPriorityClass(IntPtr hProcess, uint dwPriorityClass);

        [DllImport("kernel32.dll")]
        public static extern bool CloseHandle(IntPtr hObject);

        const uint PROCESS_ALL_ACCESS = 0x1F0FFF; // 完全訪問權限
        const uint HIGH_PRIORITY_CLASS = 0x00000080; // 高優先級類別

        static void SetPriorityExample(IntPtr processHandle)
        {
            if (SetPriorityClass(processHandle, HIGH_PRIORITY_CLASS))
            {
                Debug.WriteLine("成功設置進程為高優先級！");
            }
            else
            {
                System.Windows.MessageBox.Show("設定失敗");
            }
        }
        #endregion

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

    }
}