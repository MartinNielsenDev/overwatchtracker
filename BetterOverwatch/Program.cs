﻿using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;
using BetterOverwatch.DataObjects;
using BetterOverwatch.Forms;
using BetterOverwatch.Game;
using BetterOverwatch.Networking;
using BetterOverwatch.TensorFlow;

namespace BetterOverwatch
{
    internal class Program
    {
        public static AuthenticationForm autenticationForm;
        public static AdminPromptForm adminPromptForm;
        private static KeyboardHook keyboardHook;
        private static readonly Mutex mutex = new Mutex(true, "74bf6260-c133-4d69-ad9c-efc607887c97");

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetDllDirectory(string lpPathName);
        [STAThread]
        private static void Main()
        {
            AppData.initalize = new Initalize(
                "1.4.9",
                "betteroverwatch.com",
                "https://api.github.com/repos/MartinNielsenDev/OverwatchTracker/releases/latest");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            Functions.DebugMessage("Starting Better Overwatch version " + AppData.initalize.Version);

            if (!mutex.WaitOne(TimeSpan.Zero, true))
            {
                MessageBox.Show("Better Overwatch is already running\r\n\r\nYou must close other instances of Better Overwatch if you want to open this one", "Better Overwatch", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            AppDomain.CurrentDomain.AssemblyResolve += (s, assembly) =>
            {
                if (assembly.Name.Contains("NumSharp.Core,"))
                {
                    return LoadAssembly("BetterOverwatch.Resources.NumSharp.Core.dll");
                }
                if (assembly.Name.Contains("Google.Protobuf,"))
                {
                    return LoadAssembly("BetterOverwatch.Resources.Google.Protobuf.dll");
                }
                if (assembly.Name.Contains("tensorflow,"))
                {
                    return LoadAssembly("BetterOverwatch.Resources.tensorflow.dll");
                }
                if (assembly.Name.Contains("TensorFlow.NET,"))
                {
                    return LoadAssembly("BetterOverwatch.Resources.TensorFlow.NET.dll");
                }
                if (assembly.Name.Contains("Newtonsoft.Json,"))
                {
                    return LoadAssembly("BetterOverwatch.Resources.Newtonsoft.Json.dll");
                }
                if (assembly.Name.Contains("NeuralNetwork,"))
                {
                    return LoadAssembly("BetterOverwatch.Resources.NeuralNetwork.dll");
                }
                if (assembly.Name.Contains("AForge.Imaging,"))
                {
                    return LoadAssembly("BetterOverwatch.Resources.AForge.Imaging.dll");
                }
                if (assembly.Name.Contains("SharpDX.Direct3D11,"))
                {
                    return LoadAssembly("BetterOverwatch.Resources.SharpDX.Direct3D11.dll");
                }
                if (assembly.Name.Contains("SharpDX.DXGI,"))
                {
                    return LoadAssembly("BetterOverwatch.Resources.SharpDX.DXGI.dll");
                }
                if (assembly.Name.Contains("SharpDX,"))
                {
                    return LoadAssembly("BetterOverwatch.Resources.SharpDX.dll");
                }
                if (assembly.Name.Contains("System,"))
                {
                    return LoadAssembly("BetterOverwatch.Resources.System.dll");
                }
                if (assembly.Name.Contains("System.Drawing,"))
                {
                    return LoadAssembly("BetterOverwatch.Resources.System.Drawing.dll");
                }
                if (assembly.Name.Contains("System.Windows.Forms,"))
                {
                    return LoadAssembly("BetterOverwatch.Resources.System.Windows.Forms.dll");
                }
                if (assembly.Name.Contains("System.Xml,"))
                {
                    return LoadAssembly("BetterOverwatch.Resources.System.Xml.dll");
                }
                if (assembly.Name.Contains("System.Memory,"))
                {
                    return LoadAssembly("BetterOverwatch.Resources.System.Memory.dll");
                }
                if (assembly.Name.Contains("System.Numerics.Vectors,"))
                {
                    return LoadAssembly("BetterOverwatch.Resources.System.Numerics.Vectors.dll");
                }
                if (assembly.Name.Contains("System.Runtime.CompilerServices.Unsafe,"))
                {
                    return LoadAssembly("BetterOverwatch.Resources.System.Runtime.CompilerServices.Unsafe.dll");
                }
                if (assembly.Name.Contains("System.Runtime.Runtime,"))
                {
                    return LoadAssembly("BetterOverwatch.Resources.System.Runtime.dll");
                }
                if (assembly.Name.Contains("System.Runtime.ValueTuple,"))
                {
                    return LoadAssembly("BetterOverwatch.Resources.System.ValueTuple.dll");
                }
                return null;
            };

            try
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    AppData.isAdmin = new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
                }
                if (!Server.CheckNewestVersion()) return;

                Directory.CreateDirectory(AppData.configPath);
                AppData.settings = new Settings();
                Settings.Load();
                AppData.gameData = new GameData();
                ScreenCaptureHandler.trayMenu = new TrayMenu();
                Server.autoUpdaterTimer.Start();
                keyboardHook = new KeyboardHook(true);
                keyboardHook.KeyDown += TABPressed;
                keyboardHook.KeyUp += TABReleased;
                AppData.tf = new TensorFlowNetwork();

            //    MessageBox.Show(AppData.tf.Run(new[] {
            //    new Bitmap(@"C:\test\t\delete\3c9a2329-7bee-4add-845b-3b1394b1d769.png"),
            //    new Bitmap(@"C:\test\t\delete\6e5739a5-f59a-4b41-b2c2-2b1b595ba52a.png")
            //}));
                Functions.DebugMessage("Better Overwatch started");
            }
            catch (Exception e)
            {
                MessageBox.Show("Startup error: " + e + "\r\n\r\nReport this on the discord server");
                Environment.Exit(0);
                return;
            }
            if (!AppData.isAdmin)
            {
                adminPromptForm = new AdminPromptForm();
                adminPromptForm.Show();
            }
            else
            {
                Server.VerifyToken();
            }
            new Thread(ScreenCaptureHandler.ScreenCapture) { IsBackground = true }.Start();
            Application.Run(ScreenCaptureHandler.trayMenu);
        }
        private static void TABPressed(Keys key, bool Shift, bool Ctrl, bool Alt)
        {
            if (key == Keys.Tab)
            {
                AppData.gameData.tabTimer.Start();
                AppData.gameData.tabPressed = true;
            }
        }
        private static void TABReleased(Keys key, bool Shift, bool Ctrl, bool Alt)
        {
            if (key == Keys.Tab)
            {
                AppData.gameData.tabTimer.Reset();
                AppData.gameData.tabPressed = false;
            }
        }
        private static Assembly LoadAssembly(string resource)
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource))
            {
                if (stream != null)
                {
                    byte[] assemblyData = new byte[stream.Length];

                    stream.Read(assemblyData, 0, assemblyData.Length);
                    return Assembly.Load(assemblyData);
                }
            }
            return null;
        }
    }
}