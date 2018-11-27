using System.Windows;
using System.IO;
using System.Windows.Media;
using System;
using System.Windows.Forms;

namespace Secode
{
    /// <summary>
    /// TextInput.xaml 的交互逻辑
    /// </summary>
    public partial class Setting : Window
    {
        public Setting()
        {
            InitializeComponent();
            InitControl();
        }

        public void InitControl()
        {
            Refresh();

            #region Protoc目录事件

            ProtocCheckBox.Checked += delegate
            {
                ProtocInput.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFFFF"));
            };
            ProtocCheckBox.Unchecked += delegate
            {
                ProtocInput.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7FFFFFFF"));
                ProtocInput.Content = Path.Combine(Environment.CurrentDirectory, "Protoc");
            };
            ProtocInput.MouseLeftButtonUp += delegate
            {
                if (ProtocCheckBox.IsChecked == false) return;

                FolderBrowserDialog dialog = new FolderBrowserDialog();
                dialog.RootFolder = Environment.SpecialFolder.Desktop;
                dialog.SelectedPath = ProtocInput.Content.ToString();
                dialog.Description = "选择Protoc目录";
                dialog.ShowNewFolderButton = true;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if (!Directory.Exists(dialog.SelectedPath)) return;

                    ProtocInput.Content = dialog.SelectedPath;

                    if (AppCheckBox.IsChecked == false)
                    {
                        AppInput.Content = Path.Combine(ProtocInput.Content.ToString(), "App", "protoc.exe");
                    }
                    if (SourceCheckBox.IsChecked == false)
                    {
                        SourceInput.Content = Path.Combine(ProtocInput.Content.ToString(), "Sources");
                    }
                    if (OutputCheckBox.IsChecked == false)
                    {
                        OutputInput.Content = Path.Combine(ProtocInput.Content.ToString(), "Output");
                    }
                }
            };

            #endregion

            #region Protoc.exe应用事件

            AppCheckBox.Checked += delegate
            {
                AppInput.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFFFF"));
            };
            AppCheckBox.Unchecked += delegate
            {
                AppInput.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7FFFFFFF"));
                AppInput.Content = Path.Combine(ProtocInput.Content.ToString(), "App", "protoc.exe");
            };
            AppInput.MouseLeftButtonUp += delegate
            {
                if (AppCheckBox.IsChecked == false) return;

                var finfo = new FileInfo(AppInput.Content.ToString());

                OpenFileDialog dialog = new OpenFileDialog();
                dialog.Title = "选择Protoc.exe应用";
                dialog.CheckFileExists = true;
                dialog.InitialDirectory = finfo.Exists ? finfo.Directory.FullName : Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                dialog.Filter = "EXE应用|*.exe";
                dialog.FileName = finfo.Name;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if (!Directory.Exists(dialog.FileName)) return;

                    ProtocInput.Content = dialog.FileName;
                }
            };

            #endregion

            #region 源文件目录事件

            SourceCheckBox.Checked += delegate
            {
                SourceInput.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFFFF"));
            };
            SourceCheckBox.Unchecked += delegate
            {
                SourceInput.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7FFFFFFF"));
                SourceInput.Content = Path.Combine(ProtocInput.Content.ToString(), "Sources");
            };
            SourceInput.MouseLeftButtonUp += delegate
            {
                if (SourceCheckBox.IsChecked == false) return;

                FolderBrowserDialog dialog = new FolderBrowserDialog();
                dialog.RootFolder = Environment.SpecialFolder.Desktop;
                dialog.SelectedPath = SourceInput.Content.ToString();
                dialog.Description = "选择源文件目录";
                dialog.ShowNewFolderButton = true;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if (!Directory.Exists(dialog.SelectedPath)) return;

                    SourceInput.Content = dialog.SelectedPath;
                }
            };

            #endregion

            #region 输出目录事件

            OutputCheckBox.Checked += delegate
            {
                OutputInput.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFFFF"));
            };
            OutputCheckBox.Unchecked += delegate
            {
                OutputInput.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7FFFFFFF"));
                OutputInput.Content = Path.Combine(ProtocInput.Content.ToString(), "Output");
            };
            OutputInput.MouseLeftButtonUp += delegate
            {
                if (OutputCheckBox.IsChecked == false) return;

                FolderBrowserDialog dialog = new FolderBrowserDialog();
                dialog.RootFolder = Environment.SpecialFolder.Desktop;
                dialog.SelectedPath = OutputInput.Content.ToString();
                dialog.Description = "选择输出目录";
                dialog.ShowNewFolderButton = true;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if (!Directory.Exists(dialog.SelectedPath)) return;

                    OutputInput.Content = dialog.SelectedPath;
                }
            };

            #endregion
        }

        public void Refresh()
        {
            ProtocCheckBox.IsChecked = DataSet.Contain("OpcodeGUI_ProtocPath");
            ProtocInput.Content = MainWindow.ProtocPath;
            if (ProtocCheckBox.IsChecked == false)
                ProtocInput.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7FFFFFFF"));

            AppCheckBox.IsChecked = DataSet.Contain("OpcodeGUI_AppPath");
            AppInput.Content = MainWindow.AppPath;
            if (AppCheckBox.IsChecked == false)
                AppInput.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7FFFFFFF"));

            SourceCheckBox.IsChecked = DataSet.Contain("OpcodeGUI_SourcesPath");
            SourceInput.Content = MainWindow.SourcesPath;
            if (SourceCheckBox.IsChecked == false)
                SourceInput.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7FFFFFFF"));

            OutputCheckBox.IsChecked = DataSet.Contain("OpcodeGUI_OutputPath");
            OutputInput.Content = MainWindow.OutputPath;
            if (OutputCheckBox.IsChecked == false)
                OutputInput.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7FFFFFFF"));
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            if (ProtocCheckBox.IsChecked == false || !Directory.Exists(ProtocInput.Content.ToString()))
            {
                DataSet.Delete("OpcodeGUI_ProtocPath");
            }
            else if (ProtocInput.Content.ToString() != MainWindow.ProtocPath)
            {
                MainWindow.ProtocPath = ProtocInput.Content.ToString();
            }
            if (AppCheckBox.IsChecked == false || !File.Exists(AppInput.Content.ToString()))
            {
                DataSet.Delete("OpcodeGUI_AppPath");
            }
            else if (AppInput.Content.ToString() != MainWindow.AppPath)
            {
                MainWindow.AppPath = AppInput.Content.ToString();
            }
            if (SourceCheckBox.IsChecked == false || !Directory.Exists(SourceInput.Content.ToString()))
            {
                DataSet.Delete("OpcodeGUI_SourcesPath");
            }
            else if (SourceInput.Content.ToString() != MainWindow.SourcesPath)
            {
                MainWindow.SourcesPath = SourceInput.Content.ToString();
            }
            if (OutputCheckBox.IsChecked == false || !Directory.Exists(OutputInput.Content.ToString()))
            {
                DataSet.Delete("OpcodeGUI_OutputPath");
            }
            else if (OutputInput.Content.ToString() != MainWindow.OutputPath)
            {
                MainWindow.OutputPath = OutputInput.Content.ToString();
            }
            Close();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
