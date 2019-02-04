﻿using System;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.Win32;
using System.IO;

namespace BetterOverwatch
{
    public class ContextMenu : Form
    {
        public MenuItem currentGame = new MenuItem("Last Game");
        public NotifyIcon trayIcon = new NotifyIcon();
        public System.Windows.Forms.ContextMenu trayMenu = new System.Windows.Forms.ContextMenu();
        private RegistryKey reg = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);

        public ContextMenu()
        {
            try
            {
                currentGame.MenuItems.Add("Time elapsed: --:--");
                currentGame.MenuItems.Add("Skill rating: ----");
                currentGame.MenuItems.Add("Map: ----");
                currentGame.MenuItems.Add("Teams rating: ---- | ----");
                currentGame.MenuItems.Add("Last Hero: ----");
                currentGame.MenuItems.Add("Final score: - | -");

                for(int i = 0; i < currentGame.MenuItems.Count; i++) { currentGame.MenuItems[i].Enabled = false; }
                MenuItem debugTools = new MenuItem("Debug tools");
                debugTools.MenuItems.Add("Open logs", OpenLogs);
                debugTools.MenuItems.Add(currentGame);
                
                trayMenu.MenuItems.Add("Better Overwatch v" + Vars.initalize.Version);
                trayMenu.MenuItems.Add("Login", OpenMatchHistory);
                trayMenu.MenuItems.Add("-");
                trayMenu.MenuItems.Add("Upload screenshot of player list", ToggleUpload);
                trayMenu.MenuItems.Add("Start when Windows starts", ToggleWindows);
                trayMenu.MenuItems.Add("Play audio on success", ToggleAudio);
                trayMenu.MenuItems.Add(debugTools);
                trayMenu.MenuItems.Add("-");
                trayMenu.MenuItems.Add("Exit", OnExit);
                trayMenu.MenuItems[0].Enabled = false;

                if (Vars.settings.uploadScreenshot)
                {
                    trayMenu.MenuItems[3].Checked = true;
                }
                if (Vars.settings.startWithWindows)
                {
                    trayMenu.MenuItems[4].Checked = true;
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                    {
                        if (key != null)
                        {
                            key.SetValue("OverwatchTracker", "\"" + Application.ExecutablePath.ToString() + "\"");
                        }
                    }
                }
                if (Vars.settings.playAudioOnSuccess)
                {
                    trayMenu.MenuItems[5].Checked = true;
                }

                trayIcon.Text = "Waiting for Overwatch, idle...";
                trayIcon.Icon = Properties.Resources.Idle;
                trayIcon.ContextMenu = trayMenu;
                trayIcon.Visible = true;
                trayIcon.DoubleClick += new EventHandler(OpenMatchHistory);
            }catch { }
        }
        protected override void OnLoad(EventArgs e)
        {
            Visible = false;
            ShowInTaskbar = false;

            base.OnLoad(e);
        }
        public void TrayPopup(string title, string text, int timeout)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<string, string, int>(TrayPopup), new object[] { title, text, timeout });
                return;
            }
            Functions.DebugMessage(title);
            trayIcon.ShowBalloonTip(timeout, title, text, ToolTipIcon.None);
        }
        private void OnExit(object sender, EventArgs e)
        {
            Application.Exit();
        }
        private void OpenLogs(object sender, EventArgs e)
        {
            Process.Start(Path.Combine(Vars.configPath, "debug.log"));
        }
        private void ToggleUpload(object sender, EventArgs e)
        {
            if (trayMenu.MenuItems[3].Checked)
            {
                trayMenu.MenuItems[3].Checked = false;
            }
            else
            {
                trayMenu.MenuItems[3].Checked = true;
            }
            Vars.settings.uploadScreenshot = trayMenu.MenuItems[3].Checked;
            Settings.Save();
        }
        private void ToggleWindows(object sender, EventArgs e)
        {
            if (trayMenu.MenuItems[4].Checked)
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        key.DeleteValue("OverwatchTracker");
                    }
                }
                trayMenu.MenuItems[4].Checked = false;
            }
            else
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        key.SetValue("OverwatchTracker", "\"" + Application.ExecutablePath.ToString() + "\"");
                    }
                }
                trayMenu.MenuItems[4].Checked = true;
            }
            Vars.settings.startWithWindows = trayMenu.MenuItems[4].Checked;
            Settings.Save();
        }
        private void ToggleAudio(object sender, EventArgs e)
        {
            if (trayMenu.MenuItems[5].Checked)
            {
                trayMenu.MenuItems[5].Checked = false;
            }
            else
            {
                trayMenu.MenuItems[5].Checked = true;
            }
            Vars.settings.playAudioOnSuccess = trayMenu.MenuItems[5].Checked;
            Settings.Save();
        }
        private void OpenMatchHistory(object sender, EventArgs e)
        {
            if(Vars.settings.publicToken.Equals(String.Empty))
            {
                Server.FetchTokens();
            }
            if (Vars.settings.publicToken.Equals(String.Empty))
            {
                // no publicToken fetched, popup browser so user can create one
                Process.Start("http://" + Vars.initalize.Host + "/new-account/?privateToken=" + Vars.settings.privateToken);
            }
            else
            {
                // publicToken successfully fetched, login instead with their privateToken
                Process.Start("http://" + Vars.initalize.Host + "/" + Vars.settings.publicToken + "?login=" + Vars.settings.privateToken);
            }
        }
        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                trayIcon.Dispose();
            }

            base.Dispose(isDisposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // ContextMenu
            // 
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Name = "ContextMenu";
            this.TopMost = true;
            this.ResumeLayout(false);

        }
    }
}