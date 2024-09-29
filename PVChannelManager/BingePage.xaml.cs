using Microsoft.Win32;
using PVLib;
using System;
using System.Collections.Generic;
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
    /// Interaction logic for BingePage.xaml
    /// </summary>
    public partial class BingePage : Page
    {
        Binge_LikeChannel channel;
        public BingePage(Binge_LikeChannel c)
        {
            channel = c;
            InitializeComponent();
            ChanName.Text = channel.ChannelName;
            SendToNC.IsChecked = channel.SendToNextChanWhenFinished;
            ChannelList.ItemsSource = GetChannels();
            if (ChannelList.Items.Count > 0)
            {
                ChannelList.SelectedIndex = 0;
            }
            LoadList();
        }

        string[] GetChannels()
        {
            List<string> channels = new List<string>();
            DirectoryInfo Chans = new(Path.Combine(Directory.GetCurrentDirectory(), "Channels"));

            for (int i = 0; i < Chans.GetDirectories().Length; i++)
            {
                for (int j = 0; j < Chans.GetDirectories()[i].GetFiles().Length; j++)
                {
                    if (Chans.GetDirectories()[i].GetFiles()[j].Extension.Contains("chan"))
                    {
                        channels.Add(Chans.GetDirectories()[i].GetFiles()[j].FullName);
                        break;
                    }
                }
            }
            return channels.ToArray();
        }

        private void SendToNC_Checked(object sender, RoutedEventArgs e)
        {
            channel.SendToNextChanWhenFinished = true;
            CJ.IsEnabled = true;
        }

        private void SendToNC_Unchecked(object sender, RoutedEventArgs e)
        {
            channel.SendToNextChanWhenFinished = false;
            CJ.IsEnabled = false;
        }

        void LoadList(string j= "")
        {
            AllShows.Children.Clear();
            var sh = channel.Shows;
            for (int i = 0; i < sh.Length; i++)
            {
                var Butt = new OmniButton<string>(new FileInfo(sh[i].HomeDirectory).Name, channel.Cancel);
                Butt.Content = new FileInfo(sh[i].HomeDirectory).Name;
                Butt.GetClick += LoadList;
                AllShows.Children.Add(Butt);
            }
        }
        
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SaveLoad<Binge_LikeChannel>.Save(channel, Path.Combine(channel.HomeDirectory,"Channel.chan"));
            MainPage.Instance.Load();
            MainWindow.Instance.Main.Content = MainPage.Instance;
        }

        private void ChannelList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            channel.NextChan = ChannelList.SelectedItem.ToString();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog folderDialog = new OpenFolderDialog();
            folderDialog.DefaultDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
            folderDialog.Multiselect = true;
            if ((bool)folderDialog.ShowDialog())
            {
                for (int i = 0; i < folderDialog.FolderNames.Length; i++)
                {
                    DirectoryInfo showinfo = new(folderDialog.FolderNames[i]);
                    Show show = new();
                    show.HomeDirectory = showinfo.FullName;
                    SaveLoad<Show>.Save(show, Path.Combine(channel.ShowDirectory, showinfo.Name + ".shw"));
                    LoadList();
                }
            }
        }
    }
}
