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
        private string[] targetProcessNames = Array.Empty<string>();
        private NotifyIcon trayIcon = new();
        private bool isRunning = true;
        private CancellationTokenSource cts = new();
        private readonly string shortcutPath = string.Empty;
        private readonly string listFilePath = string.Empty;
        private const int CHECK_INTERVAL_SECOND = 10;

        public MainWindow()
        {
            InitializeComponent();
            try
            {
                InitializeTrayIcon();

                string? workingDirPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                if (!string.IsNullOrEmpty(workingDirPath))
                {
                    listFilePath = Path.Combine(workingDirPath, "processlist.txt");
                }

                LoadListFromTemp();

                targetProcessNames = myTextBox.Text.Split(',', StringSplitOptions.RemoveEmptyEntries);

                SwitchButtonText();

                Task.Run(() => ScheduledCheck(), cts.Token);

                string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                shortcutPath = Path.Combine(startupFolder, "DisableWinEco.lnk");

                myCheckBox.IsChecked = System.IO.File.Exists(shortcutPath);
                this.Visibility = myCheckBox.IsChecked == true ? Visibility.Hidden : Visibility.Visible;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing application: {ex.Message}");
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                isRunning = !isRunning;
                SwitchButtonText();
                if (isRunning)
                {
                    cts = new CancellationTokenSource();
                    targetProcessNames = myTextBox.Text.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    Task.Run(() => ScheduledCheck(), cts.Token);
                    SaveListToTemp();
                }
                else
                {
                    cts.Cancel();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error on button click: {ex.Message}");
            }
        }

        private void SwitchButtonText()
        {
            myButton.Content = isRunning ? "Stop" : "Start";
        }

        private void ScheduledCheck()
        {
            try
            {
                while (isRunning)
                {
                    if (cts.Token.IsCancellationRequested)
                    {
                        break;
                    }

                    CheckProcessPriority();
                    Thread.Sleep(CHECK_INTERVAL_SECOND * 1000); // Updated sleep calculation for accuracy.
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in scheduled check: {ex.Message}");
            }
        }

        private void CheckProcessPriority()
        {
            try
            {
                foreach (var process in Process.GetProcesses())
                {
                    if (targetProcessNames.Any(keyword => !string.IsNullOrWhiteSpace(keyword) &&
                        process.ProcessName.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                    {
                        IntPtr processHandle = OpenProcess(PROCESS_ALL_ACCESS, false, process.Id);

                        if (processHandle == IntPtr.Zero || GetPriorityClass(processHandle) == HIGH_PRIORITY_CLASS)
                        {
                            continue;
                        }

                        try
                        {
                            Debug.WriteLine($"Changing priority of process: {process.ProcessName}");
                            SetPriorityExample(processHandle);
                        }
                        finally
                        {
                            CloseHandle(processHandle);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking process priority: {ex.Message}");
            }
        }

        private void InitializeTrayIcon()
        {
            trayIcon = new NotifyIcon
            {
                Text = "Disable Efficiency Mode",
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath),
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

        private void SaveListToTemp()
        {
            try
            {
                System.IO.File.WriteAllLines(listFilePath, targetProcessNames);
                Debug.WriteLine($"List saved to temp file: {listFilePath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving list to temp file: {ex.Message}");
            }
        }

        private void LoadListFromTemp()
        {
            try
            {
                if (System.IO.File.Exists(listFilePath))
                {
                    targetProcessNames = System.IO.File.ReadAllLines(listFilePath);
                    myTextBox.Text = string.Join(",", targetProcessNames);
                }
                else
                {
                    myTextBox.Text = "chrome,edge,opera"; // Default value.
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading list from temp file: {ex.Message}");
            }
        }

        private void CreateStartupShortcut()
        {
            try
            {
                if (!System.IO.File.Exists(shortcutPath))
                {
                    IWshShell wsh = new WshShell();
                    IWshShortcut shortcut = (IWshShortcut)wsh.CreateShortcut(shortcutPath);

                    shortcut.TargetPath = Environment.ProcessPath ?? string.Empty;
                    shortcut.WorkingDirectory = Path.GetDirectoryName(shortcut.TargetPath);
                    shortcut.Description = "DisableWinEco Startup Shortcut";
                    shortcut.Save();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating startup shortcut: {ex.Message}");
            }
        }

        private void RemoveStartupShortcut()
        {
            try
            {
                if (System.IO.File.Exists(shortcutPath))
                {
                    System.IO.File.Delete(shortcutPath);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error removing startup shortcut: {ex.Message}");
            }
        }

        #region Native Methods and Constants

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        public static extern uint GetPriorityClass(IntPtr hProcess);

        [DllImport("kernel32.dll")]
        public static extern bool SetPriorityClass(IntPtr hProcess, uint dwPriorityClass);

        [DllImport("kernel32.dll")]
        public static extern bool CloseHandle(IntPtr hObject);

        private const uint PROCESS_ALL_ACCESS = 0x1F0FFF;
        private const uint HIGH_PRIORITY_CLASS = 0x00000080;

        private static void SetPriorityExample(IntPtr processHandle)
        {
            try
            {
                if (SetPriorityClass(processHandle, HIGH_PRIORITY_CLASS))
                {
                    Debug.WriteLine("Successfully set high priority.");
                }
                else
                {
                    Debug.WriteLine("Failed to set priority.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting priority: {ex.Message}");
            }
        }

        #endregion

        #region CheckBox_autoStart

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                CreateStartupShortcut();
                Debug.WriteLine("CheckBox_Checked: Startup shortcut created.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CheckBox_Checked: {ex.Message}");
            }
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                RemoveStartupShortcut();
                Debug.WriteLine("CheckBox_Unchecked: Startup shortcut removed.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CheckBox_Unchecked: {ex.Message}");
            }
        }

        #endregion
    }

}