using DiscordRPC;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Button = DiscordRPC.Button;

namespace WuWaDiscordRpc;

internal static class Program
{
    private const string AppId_En = "1243140591064453202";

    [STAThread]
    static void Main()
    {
        using var self = new Mutex(true, "WuWa DiscordRPC", out var allow);
        if (!allow)
        {
            MessageBox.Show("WuWa DiscordRPC is already running.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Environment.Exit(-1);
        }

        if (Properties.Settings.Default.IsFirstTime)
        {
            AutoStart.Set();
            Properties.Settings.Default.IsFirstTime = false;
            Properties.Settings.Default.Save();
        }

        Task.Run(async () =>
        {
            using var clientEn = new DiscordRpcClient(AppId_En);
            clientEn.Initialize();

            var playing = false;

            while (true)
            {
                await Task.Delay(1000);

                Debug.Print($"InLoop");

                Process[] processes = Process.GetProcessesByName("Wuthering Waves");
                
                if (processes.Length == 0)
                {
                    Debug.Print($"Not found game process.");
                    playing = false;
                    if (clientEn.CurrentPresence != null)
                    {
                        clientEn.ClearPresence();
                    }
                    continue;
                }

                try
                {
                    var process = processes[0];

                    Debug.Print($"Check process with {process.ProcessName}");

                    if (!playing)
                    {
                        playing = true;
                        clientEn.UpdateRpc("logo", "Wuthering Waves");
                        Debug.Print($"Set RichPresence to  {process.ProcessName}");
                    }
                    else
                    {
                        Debug.Print($"Keep RichPresence to {process.ProcessName}");
                    }
                }
                catch (Exception e)
                {
                    playing = false;
                    if (clientEn.CurrentPresence != null)
                    {
                        clientEn.ClearPresence();
                    }
                    Debug.Print($"{e.Message}{Environment.NewLine}{e.StackTrace}");
                }

                GC.Collect();
                GC.WaitForFullGCComplete();
            }
        });

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var notifyMenu = new ContextMenu();
        var exitButton = new MenuItem("Exit");
        var autoButton = new MenuItem("AutoStart" + "    " + (AutoStart.Check() ? "√" : "✘"));
        notifyMenu.MenuItems.Add(0, autoButton);
        notifyMenu.MenuItems.Add(1, exitButton);

        var notifyIcon = new NotifyIcon()
        {
            BalloonTipIcon = ToolTipIcon.Info,
            ContextMenu = notifyMenu,
            Text = "WuWa DiscordRPC",
            Icon = Properties.Resources.tray,
            Visible = true,
        };

        exitButton.Click += (_, _) =>
        {
            notifyIcon.Visible = false;
            Thread.Sleep(100);
            Environment.Exit(0);
        };
        autoButton.Click += (_, _) =>
        {
            if (AutoStart.Check())
            {
                AutoStart.Remove();
            }
            else
            {
                AutoStart.Set();
            }

            autoButton.Text = "AutoStart" + "    " + (AutoStart.Check() ? "√" : "✘");
        };


        Application.Run();
    }

    private static void UpdateRpc(this DiscordRpcClient client, string key, string text)
        => client.SetPresence(new RichPresence
        {
            Assets = new Assets
            {
                LargeImageKey = key,
                LargeImageText = text,
                SmallImageText = "Stormy Waves",
                SmallImageKey = "sw",
            },
            Buttons = new Button[]
            {
                new Button
                {
                    Label = "Download Game",
                    Url = "https://wutheringwaves.kurogames.com/"
                },
                new Button
                {
                    Label = "Download RPC",
                    Url = "https://github.com/Stormy-Waves/WuWaRpc/releases/latest"
                }
            },
            Timestamps = Timestamps.Now,
        });

    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
}
