﻿using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using PVLib;


namespace PVChannelManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow Instance = null;
        public static string Channels = Path.Combine(Directory.GetCurrentDirectory(), "Channels");
        
        public MainWindow()
        {
            InitializeComponent();
            Instance ??= this;
            Directory.CreateDirectory(Channels);
            Load();
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            ChannelMaker channel = new ChannelMaker();
            channel.Show();

        }

        public void Load()
        {
            DirectoryInfo info = new DirectoryInfo(Channels);
            ChannelList.Items.Clear();
            for (int i = 0; i < info.GetDirectories().Length; i++)
            {
                var ch = SaveLoad<Channel>.Load(Path.Combine(info.GetDirectories()[i].FullName, "Channel.chan"));
                ChannelInfo channel = new(ch);
                ChannelList.Items.Add(channel);
            }
        }
        Process Server = null;
        private void StartServer_Click(object sender, RoutedEventArgs e)
        {
            if(Server != null)
            Server.Kill();
            try
            {
                Server = new Process();
                Server.StartInfo = new(Path.Combine(Directory.GetCurrentDirectory(), "Server", "PseudoVision.exe"));
                Server.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            Server.Start();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < ChannelList.Items.Count; i++)
            {
                if (ChannelList.Items[i].GetType() == typeof(ChannelInfo))
                {
                    var ch = (ChannelInfo)ChannelList.Items[i];
                    SaveLoad<Channel>.Save(ch.subject ,Path.Combine(ch.subject.HomeDirectory, "Channel.chan"));
                }
            }
            
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
            base.OnClosing(e);
        }
        bool serv;
        private void ServerStatus_Checked(object sender, RoutedEventArgs e)
        {
            Server = new Process();

            Server.StartInfo = new(Path.Combine(Directory.GetCurrentDirectory(), "Server", "PseudoVision.exe"));
            Server.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();
            Server.StartInfo.CreateNoWindow = true;
            Server.Start();
            ServerStatus.Content = "Server: On";
        }

        private void ServerStatus_Unchecked(object sender, RoutedEventArgs e)
        {
                Server.Kill();
                ServerStatus.Content = "Server: Off";
               
        }
    }
}