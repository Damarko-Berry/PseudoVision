﻿using PVLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

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
            Load();
            if (!File.Exists("settings"))
            {
                SaveLoad<Settings>.Save(Settings.Default, "settings");
            }
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
                var ch = SaveLoad<Channel>.Load(Path.Combine(info.GetDirectories()[i].FullName, "Channel.chan"));
                ChannelInfo channel = new(ch);
                ChannelList.Children.Add(channel);
            }
        }
        public void SaveChans()
        {
            for (int i = 0; i < ChannelList.Children.Count; i++)
            {
                if (ChannelList.Children[i].GetType() == typeof(ChannelInfo))
                {
                    var ch = (ChannelInfo)ChannelList.Children[i];
                    SaveLoad<Channel>.Save(ch.subject, Path.Combine(ch.subject.HomeDirectory, "Channel.chan"));
                }
            }
        }
        Process Server = null;


        private void Save_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < ChannelList.Children.Count; i++)
            {
                if (ChannelList.Children[i].GetType() == typeof(ChannelInfo))
                {
                    var ch = (ChannelInfo)ChannelList.Children[i];
                    SaveLoad<Channel>.Save(ch.subject, Path.Combine(ch.subject.HomeDirectory, "Channel.chan"));
                }
            }

        }

        bool serv;
        private void ServerStatus_Checked(object sender, RoutedEventArgs e)
        {
            Server = new Process();
            Server.StartInfo = new(Path.Combine(Directory.GetCurrentDirectory(), "PseudoVision.exe"));
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

        private void tosets_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.Main.Content = new SettingsPage();
        }
    }
}