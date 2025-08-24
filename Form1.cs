using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Net.Http.Headers;
using System.Linq;

namespace AIClipboardNotifier
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public class MainForm : Form
    {
        private const int WM_CLIPBOARDUPDATE = 0x031D;
        //private bool isMonitoring = true;

        //private bool isTranslateEnabled = false;
        //private bool isFormaterEnabled = true;
        //private bool isAIAnythingEnabled = false;

        //private bool useOllama = false;
        //private bool useOpenWebui = false;
        //private bool useOpenAI = false;
        
        private NotifyIcon trayIcon;
        private Icon enableIcon;
        private Icon stopIcon;
        private ContextMenuStrip trayMenu;
        private ToolStripMenuItem statusMenuItem;
        private ToolStripMenuItem translateMenuItem;
        private ToolStripMenuItem formaterMenuItem;
        private ToolStripMenuItem aiAnythingMenuItem;
        private ToolStripMenuItem promptListMenuItem;
        private ToolStripMenuItem modelListMenuItem;
        private const string AppName = "AIClipboardNotifier";
        private ToolStripMenuItem autoStartMenuItem;

        //private string endpoint_ollama = "http://localhost:11434/api/generate";//ollama
        //private string endpoint_openwebui = "http://localhost:3000/ollama/api/chat";//openwebui
        //private string endpoint_openai = "http://localhost:3000/api/chat/completions";//openai
        //private string apiKey_openwebui = "sk_";//open-webui
        //private string apiKey_openai = "sk_";//openai-like
        //private string transLang = "chinese";//translation
        //private string prompt = "";
        //private int promptIndex = -1;
        //private double timeout = 30;
        //private string[] promptList = { };
        //public string[] promptTitle = { };
        //private string[] modelList = { };
        //private string[] modelTitle = { };
        //private string modelForTranslate = "";
        //private int modelIndex = -1;
        //private string picPath = "";
        //private string imageBase64 = "";

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

        public MainForm()
        {
            ConfigLoader.LoadConfig(); // 将当前实例传递给 LoadConfig 方法
            InitializeTrayIcon();
            InitializeMainForm();
            AddClipboardFormatListener(this.Handle);
        }

        private void InitializeMainForm()
        {
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new Size(0, 0);
        }

        private void InitializeTrayIcon()
        {
            using (MemoryStream stream = new MemoryStream(AIClipboardNotifier.Properties.Resources.enabledIcon))
            {
                enableIcon = new Icon(stream); 
            }

            using (MemoryStream stream = new MemoryStream(AIClipboardNotifier.Properties.Resources.stopedIcon))
            {
                stopIcon = new Icon(stream);
            }

            trayIcon = new NotifyIcon
            {
                Text = "Clipboard Notifier",
                Icon = enableIcon,
                Visible = true
            };

            trayMenu = new ContextMenuStrip();

            //Stop
            statusMenuItem = new ToolStripMenuItem("Stop", null, ToggleMonitoring);

            //xml/json format
            formaterMenuItem = new ToolStripMenuItem("xml/json format", null, ToggleFormater)
            {
                CheckOnClick = true,
                Checked = ConfigLoader.isFormaterEnabled
            };

            //AI Translate
            translateMenuItem = new ToolStripMenuItem("AI Translate", null, ToggleTranslate)
            {
                CheckOnClick = true,
                Checked = ConfigLoader.isTranslateEnabled
            };

            //AI Anything
            aiAnythingMenuItem = new ToolStripMenuItem("AI Anything", null, ToggleAIAnything)
            {
                CheckOnClick = true,
                Checked = ConfigLoader.isAIAnythingEnabled
            };

            // Prompt List
            if (ConfigLoader.promptList.Length != 0)
            {
                promptListMenuItem = new ToolStripMenuItem("Prompt List");
                UpdatePromptListMenuItems();
            }
            else
            {
                promptListMenuItem = new ToolStripMenuItem("Prompt List (Empty)");
            }

            if (ConfigLoader.modelList.Length != 0)
            {
                modelListMenuItem = new ToolStripMenuItem("Model List");
                UpdateModelListMenuItems();
            }
            else
            {
                modelListMenuItem = new ToolStripMenuItem("Model List (Empty)");
            }

            //Exit
            var exitMenuItem = new ToolStripMenuItem("Exit", null, OnExit);

            trayMenu.Items.Add(statusMenuItem);
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add(formaterMenuItem);
            trayMenu.Items.Add(translateMenuItem);
            trayMenu.Items.Add(aiAnythingMenuItem);
            trayMenu.Items.Add(promptListMenuItem);
            trayMenu.Items.Add(modelListMenuItem);
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add(exitMenuItem);

            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.DoubleClick += ToggleMonitoring;

            autoStartMenuItem = new ToolStripMenuItem("AutoStart", null, ToggleAutoStart)
            {
                CheckOnClick = true,
                Checked = IsAutoStartEnabled()
            };

            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add(autoStartMenuItem);
        }

        private void UpdatePromptListMenuItems()
        {
            promptListMenuItem.DropDownItems.Clear();
            for (int i = 0; i < ConfigLoader.promptTitle.Length; i++)
            {
                var item = new ToolStripMenuItem(ConfigLoader.promptTitle[i])
                {
                    CheckOnClick = false,
                    Checked = (i == ConfigLoader.promptIndex)
                };
                int index = i;
                item.Click += (s, e) =>
                {
                    if (item.Checked)
                    {
                        item.Checked = false;
                        ConfigLoader.promptIndex = -1;
                        aiAnythingMenuItem.Checked = false;
                        ConfigLoader.isAIAnythingEnabled = false;
                    }
                    else
                    {
                        foreach (ToolStripMenuItem menuItem in promptListMenuItem.DropDownItems)
                        {
                            menuItem.Checked = false;
                        }
                        item.Checked = true;
                        ConfigLoader.promptIndex = index;
                        aiAnythingMenuItem.Checked = true;
                        ConfigLoader.isAIAnythingEnabled = true;
                        if (ConfigLoader.isTranslateEnabled)
                        {
                            translateMenuItem.Checked = false;
                            ConfigLoader.isTranslateEnabled = false;
                        }
                    }
                };
                promptListMenuItem.DropDownItems.Add(item);
            }
        }

        private void UpdateModelListMenuItems()
        {
            modelListMenuItem.DropDownItems.Clear();
            for (int i = 0; i < ConfigLoader.modelTitle.Length; i++)
            {
                var item = new ToolStripMenuItem(ConfigLoader.modelTitle[i])
                {
                    CheckOnClick = false,
                    Checked = (i == ConfigLoader.modelIndex)
                };
                int index = i;
                item.Click += (s, e) =>
                {
                    foreach (ToolStripMenuItem menuItem in modelListMenuItem.DropDownItems)
                    {
                        menuItem.Checked = false;
                    }
                    item.Checked = true;
                    ConfigLoader.modelIndex = index;
                };
                modelListMenuItem.DropDownItems.Add(item);
            }
        }

        // Formater
        private void ToggleFormater(object sender, EventArgs e)
        {
            ConfigLoader.isFormaterEnabled = formaterMenuItem.Checked;
        }



        private void ToggleTranslate(object sender, EventArgs e)
        {
            if(!ConfigLoader.checkAIConfig()){
                MessageBox.Show("AI is not configured. Please set ollama or open-webui enable field to true in config.txt");
                translateMenuItem.Checked = false;
                return;
            }

            ConfigLoader.isTranslateEnabled = translateMenuItem.Checked;
            if (translateMenuItem.Checked)
            {
                aiAnythingMenuItem.Checked = false;
                ConfigLoader.isAIAnythingEnabled = false;
            }
        }

        private void ToggleAIAnything(object sender, EventArgs e)
        {
            if (ConfigLoader.promptIndex == -1)
            {
                MessageBox.Show("Please select one prompt from the Prompt List first, then enable AI Anything.");
                aiAnythingMenuItem.Checked = false;
                return;
            }

            if (!ConfigLoader.checkAIConfig())
            {
                MessageBox.Show("AI is not configured. Please set ollama or open-webui enable field to true in config.txt");
                aiAnythingMenuItem.Checked = false;
                return;
            }

            ConfigLoader.isAIAnythingEnabled = aiAnythingMenuItem.Checked;
            if (aiAnythingMenuItem.Checked)
            {
                translateMenuItem.Checked = false;
                ConfigLoader.isTranslateEnabled = false;
            }
        }

        private bool IsAutoStartEnabled()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", false))
            {
                return key.GetValue(AppName) != null;
            }
        }

        private void ToggleAutoStart(object sender, EventArgs e)
        {
            bool enable = autoStartMenuItem.Checked;
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", true))
            {
                if (enable)
                {
                    string exePath = $"\"{Application.ExecutablePath}\"";
                    key.SetValue(AppName, exePath);
                }
                else
                {
                    key.DeleteValue(AppName, false);
                }
            }
        }

        private void ToggleMonitoring(object sender, EventArgs e)
        {
            ConfigLoader.isMonitoring = !ConfigLoader.isMonitoring;

            if (ConfigLoader.isMonitoring)
            {
                AddClipboardFormatListener(this.Handle);
                statusMenuItem.Text = "Stop";
                trayIcon.Icon = enableIcon;
                ConfigLoader.LoadConfig();
                UpdatePromptListMenuItems();
                UpdateModelListMenuItems();
            }
            else
            {
                RemoveClipboardFormatListener(this.Handle);
                statusMenuItem.Text = "Start";
                trayIcon.Icon = stopIcon;
            }
        }

        private void OnExit(object sender, EventArgs e)
        {
            trayIcon.Visible = false;
            Application.Exit();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_CLIPBOARDUPDATE && ConfigLoader.isMonitoring)
            {
                ShowClipboardContent();
            }
            base.WndProc(ref m);
        }

        private async void ShowClipboardContent()
        {
            try
            {
                AIProcess.picPath = "";
                if (Clipboard.ContainsImage())
                {
                    Image img = Clipboard.GetImage();
                    string fileName = "clipboard_image.png";
                    AIProcess.picPath = Path.Combine(Environment.CurrentDirectory, fileName);
                    img.Save(AIProcess.picPath, System.Drawing.Imaging.ImageFormat.Png);
                    AIProcess.imageBase64 = Convert.ToBase64String(File.ReadAllBytes(AIProcess.picPath));
                }

                if (Clipboard.ContainsFileDropList())
                {
                    var files = Clipboard.GetFileDropList();
                    List<string> imagePaths = new List<string>();
                    foreach (string file in files)
                    {
                        string ext = Path.GetExtension(file).ToLower();
                        if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".gif")
                        {
                            imagePaths.Add(file);
                        }
                    }
                    if (imagePaths.Count > 0)
                    {
                        AIProcess.picPath = imagePaths[0];
                        AIProcess.imageBase64 = Convert.ToBase64String(File.ReadAllBytes(AIProcess.picPath));
                    }
                }

                if (Clipboard.ContainsText() || AIProcess.picPath.Length != 0)
                {
                    string text = Clipboard.GetText();
                    if (AIProcess.picPath.Length != 0)
                    {
                        text = AIProcess.picPath;
                    }

                    if (ConfigLoader.isFormaterEnabled && AIProcess.picPath.Length ==0)
                    {
                        string formattedText = XMLFormater.FormatText(text);
                        if (formattedText != text && formattedText != "null")
                        {
                            RemoveClipboardFormatListener(this.Handle);

                            Clipboard.SetText(formattedText);
                            new ClipboardPopup(formattedText, false).Show();

                            AddClipboardFormatListener(this.Handle);
                            return;
                        }
                    }

                    if (ConfigLoader.isTranslateEnabled || ConfigLoader.isAIAnythingEnabled)
                    {
                        new ClipboardPopup(text, false).Show();

                        var timeoutDuration = TimeSpan.FromSeconds(ConfigLoader.timeout);
                        var processTask = AIProcess.ProcessRequestAsync(text);//The main process of AI request
                        var timeoutTask = Task.Delay(timeoutDuration);

                        var completedTask = await Task.WhenAny(processTask, timeoutTask);
                        string translatedText = string.Empty;

                        if (completedTask == timeoutTask)
                        {
                            translatedText = "Ollama timeout, maybe model is loading, please try again";
                        }
                        else if (processTask.IsCompleted && !string.IsNullOrEmpty(processTask.Result))
                        {
                            translatedText = processTask.Result;
                        } 
                        else
                        {
                            translatedText = "Ollama request failed";
                        }

                        RemoveClipboardFormatListener(this.Handle);
                        Clipboard.SetText(translatedText);
                        new ClipboardPopup(translatedText, true).Show();
                        AddClipboardFormatListener(this.Handle);

                        return;
                    }

                    new ClipboardPopup(text, false).Show();
                }
            }
            catch (Exception ex)
            {
                AddClipboardFormatListener(this.Handle);
                new ClipboardPopup("Clipboard error:" + ex.ToString(), false).Show();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                trayIcon?.Dispose();
                trayMenu?.Dispose();
            }
            RemoveClipboardFormatListener(this.Handle);
            base.Dispose(disposing);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
            base.OnFormClosing(e);
        }
    }

    public class ClipboardPopup : Form
    {
        private readonly Timer _fadeInTimer;
        private readonly Timer _fadeOutTimer;
        private readonly string _text;
        private readonly bool _isTranslation;
        private readonly Timer _stayTimer;

        public ClipboardPopup(string text, bool isTranslation = false)
        {
            _text = text;
            _isTranslation = isTranslation;
            InitializePopup();

            _fadeInTimer = new Timer { Interval = 20 };
            _fadeInTimer.Tick += FadeIn;
            _fadeInTimer.Start();

            _stayTimer = new Timer { Interval = 3000 }; // stay 3 seconds
            _stayTimer.Tick += StartFadeOut;

            _fadeOutTimer = new Timer { Interval = 20 };
            _fadeOutTimer.Tick += FadeOut;
        }

        private void FadeIn(object sender, EventArgs e)
        {
            if (this.Opacity < 1)
            {
                this.Opacity += 0.05;
            }
            else
            {
                _fadeInTimer.Stop();
                _stayTimer.Start();
            }
        }

        private void StartFadeOut(object sender, EventArgs e)
        {
            _stayTimer.Stop();
            _fadeOutTimer.Start();
        }

        private void InitializePopup()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.StartPosition = FormStartPosition.Manual;
            this.Size = new Size(300, 150);
            this.Opacity = 0; 
            this.BackColor = _isTranslation ? Color.LightGreen : Color.LightGray;
            UpdatePosition();

            var lbl = new Label
            {
                Text = _text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Arial", 10)
            };
            Controls.Add(lbl);
        }

        private void UpdatePosition()
        {
            var screen = Screen.PrimaryScreen.WorkingArea;
            this.Location = new Point(screen.Right - Width - 10, screen.Bottom - Height - 10);
        }

        private void FadeOut(object sender, EventArgs e)
        {
            if (this.Opacity > 0)
            {
                this.Opacity -= 0.05;
            }
            else
            {
                _fadeOutTimer.Stop();
                this.Close();
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
                cp.ExStyle |= 0x00000008; // WS_EX_TOPMOST
                return cp;
            }
        }
    }
}