﻿using PVLib;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace PVChannelManager
{
    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public partial class MainPage : Page
    {
        public static MainPage Instance = null;
        
        public MainPage()
        {

            InitializeComponent();
            if (Instance == null) Instance = this;
            if (!File.Exists(FileSystem.SettingsFile))
            {
                SaveLoad<Settings>.Save(Settings.Default, FileSystem.SettingsFile);
            }
            Settings.CurrentSettings = SaveLoad<Settings>.Load(FileSystem.SettingsFile);

            var pros = Process.GetProcesses(Environment.MachineName);
            for (int i = 0; i < pros.Length; i++)
            {
                if (pros[i].ProcessName == "PseudoVision")
                {
                    Server = pros[i];
                    ServerStatus.IsChecked = true;
                }
            }
            Load();
        }
        private void New_Click(object sender, RoutedEventArgs e)
        {
            ChannelMaker channel = new ChannelMaker();
            channel.Show();

        }

        public void Load()
        {
            DirectoryInfo info = new DirectoryInfo(MainWindow.Channels);
            ChannelList.Children.Clear();
            for (int i = 0; i < info.GetDirectories().Length; i++)
            {
                var ch = Channel.Load(Path.Combine(info.GetDirectories()[i].FullName, "Channel.chan"));
                ChannelInfo channel = new(ch);
                ChannelList.Children.Add(channel);
            }
        }
        
        Process Server = null;

        private void ServerStatus_Checked(object sender, RoutedEventArgs e)
        {
            if (Server == null)
            {
                Server = new Process();
                Server.StartInfo = new(Path.Combine(Directory.GetCurrentDirectory(), "PseudoVision.exe"));
                Server.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();
                Server.StartInfo.CreateNoWindow = true;
                Server.Start();
            }
            ServerStatus.Content = "Server: On";
        }

        private void ServerStatus_Unchecked(object sender, RoutedEventArgs e)
        {
            Server.Kill();
            ServerStatus.Content = "Server: Off";
            Server = null;
        }

        private void tosets_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.Main.Content = new SettingsPage();
        }
    }
}
