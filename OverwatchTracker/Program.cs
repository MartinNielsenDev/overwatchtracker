﻿using System;
using System.Reflection;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Security.Principal;
using System.Runtime.InteropServices;
using DesktopDuplication;

namespace OverwatchTracker
{
    class Program
    {
        [DllImport("user32.dll")]
        public static extern uint SendMessage(IntPtr hWnd, uint msg, uint wParam, uint lParam);
        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);
        public static ContextMenu contextMenu;
        private static AdminPromptForm adminPromptForm;
        public static Thread uploaderThread;
        private static Mutex mutex = new Mutex(true, "74bf6260-c133-4d69-ad9c-efc607887c97");
        private static DesktopDuplicator desktopDuplicator;
        [STAThread]
        static void Main()
        {
            Functions.DebugMessage("Starting Overwatch Tracker version " + Vars.version);

            if (!mutex.WaitOne(TimeSpan.Zero, true))
            {
                Functions.DebugMessage("Overwatch Tracker is already running...");
                return;
            }
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler((s, assembly) =>
            {
                if (assembly.Name.Contains("Newtonsoft.Json,"))
                {
                    return LoadAssembly("OverwatchTracker.dlls.Newtonsoft.Json.dll");
                }
                if (assembly.Name.Contains("AForge.Imaging,"))
                {
                    return LoadAssembly("OverwatchTracker.dlls.AForge.Imaging.dll");
                }
                if (assembly.Name.Contains("SharpDX.Direct3D11,"))
                {
                    return LoadAssembly("OverwatchTracker.dlls.SharpDX.Direct3D11.dll");
                }
                if (assembly.Name.Contains("SharpDX.DXGI,"))
                {
                    return LoadAssembly("OverwatchTracker.dlls.SharpDX.DXGI.dll");
                }
                if (assembly.Name.Contains("SharpDX,"))
                {
                    return LoadAssembly("OverwatchTracker.dlls.SharpDX.dll");
                }
                return null;
            });
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
            try
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    Vars.isAdmin = new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
                }
                if (!Server.NewestVersionV2(firstRun: true)) return;
                Server.FetchBlizzardAppOffset();
                Directory.CreateDirectory(Vars.configPath);
                Vars.settings = new Settings();
                Settings.Load();
                Settings.Save();
                Vars.gameData = new GameData();
                adminPromptForm = new AdminPromptForm();
                contextMenu = new ContextMenu();

                if (!Settings.VerifyUser()) return;

                Thread captureDesktopThread = new Thread(CaptureDesktop)
                {
                    IsBackground = true
                };
                captureDesktopThread.Start();
                Server.autoUpdaterTimer.Start();
                Functions.DebugMessage("> Success - Overwatch Tracker started without fail");
            }
            catch (Exception e)
            {
                MessageBox.Show("startUp error: " + e.ToString() + "\r\n\r\nReport this on the discord server");
                Application.Exit();
            }
            if (!Vars.isAdmin)
            {
                adminPromptForm.Show();
            }
            Application.Run(contextMenu);
        }
        public static void CaptureDesktop()
        {
            Vars.statsTimer.Restart();
            try
            {
                desktopDuplicator = new DesktopDuplicator(0);
                Vars.frameTimer.Start();
            }
            catch (Exception e)
            {
                Functions.DebugMessage("Could not initialize desktopDuplication API - shutting down:" + e.ToString());
                Environment.Exit(0);
            }
            while (true)
            {
                if (!Functions.activeWindowTitle().Equals("Overwatch"))
                {
                    if (!Vars.overwatchRunning)
                    {
                        if (Functions.IsProcessOpen("Overwatch"))
                        {
                            contextMenu.trayIcon.Text = "Visit play menu to update your skill rating";
                            contextMenu.trayIcon.Icon = Properties.Resources.IconVisitMenu;
                            Vars.overwatchRunning = true;
                        }
                        else
                        {
                            Server.AutoUpdater();
                        }
                    }
                    else
                    {
                        if (!Functions.IsProcessOpen("Overwatch"))
                        {
                            contextMenu.trayIcon.Text = "Waiting for Overwatch, idle...";
                            contextMenu.trayIcon.Icon = Properties.Resources.Idle;
                            Vars.overwatchRunning = false;
                        }
                    }
                    Thread.Sleep(500);
                }
                else
                {
                    Thread.Sleep(50);
                    contextMenu.currentGame.MenuItems[0].Text = "Game elapsed: " + Functions.SecondsToMinutes((int)Math.Floor(Convert.ToDouble(Vars.gameTimer.ElapsedMilliseconds / 1000)));

                    if (Vars.frameTimer.ElapsedMilliseconds >= Vars.loopDelay)
                    {
                        DesktopFrame frame = null;

                        try
                        {
                            frame = desktopDuplicator.GetLatestFrame();
                        }
                        catch
                        {
                            desktopDuplicator.Reinitialize();
                            continue;
                        }
                        if (frame != null)
                        {
                            try
                            {
                                Protocols.CheckMainMenu(frame.DesktopImage);

                                if (Vars.gameData.gameState != Vars.STATUS_INGAME)
                                {
                                    string quickPlayText = Functions.BitmapToText(frame.DesktopImage, 476, 644, 80, 40, contrastFirst: false, radius: 140, network: 0, invertColors: true);

                                    if (Functions.CompareStrings(quickPlayText, "PLRY") >= 70)
                                    {
                                        Protocols.CheckPlayMenu(frame.DesktopImage);
                                    }
                                }

                                if (Vars.gameData.gameState == Vars.STATUS_IDLE || Vars.gameData.gameState == Vars.STATUS_FINISHED || Vars.gameData.gameState == Vars.STATUS_WAITFORUPLOAD)
                                {
                                    Protocols.CheckCompetitiveGameEntered(frame.DesktopImage);
                                }

                                if (Vars.gameData.gameState == Vars.STATUS_INGAME)
                                {
                                    Protocols.CheckMap(frame.DesktopImage);
                                    Protocols.CheckTeamsSkillRating(frame.DesktopImage);

                                    if (!Vars.gameData.team1sr.Equals(String.Empty) &&
                                        !Vars.gameData.team2sr.Equals(String.Empty) &&
                                        !Vars.gameData.map.Equals(String.Empty) ||
                                        !Vars.gameData.map.Equals(String.Empty) && Vars.getInfoTimeout.ElapsedMilliseconds >= 4000)
                                    {
                                        if (Vars.gameData.playerlistimage == null)
                                        {
                                            Thread.Sleep(2000);
                                            try
                                            {
                                                frame = desktopDuplicator.GetLatestFrame();
                                                Vars.gameData.playerlistimage = new Bitmap(Functions.CaptureRegion(frame.DesktopImage, 0, 110, 1920, 700));
                                            }
                                            catch { }

                                        }
                                        Vars.loopDelay = 500;
                                        Vars.gameData.gameState = Vars.STATUS_RECORDING;
                                        contextMenu.trayIcon.Text = "Recording... visit the main menu after the game";
                                        contextMenu.trayIcon.Icon = Properties.Resources.IconRecord;
                                        Vars.statsTimer.Restart();
                                        Vars.getInfoTimeout.Stop();
                                    }
                                    else if (Vars.getInfoTimeout.ElapsedMilliseconds >= 4500)
                                    {
                                        Vars.roundTimer.Stop();
                                        Vars.gameTimer.Stop();
                                        Vars.getInfoTimeout.Stop();
                                        Vars.gameData.gameState = Vars.STATUS_IDLE;
                                        Functions.DebugMessage("Failed to find game");
                                    }
                                }

                                if (Vars.gameData.gameState == Vars.STATUS_RECORDING)
                                {
                                    if (GetAsyncKeyState(0x09) < 0)
                                    {
                                        Protocols.CheckHeroPlayed(frame.DesktopImage);
                                        if (Vars.roundTimer.ElapsedMilliseconds >= Functions.GetTimeDeduction(getNextDeduction: true))
                                        {
                                            Protocols.CheckStats(frame.DesktopImage);
                                        }
                                    }
                                    else if (Vars.roundTimer.ElapsedMilliseconds >= Functions.GetTimeDeduction(getNextDeduction: true))
                                    {
                                        Protocols.CheckRoundCompleted(frame.DesktopImage);
                                    }
                                    Protocols.CheckMainMenu(frame.DesktopImage);
                                    Protocols.CheckFinalScore(frame.DesktopImage);
                                }

                                if (Vars.gameData.gameState == Vars.STATUS_FINISHED)
                                {
                                    Protocols.CheckGameScore(frame.DesktopImage);
                                }
                            }
                            catch (Exception e)
                            {
                                Functions.DebugMessage("Main Exception: " + e.ToString());
                                Thread.Sleep(500);
                            }
                        }

                        Vars.frameTimer.Restart();
                    }
                }
            }
        }
        private static Assembly LoadAssembly(string resource)
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource))
            {
                byte[] assemblyData = new byte[stream.Length];

                stream.Read(assemblyData, 0, assemblyData.Length);
                return Assembly.Load(assemblyData);
            }
        }
    }
}
