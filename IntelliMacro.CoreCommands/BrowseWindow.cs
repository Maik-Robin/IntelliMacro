using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using IntelliMacro.Runtime;
using System.Runtime.InteropServices;
using System.IO;

namespace IntelliMacro.CoreCommands
{
    partial class BrowseWindow : Form, IMacroEvent
    {
        static BrowseWindow instance;

        public static BrowseWindow Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new BrowseWindow();
                }
                return instance;
            }
        }

        public BrowseWindow()
        {
            InitializeComponent();
            webBrowser.CanGoBackChanged += new EventHandler(webBrowser_CanGoBackChanged);
            webBrowser.CanGoForwardChanged += new EventHandler(webBrowser_CanGoForwardChanged);
        }

        void webBrowser_CanGoForwardChanged(object sender, EventArgs e)
        {
            forwardButton.Enabled = webBrowser.CanGoForward;
        }

        void webBrowser_CanGoBackChanged(object sender, EventArgs e)
        {
            backButton.Enabled = webBrowser.CanGoBack;
        }

        internal void BrowseToURL(string url)
        {
            Uri uri = webBrowser.Url;
            try
            {
                try
                {
                    uri = new Uri(new Uri(urlText.Text), url);
                }
                catch (UriFormatException)
                {
                    uri = new Uri(url);
                }
            }
            catch (UriFormatException)
            {
                try
                {
                    uri = new Uri("http://" + url);
                }
                catch (UriFormatException ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            urlText.Text = uri.ToString();
            webBrowser.Url = uri;
        }

        private void goButton_Click(object sender, EventArgs e)
        {
            BrowseToURL(urlText.Text);
        }

        private void backButton_Click(object sender, EventArgs e)
        {
            BrowseRecordingListener.Instance.AddEvent("BrowseNavigate", "Back");
            webBrowser.GoBack();
        }

        private void forwardButton_Click(object sender, EventArgs e)
        {
            BrowseRecordingListener.Instance.AddEvent("BrowseNavigate", "Forward");
            webBrowser.GoForward();
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            BrowseRecordingListener.Instance.AddEvent("BrowseNavigate", "Stop");
            webBrowser.Stop();
        }

        private void reloadButton_Click(object sender, EventArgs e)
        {
            BrowseRecordingListener.Instance.AddEvent("BrowseNavigate", "Reload");
            webBrowser.Refresh(WebBrowserRefreshOption.Completely);
        }

        public event EventHandler Occurred;
        private bool jsLoaded = false;

        public bool HasOccurred { get { return !webBrowser.IsBusy; } }

        public void ClearOccurred() { }

        private void webBrowser_LocationChanged(object sender, EventArgs e)
        {
            urlText.Text = webBrowser.Url.ToString();
        }

        private void webBrowser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            jsLoaded = false;
            if (Occurred != null)
                Occurred(sender, e);
            urlText.Text = e.Url.ToString();
            if (clickActButton.Checked)
            {
                ClickAndAct(true);
            }
        }

        private void ClickAndAct(bool activate)
        {
            if (activate)
            {
                ExecuteJavaScript("IntelliMacroBrowserScripting_ClickNActActivate", true);
            }
            else
            {
                ExecuteJavaScript("IntelliMacroBrowserScripting_ClickNActActivate", false);
            }
        }

        private void BrowseWindow_FormClosed(object sender, FormClosedEventArgs e)
        {
            instance = null;
        }

        private void urlText_Enter(object sender, EventArgs e)
        {
            System.Windows.Forms.Timer t = new System.Windows.Forms.Timer();
            t.Tick += delegate { t.Enabled = false; urlText.SelectAll(); };
            t.Interval = 100;
            t.Enabled = true;
        }

        private void urlText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Return)
            {
                goButton_Click(null, null);
            }
        }

        private void clickActButton_Click(object sender, EventArgs e)
        {
            ClickAndAct(clickActButton.Checked);
        }

        private void webBrowser_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            if (e.TargetFrameName == "")
            {
                urlText.Text = e.Url.ToString();
                BrowseRecordingListener.Instance.AddEvent("BrowseFollowLink", urlText.Text);
            }
        }

        internal string ExecuteJavaScript(string method, params object[] args)
        {
            if (!jsLoaded)
            {
                webBrowser.ObjectForScripting = new BrowserScriptingObject(this);
                Stream s = typeof(BrowseWindow).Assembly.GetManifestResourceStream("IntelliMacro.CoreCommands.BrowserScripting.js");
                webBrowser.Document.InvokeScript("eval", new object[] { new StreamReader(s, Encoding.ASCII).ReadToEnd() });
                s.Close();
                jsLoaded = true;
            }
            return "" + webBrowser.Document.InvokeScript(method, args);
        }

        internal void RecordFormElementAction(string formName, string elementName, string elementValue)
        {
            BrowseRecordingListener.Instance.AddEvent("BrowseFormElement", formName, elementName, elementValue);
            ExecuteJavaScript("IntelliMacroBrowserScripting_FormElementAction", formName, elementName, elementValue);
        }

        internal void Navigate(string direction)
        {
            switch (direction)
            {
                case "Back":
                    webBrowser.GoBack();
                    break;
                case "Forward":
                    webBrowser.GoForward();
                    break;
                case "Reload":
                    webBrowser.Refresh();
                    break;
                case "Stop":
                    webBrowser.Stop();
                    break;
                default:
                    throw new MacroErrorException("Unsupported direction: " + direction);
            }
        }
    }

    [ComVisible(true)]
    public sealed class BrowserScriptingObject
    {
        BrowseWindow window;
        internal BrowserScriptingObject(BrowseWindow window)
        {
            this.window = window;
        }

        public void RecordFormElementAction(string formName, string elementName, string elementValue)
        {
            window.RecordFormElementAction(formName, elementName, elementValue);
        }
    }
}
