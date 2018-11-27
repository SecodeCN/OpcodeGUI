using System;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Controls;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Media;

using System.Xml;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Search;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit.Document;
using System.Windows.Input;
using ICSharpCode.AvalonEdit.Editing;

namespace Secode
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        #region 路径参数

        /// <summary>
        /// 当前编辑项目
        /// </summary>
        public string CurrentOption
        {
            get
            {
                if (!DataSet.Contain("OpcodeGUI_CurrentOption")) return "";
                return DataSet.GetString("OpcodeGUI_CurrentOption");
            }
            set
            {
                DataSet.Set("OpcodeGUI_CurrentOption", value);
            }
        }

        /// <summary>
        /// Protoc目录
        /// </summary>
        public static string ProtocPath
        {
            get
            {
                if (!DataSet.Contain("OpcodeGUI_ProtocPath"))
                {
                    return Path.Combine(Environment.CurrentDirectory, "Protoc");
                }
                return DataSet.GetString("OpcodeGUI_ProtocPath");
            }
            set
            {
                DataSet.Set("OpcodeGUI_ProtocPath", value);
            }
        }

        /// <summary>
        /// Protoc.exe应用路径
        /// </summary>
        public static string AppPath
        {
            get
            {
                var path = "";
                if (!DataSet.Contain("OpcodeGUI_AppPath"))
                {
                    path = Path.Combine(ProtocPath, "App", "protoc.exe");
                }
                else
                {
                    path = DataSet.GetString("OpcodeGUI_AppPath");
                }
                var finfo = new FileInfo(path);
                if (!finfo.Exists) return null;
                return path;
            }
            set
            {
                DataSet.Set("OpcodeGUI_AppPath", value);
            }
        }

        /// <summary>
        /// 源文件保存路径
        /// </summary>
        public static string SourcesPath
        {
            get
            {
                var path = "";
                if (!DataSet.Contain("OpcodeGUI_SourcesPath"))
                {
                    path = Path.Combine(ProtocPath, "Sources");
                }
                else
                {
                    path = DataSet.GetString("OpcodeGUI_SourcesPath");
                }
                var dinfo = new DirectoryInfo(path);
                if (!dinfo.Exists) dinfo.Create();
                return path;
            }
            set
            {
                DataSet.Set("OpcodeGUI_SourcesPath", value);
            }
        }

        /// <summary>
        /// 输出路径
        /// </summary>
        public static string OutputPath
        {
            get
            {
                var path = "";
                if (!DataSet.Contain("OpcodeGUI_OutputPath"))
                {
                    path = Path.Combine(ProtocPath, "Output");
                }
                else
                {
                    path = DataSet.GetString("OpcodeGUI_OutputPath");
                }
                var dinfo = new DirectoryInfo(path);
                if (!dinfo.Exists) dinfo.Create();
                return path;
            }
            set
            {
                DataSet.Set("OpcodeGUI_OutputPath", value);
            }
        }

        #endregion

        #region 初始化

        public MainWindow()
        {
            InitializeComponent();
            InitControl();
        }

        /// <summary>
        /// 初始化组件
        /// </summary>
        public void InitControl()
        {
            InitContentEditor();

            LoadList();
            LoadContent();

            OptionListBox.SelectionChanged += delegate
            {
                if (OptionListBox.SelectedItem == null) return;
                CurrentOption = ((ListBoxItem)OptionListBox.SelectedItem).Content.ToString();
                LoadContent();
            };

            ContentText.TextChanged += delegate
            {
                SaveContent();
            };

            NewButton.Click += delegate
            {
                New();
            };

            DeleteButton.Click += delegate
            {
                Delete();
            };

            OpenProtocPathButton.Click += delegate
            {
                OpenProtocPath();
            };

            SettingButton.Click += delegate
            {
                OpenSettingPanel();
            };

            RefreshButton.Click += delegate
            {
                LoadList();
                LoadContent();
                Log("重新加载完成");
            };

            SaveButton.Click += delegate
            {
                Save();
            };
        }

        #endregion

        #region 加载

        #region 初始化文本编辑器

        /// <summary>
        /// 初始化文本编辑器
        /// </summary>
        public void InitContentEditor()
        {
            #region 加载高亮配置
            IHighlightingDefinition protocHighlighting = LoadHighlighting("ProtocHighlighting");
            IHighlightingDefinition csharpHighlighting = LoadHighlighting("CSharpHighlighting");
            #endregion

            #region 注册高亮配置
            //HighlightingManager.Instance.RegisterHighlighting("Protoc", new string[] { ".proto" }, protocHighlighting);
            #endregion

            #region 配置文本格式化类型
            this.SetValue(TextOptions.TextFormattingModeProperty, TextFormattingMode.Display);
            #endregion

            #region 注册输入提示事件
            ContentText.TextArea.TextEntering += ContentText_TextArea_TextEntering;
            ContentText.TextArea.TextEntered += ContentText_TextArea_TextEntered;
            #endregion

            #region 配置文本高亮设置
            ContentText.SyntaxHighlighting = protocHighlighting;
            GenerateText.SyntaxHighlighting = csharpHighlighting;
            OpcodeText.SyntaxHighlighting = csharpHighlighting;
            #endregion

            #region 配置搜索设置
            SearchPanel.Install(ContentText);
            #endregion

            #region Folder 配置初始化

            FoldingStrategys = new Dictionary<ICSharpCode.AvalonEdit.TextEditor, object>();
            FoldingManagers = new Dictionary<ICSharpCode.AvalonEdit.TextEditor, FoldingManager>();
            FoldingUpdateTimer = new DispatcherTimer();
            FoldingUpdateTimer.Interval = TimeSpan.FromSeconds(2);

            InitFoldings(ContentText);
            InitFoldings(GenerateText);
            InitFoldings(OpcodeText);

            FoldingUpdateTimer.Start();

            #endregion
        }

        #region 加载高亮配置

        /// <summary>
        /// 加载高亮配置
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public IHighlightingDefinition LoadHighlighting(string key)
        {
            IHighlightingDefinition protocHighlighting;
            using (Stream s = typeof(MainWindow).Assembly.GetManifestResourceStream("Secode.Xshd." + key + ".xshd"))
            {
                if (s == null)
                {
                    Console.WriteLine("Could not find embedded resource： " + key);
                    return null;
                }
                else
                {
                    using (XmlReader reader = new XmlTextReader(s))
                    {
                        protocHighlighting = ICSharpCode.AvalonEdit.Highlighting.Xshd.
                            HighlightingLoader.Load(reader, HighlightingManager.Instance);
                    }
                }
            }
            return protocHighlighting;
        }

        #endregion

        #region Folder 配置

        private Dictionary<ICSharpCode.AvalonEdit.TextEditor, FoldingManager> FoldingManagers;
        private Dictionary<ICSharpCode.AvalonEdit.TextEditor, object> FoldingStrategys;
        private DispatcherTimer FoldingUpdateTimer;

        /// <summary>
        /// 初始化编辑器的Folder合并
        /// </summary>
        /// <param name="editor"></param>
        public void InitFoldings(ICSharpCode.AvalonEdit.TextEditor editor)
        {
            FoldingStrategys.Add(editor, new BraceFoldingStrategy());
            FoldingManagers.Add(editor, FoldingManager.Install(editor.TextArea));
            FoldingUpdateTimer.Tick += delegate { UpdateFoldings(editor); };
        }

        /// <summary>
        /// 更新Folder合并
        /// </summary>
        /// <param name="editor"></param>
        public void UpdateFoldings(ICSharpCode.AvalonEdit.TextEditor editor)
        {
            if (!FoldingStrategys.ContainsKey(editor)) return;

            if (FoldingStrategys[editor] is BraceFoldingStrategy)
            {
                ((BraceFoldingStrategy)FoldingStrategys[editor]).UpdateFoldings(FoldingManagers[editor], editor.Document);
            }
            if (FoldingStrategys[editor] is XmlFoldingStrategy)
            {
                ((XmlFoldingStrategy)FoldingStrategys[editor]).UpdateFoldings(FoldingManagers[editor], editor.Document);
            }
        }

        #endregion

        #region 注册输入提示事件

        public void ContentText_TextArea_TextEntered(object sender, TextCompositionEventArgs e)
        {
            switch (e.Text)
            {
                case "\n":
                    if (CurrentLineParse.Length == 0)
                    {
                        ShowCompletionWindow(new Dictionary<string, string>()
                        {
                         { "syntax", "Protoc类型" },
                         { "message", "消息名称" },
                         { "package", "命名空间" },
                        });
                    }
                    else
                    {
                        if (CurrentLineText.Length < CurrentLineParse.Length)
                            CurrentWrite(CurrentLineParse.Substring(CurrentLineText.Length));
                        ShowCompletionWindow(new Dictionary<string, string>()
                        {
                         { "int32", "32位int" },
                         { "int64", "64位int" },
                         { "float", "浮点型" },
                         { "string", "字符串" },
                         { "bytes", "二进制数据" },
                         { "repeated", "队列，可为多项的" },
                        });
                    }
                    break;
                case "=":
                    if (CurrentLineContent == "syntax=")
                    {
                        ShowCompletionWindow(new Dictionary<string, string>()
                        {
                         { "\"proto3\";", "Proto3模式" },
                         { "\"proto2\";", "Proto2模式" },
                        });
                    }
                    break;
                case " ":
                    if (CurrentLineContent == "repeated")
                    {
                        ShowCompletionWindow(new Dictionary<string, string>()
                        {
                         { "int32", "32位int" },
                         { "int64", "64位int" },
                         { "float", "浮点型" },
                         { "string", "字符串" },
                         { "bytes", "二进制数据" },
                        });
                    }
                    break;
                case "{":
                case ";":
                case "}":
                    AutoFormat();
                    break;
            }
        }

        public void ContentText_TextArea_TextEntering(object sender, TextCompositionEventArgs e)
        {
            if (e.Text.Length > 0 && completionWindow != null)
            {
                if (!char.IsLetterOrDigit(e.Text[0]))
                {
                    // Whenever a non-letter is typed while the completion window is open,
                    // insert the currently selected element.
                    completionWindow.CompletionList.RequestInsertion(e);
                }
            }
        }

        #region 执行方法

        private CompletionWindow completionWindow;

        public void ShowCompletionWindow(Dictionary<string, string> pairs)
        {
            // open code completion after the user has pressed dot:
            completionWindow = new CompletionWindow(ContentText.TextArea);
            // provide AvalonEdit with the data:
            IList<ICompletionData> data = completionWindow.CompletionList.CompletionData;

            foreach (var key in pairs)
            {
                data.Add(new CompletionData(key.Key, key.Value));
            }

            completionWindow.Show();

            if (completionWindow.CompletionList.ScrollViewer != null)
            {
                completionWindow.CompletionList.ScrollViewer.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF2D2D30"));
                completionWindow.CompletionList.ScrollViewer.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFFFF"));
            }

            completionWindow.Closed += delegate
            {
                completionWindow = null;
            };
        }

        /// <summary>
        /// 光标所在行内容
        /// </summary>
        public string CurrentLineContent
        {
            get
            {
                return CurrentLineText.Replace("\t", "").Replace(" ", "");
            }
        }

        /// <summary>
        /// 光标所在行完整文本
        /// </summary>
        public string CurrentLineText
        {
            get
            {
                var str = ContentText.Text;
                str = str.Substring(0, ContentText.CaretOffset);
                var lines = str.Split('\n');
                return lines[lines.Length - 1];
            }
        }

        /// <summary>
        /// 光标所在行内容
        /// </summary>
        public string CurrentLineParse
        {
            get
            {
                var str = ContentText.Text;
                str = str.Substring(0, ContentText.CaretOffset);
                var len = str.Replace("{", "{{").Replace("}", "").Length - str.Length;
                var parse = "";
                for (int i = 0; i < len; i++) parse += "\t";
                return parse;
            }
        }

        /// <summary>
        /// 光标所在处添加内容光标后移
        /// </summary>
        /// <param name="text"></param>
        public void CurrentWrite(string text)
        {
            var index = ContentText.CaretOffset;
            var str = ContentText.Text;
            var str1 = str.Substring(0, index);
            var str2 = str.Substring(index);
            ContentText.Text = str1 + text + str2;
            ContentText.CaretOffset = index + text.Length;
            ContentText.Focus();
        }

        #endregion

        #endregion

        #region 自动格式化

        /// <summary>
        /// 自动格式化
        /// </summary>
        /// <param name="editor"></param>
        public void AutoFormat()
        {
            var index = ContentText.CaretOffset;

            var text = ContentText.Text;
            var tempkey = "" + ((char)1);
            var tempkey2 = "" + ((char)2) + ((char)2);
            var tempkey3 = "" + ((char)3) + ((char)3);
            text = text.Replace("\\", tempkey);
            text = text.Replace(tempkey + tempkey, tempkey2);
            text = text.Replace(tempkey + "\"", tempkey3);
            var result = "";
            var strs = text.Split('\"');
            var parse = "";
            for (int i = 0; i < strs.Length; i += 1)
            {
                if (i % 2 == 1)
                {
                    result += "\"" + strs[i] + "\"";
                }
                else
                {
                    if (result.Length + strs[i].Length <= index)
                    {
                        var formatstr = Format(strs[i], ref parse);
                        index += formatstr.Length - strs[i].Length;
                        result += formatstr;
                    }
                    else if (result.Length < index)
                    {
                        var str1 = strs[i].Substring(0, index - result.Length);
                        var str2 = strs[i].Substring(index - result.Length);
                        var formatstr = Format(str1, ref parse);
                        index += formatstr.Length - str1.Length;
                        if (formatstr.EndsWith("\n\n") && str2.StartsWith("\n"))
                        {
                            formatstr = formatstr.Substring(0, formatstr.Length - 1);
                            index -= 1;
                        }
                        else if (formatstr.EndsWith(";\n") && str2.StartsWith("\n"))
                        {
                            formatstr = formatstr.Substring(0, formatstr.Length - 1);
                            index -= 1;
                        }
                        result += formatstr;
                        result += Format(str2, ref parse);
                    }
                    else
                    {
                        result += Format(strs[i], ref parse);
                    }
                }
            }
            result = result.Replace(tempkey3, tempkey + "\"");
            result = result.Replace(tempkey2, tempkey + tempkey);
            result = result.Replace(tempkey, "\\");

            ContentText.Text = result;

            ContentText.CaretOffset = index;
            ContentText.Focus();
        }

        public string Format(string text, ref string parse)
        {
            text = text.Replace("\r", "");
            text = text.Replace("\t", "");
            text = text.Replace("{", "\n{\n");
            text = text.Replace("}", "\n}\n");
            text = text.Replace("//", " // ");
            text = text.Replace("=", " = ");
            text = text.Replace("；", ";");
            while (text.Contains("\n\n")) text = text.Replace("\n\n", "\n");
            while (text.Contains("  ")) text = text.Replace("  ", " ");
            while (text.Contains("\n ")) text = text.Replace("\n ", "\n");
            text = text.Replace(" ;", ";");
            text = text.Replace("}", "}\n");
            var lines = text.Split('\n');
            var result = "";
            foreach (var line in lines)
            {
                if (line == "{")
                {
                    result += (string.IsNullOrEmpty(result) ? "" : parse) + line + "\n";
                    parse += "\t";
                }
                else if (line == "}")
                {
                    var len = parse.Length - 1;
                    if (len < 0) len = 0;
                    parse = parse.Substring(0, len);
                    result += (string.IsNullOrEmpty(result) ? "" : parse) + line + "\n";
                }
                else if (parse.Length != 0 && line.StartsWith("//"))
                {
                    result = result.Substring(0, result.Length - 1);
                    result += " " + line + "\n";
                }
                else
                {
                    result += (string.IsNullOrEmpty(result) ? "" : parse) + line + "\n";
                }
            }

            result = result.Substring(0, result.Length - 1);

            return result;
        }

        #endregion

        #endregion

        #region 加载列表和内容

        /// <summary>
        /// 加载队列
        /// </summary>
        public void LoadList()
        {
            var dinfo = new DirectoryInfo(SourcesPath);
            if (!dinfo.Exists) dinfo.Create();

            OptionListBox.Items.Clear();

            var files = dinfo.GetFiles();
            foreach (var file in files)
            {
                if (file.Extension != ".proto") continue;
                var item = new ListBoxItem();
                item.Content = file.Name.Replace(".proto", "");
                OptionListBox.Items.Add(item);
                if (item.Content.ToString() == CurrentOption)
                {
                    OptionListBox.SelectedItem = item;
                }
            }

            if (OptionListBox.SelectedItem == null)
            {
                OptionListBox.SelectedIndex = 0;
                CurrentOption = ((ListBoxItem)OptionListBox.SelectedItem).Content.ToString();
            }
        }

        /// <summary>
        /// 加载内容
        /// </summary>
        public void LoadContent()
        {
            var path = Path.Combine(SourcesPath, CurrentOption + ".proto");
            if (!File.Exists(path)) return;
            var text = File.ReadAllText(path);
            ContentText.Text = text;
            Run();
        }

        #endregion

        #endregion

        #region 功能

        /// <summary>
        /// 新增
        /// </summary>
        public void New()
        {
            var dialog = new TextInput("请输入新建源文件名称");
            var ans = dialog.ShowDialog();
            if (ans == true)
            {
                var name = dialog.InputText.Text;
                if (string.IsNullOrEmpty(name)) return;
                if (!name.EndsWith("Message"))
                {
                    Log("新建源文件失败，必须以'Message'结尾命名");
                    return;
                }
                var path = Path.Combine(SourcesPath, name + ".proto");
                if (File.Exists(path)) return;
                File.WriteAllText(path, ContentText.Text);
                CurrentOption = name;
                LoadList();
                LoadContent();
                Log("新建源文件“" + name + "”成功");
            }
        }

        /// <summary>
        /// 删除
        /// </summary>
        public void Delete()
        {
            var path = Path.Combine(SourcesPath, CurrentOption + ".proto");
            if (!File.Exists(path)) return;
            File.Delete(path);
            Log("删除源文件“" + CurrentOption + "”成功");
            LoadList();
            LoadContent();
        }

        /// <summary>
        /// 打开Protoc目录
        /// </summary>
        public void OpenProtocPath()
        {
            System.Diagnostics.Process.Start("Explorer.exe", ProtocPath);
        }

        /// <summary>
        /// 打开设置面板
        /// </summary>
        public void OpenSettingPanel()
        {
            var dialog = new Setting();
            var ans = dialog.ShowDialog();
            if (ans == true)
            {
                LoadList();
                LoadContent();
                Log("重新加载数据成功");
            }
        }

        /// <summary>
        /// 保存
        /// </summary>
        public void Save()
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Title = "保存生成的脚本";
            dialog.DefaultExt = ".cs";
            dialog.Filter = "C#文件|*.cs";
            dialog.AddExtension = true;
            dialog.FileName = CurrentOption + ".cs";

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var path = dialog.FileName;
                var sourcepath = Path.Combine(OutputPath, CurrentOption + ".cs");
                File.Copy(sourcepath, path, true);
                var finfo = new FileInfo(path);
                var dpath = finfo.Directory.FullName;
                var oppath = Path.Combine(dpath, CurrentOption.Replace("Message", "Opcode") + ".cs");
                File.WriteAllText(oppath, OpcodeText.Text);
                System.Diagnostics.Process.Start("Explorer.exe", dpath);
                Log("保存生成脚本完成");
            }
        }

        #endregion

        #region 执行方法

        /// <summary>
        /// 保存内容
        /// </summary>
        public void SaveContent()
        {
            var path = Path.Combine(SourcesPath, CurrentOption + ".proto");
            File.WriteAllText(path, ContentText.Text);
            Run();
        }

        /// <summary>
        /// 执行
        /// </summary>
        public void Run()
        {
            Run(CurrentOption);
            GenerateOpcode();
        }

        /// <summary>
        /// 全部执行
        /// </summary>
        public void AllRun()
        {
            foreach (var key in OptionListBox.Items)
            {
                Run(((ListBoxItem)key).Content.ToString());
            }
        }

        /// <summary>
        /// 执行制定生成
        /// </summary>
        /// <param name="op"></param>
        public void Run(string op)
        {
            if (AppPath == null) return;

            var path = Path.Combine(OutputPath, CurrentOption + ".cs");
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.UseShellExecute = false;    //是否使用操作系统shell启动
            p.StartInfo.RedirectStandardInput = true;//接受来自调用程序的输入信息
            p.StartInfo.RedirectStandardOutput = true;//由调用程序获取输出信息
            p.StartInfo.RedirectStandardError = true;//重定向标准错误输出
            p.StartInfo.CreateNoWindow = true;//不显示程序窗口
            p.Start();//启动程序

            //向cmd窗口发送输入信息
            p.StandardInput.WriteLine(AppPath.Replace("\\", "/") + " --csharp_out=\"" + OutputPath.Replace("\\", "/") + "/\" --proto_path=\"" + SourcesPath.Replace("\\", "/") + "/\" " + op + ".proto" + "&exit");
            p.StandardInput.AutoFlush = true;

            var ans = p.WaitForExit(1000);
            p.Close();

            if (!ans)
            {
                GenerateText.Text = "";
                return;
            }

            //Log(op + "生成完成." );

            path = Path.Combine(OutputPath, CurrentOption + ".cs");
            if (File.Exists(path))
            {
                var text = File.ReadAllText(path);
                var index = GenerateText.CaretOffset;
                GenerateText.Text = text.Replace("global::Google.Protobuf", "global::Secode.Protobuf");
                GenerateText.CaretOffset = index;
                File.WriteAllText(path, GenerateText.Text);
            }
        }

        /// <summary>
        /// 生成Opcode
        /// </summary>
        public void GenerateOpcode()
        {
            var Index = 0;
            var NameSpace = "Secode.Network";
            var OpcodeClass = CurrentOption.Replace("Message", "Opcode");
            var lines = File.ReadAllLines(Path.Combine(SourcesPath, CurrentOption + ".proto"));

            #region Get Index
            foreach (var line in lines)
            {
                var temp = line.Replace("/", "").Replace(" ", "");
                if (temp.Length > 0 && Regex.IsMatch(temp, @"^\d*$"))
                {
                    Index = int.Parse(temp);
                    break;
                }
            }
            #endregion

            #region Get NameSpace

            var NameSpaceRegex = new Regex("package (.*?);");
            foreach (var line in lines)
            {
                MatchCollection mc = NameSpaceRegex.Matches(line);
                if (mc != null && mc.Count > 0)
                {
                    NameSpace = mc[0].Groups[1].Value;
                    break;
                }
            }

            #endregion

            #region Append Message Start
            OpcodeText.Text = "";
            OpcodeText.AppendText("namespace " + NameSpace);
            OpcodeText.AppendText(Environment.NewLine);
            OpcodeText.AppendText("{");
            OpcodeText.AppendText(Environment.NewLine);
            #endregion

            #region Append Message Content

            var MessageRegex = new Regex(@"^message\s+(.*?)\s*//\s*(.*?)$");
            var MessageRegex2 = new Regex(@"^message\s+(.*?)\s*$");
            foreach (var line in lines)
            {
                MatchCollection mc = MessageRegex.Matches(line);
                if (mc == null || mc.Count == 0) mc = MessageRegex2.Matches(line);
                if (mc == null || mc.Count == 0) continue;
                var msgclass = mc[0].Groups[1].Value;
                var typeclass = mc[0].Groups[2].Value;
                OpcodeText.AppendText("    [Message(" + OpcodeClass + "." + msgclass + ")]");
                OpcodeText.AppendText(Environment.NewLine);
                if (!string.IsNullOrWhiteSpace(typeclass))
                {
                    OpcodeText.AppendText("    public partial class " + msgclass + " : " + typeclass + " { }");
                }
                else
                {
                    OpcodeText.AppendText("    public partial class " + msgclass + " { }");
                }
                OpcodeText.AppendText(Environment.NewLine);
                OpcodeText.AppendText(Environment.NewLine);
            }

            #endregion

            #region Append Message End
            OpcodeText.AppendText("}");
            OpcodeText.AppendText(Environment.NewLine);
            #endregion

            #region Append Opcode Start
            OpcodeText.AppendText("namespace " + NameSpace);
            OpcodeText.AppendText(Environment.NewLine);
            OpcodeText.AppendText("{");
            OpcodeText.AppendText(Environment.NewLine);
            OpcodeText.AppendText("    public static partial class " + OpcodeClass);
            OpcodeText.AppendText(Environment.NewLine);
            OpcodeText.AppendText("    {");
            OpcodeText.AppendText(Environment.NewLine);
            #endregion

            #region Append Opcode Content

            foreach (var line in lines)
            {
                MatchCollection mc = MessageRegex.Matches(line);
                if (mc == null || mc.Count == 0) mc = MessageRegex2.Matches(line);
                if (mc == null || mc.Count == 0) continue;
                var msgclass = mc[0].Groups[1].Value;
                Index += 1;
                OpcodeText.AppendText("        public const ushort " + msgclass + " = " + Index + ";");
                OpcodeText.AppendText(Environment.NewLine);
            }

            #endregion

            #region Append Opcode End
            OpcodeText.AppendText("    }");
            OpcodeText.AppendText(Environment.NewLine);
            OpcodeText.AppendText("}");
            OpcodeText.AppendText(Environment.NewLine);
            #endregion
        }

        #region Log

        /// <summary>
        /// Log输出
        /// </summary>
        /// <param name="msg"></param>
        public void Log(object msg)
        {
            LogText.AppendText(msg.ToString());
            LogText.AppendText(Environment.NewLine);
            LogText.ScrollToVerticalOffset(LogText.ExtentHeight);
        }

        #endregion

        #endregion

        #region BraceFoldingStrategy

        /// <summary>
        /// Allows producing foldings from a document based on braces.
        /// </summary>
        public class BraceFoldingStrategy
        {
            /// <summary>
            /// Gets/Sets the opening brace. The default value is '{'.
            /// </summary>
            public char OpeningBrace { get; set; }

            /// <summary>
            /// Gets/Sets the closing brace. The default value is '}'.
            /// </summary>
            public char ClosingBrace { get; set; }

            /// <summary>
            /// Creates a new BraceFoldingStrategy.
            /// </summary>
            public BraceFoldingStrategy()
            {
                this.OpeningBrace = '{';
                this.ClosingBrace = '}';
            }

            public void UpdateFoldings(FoldingManager manager, TextDocument document)
            {
                int firstErrorOffset;
                IEnumerable<NewFolding> newFoldings = CreateNewFoldings(document, out firstErrorOffset);
                manager.UpdateFoldings(newFoldings, firstErrorOffset);
            }

            /// <summary>
            /// Create <see cref="NewFolding"/>s for the specified document.
            /// </summary>
            public IEnumerable<NewFolding> CreateNewFoldings(TextDocument document, out int firstErrorOffset)
            {
                firstErrorOffset = -1;
                return CreateNewFoldings(document);
            }

            /// <summary>
            /// Create <see cref="NewFolding"/>s for the specified document.
            /// </summary>
            public IEnumerable<NewFolding> CreateNewFoldings(ITextSource document)
            {
                List<NewFolding> newFoldings = new List<NewFolding>();

                Stack<int> startOffsets = new Stack<int>();
                int lastNewLineOffset = 0;
                char openingBrace = this.OpeningBrace;
                char closingBrace = this.ClosingBrace;
                for (int i = 0; i < document.TextLength; i++)
                {
                    char c = document.GetCharAt(i);
                    if (c == openingBrace)
                    {
                        startOffsets.Push(i);
                    }
                    else if (c == closingBrace && startOffsets.Count > 0)
                    {
                        int startOffset = startOffsets.Pop();
                        // don't fold if opening and closing brace are on the same line
                        if (startOffset < lastNewLineOffset)
                        {
                            newFoldings.Add(new NewFolding(startOffset, i + 1));
                        }
                    }
                    else if (c == '\n' || c == '\r')
                    {
                        lastNewLineOffset = i + 1;
                    }
                }
                newFoldings.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));
                return newFoldings;
            }
        }

        #endregion

        #region CompletionData

        /// <summary>
        /// Implements AvalonEdit ICompletionData interface to provide the entries in the completion drop down.
        /// </summary>
        public class CompletionData : ICompletionData
        {
            public CompletionData(string text, string Description = null)
            {
                this.Text = text;
                this.Description = Description;
            }

            public System.Windows.Media.ImageSource Image
            {
                get { return null; }
            }

            public string Text { get; private set; }

            public object Description { get; private set; }

            public object Content
            {
                get { return this.Text; }
            }

            public double Priority { get { return 0; } }

            public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
            {
                textArea.Document.Replace(completionSegment, this.Text);
            }
        }

        #endregion
    }
}
