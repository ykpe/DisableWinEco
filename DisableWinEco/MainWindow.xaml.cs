using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace DisableWinEco
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private NotifyIcon trayIcon;

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
            myTextBox.Text = "chrome,edge,opera,github,outlook,excel";
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
            string[] targerProcessNames = myTextBox.Text.Split(',');
            foreach (var process in Process.GetProcesses())
            {
               
                bool containsKeyword = targerProcessNames.Any(keyword => process.ProcessName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);

                if (containsKeyword)
                {
                    // 打開進程
                    IntPtr processHandle = OpenProcess(PROCESS_ALL_ACCESS, false, process.Id);

                    if (processHandle == IntPtr.Zero)
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

        #region SetProcessPriority

        // 定義 Windows API 函數
        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

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
    }
}