using Microsoft.Win32;
using PVLib;
using System.IO;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using System.Net.Sockets;
using System.Net;
using System.Net.Http;

namespace PVChannelManager
{
    /// <summary>
    /// Interaction logic for ChannelInfo.xaml
    /// </summary>
    public partial class ChannelInfo : UserControl
    {
        public Channel subject;
        bool isInitialized = false;


        protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
        {
            switch(subject.channel_Type)
            {
                case Channel_Type.Binge_Like:
                    MainWindow.Instance.Main.Content = new BingePage((Binge_LikeChannel)subject);
                    break;
                case Channel_Type.TV_Like:
                    MainWindow.Instance.Main.Content = new TV_Page((TV_LikeChannel)subject);
                    break;
            }
        }

        public ChannelInfo(Channel subject)
        {
            InitializeComponent();
            this.subject = subject;
            NameBlock.Text = subject.ChannelName;
            UpdateShowList();
            isInitialized = true;
        }

        
        private void AddShow_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog folderDialog = new OpenFolderDialog();
            folderDialog.DefaultDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
            folderDialog.Multiselect = true;
            if ((bool)folderDialog.ShowDialog())
            {
                for (int i = 0; i < folderDialog.FolderNames.Length; i++)
                {
                    DirectoryInfo showinfo = new(folderDialog.FolderNames[i]);
                    var Dtype = ContentDirectory.DDetector(showinfo);
                    if (subject.isSupported(Dtype))
                    {
                        if (Dtype == DirectoryType.Show)
                        {
                            Show show = new();
                            show.HomeDirectory = showinfo.FullName;
                            SaveLoad<Show>.Save(show, Path.Combine(subject.ShowDirectory, showinfo.Name + ".shw"));
                        }
                        else
                        {
                            MovieDirectory show = new();
                            show.HomeDirectory = showinfo.FullName;
                            SaveLoad<MovieDirectory>.Save(show, Path.Combine(subject.ShowDirectory, showinfo.Name + ".shw"));
                        }
                        UpdateShowList();
                    }
                    else
                    {
                        MessageBox.Show("The following channel doesn't support this directory's structure");
                    }
                }
            }
        }
        bool usd;
        void UpdateShowList()
        {
            usd = true;

            ShowList.Items.Clear();
            DirectoryInfo AllShows = new(subject.ShowDirectory);
            for (int i = 0; i < AllShows.GetFiles().Length; i++)
            {
                var nam = AllShows.GetFiles()[i].Name.Replace(".shw", string.Empty);
                ShowList.Items.Add(nam);
            }
            ShowList.SelectedIndex= -1;
            usd = false;
        }

        private void ShowList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (usd | !isInitialized) return;
            string ShowName = ShowList.Items[ShowList.SelectedIndex].ToString();
            MessageBox.Show(ShowName);
            subject.Cancel(ShowName);
            UpdateShowList();
        }

        
        static string GetLocalIPAddress()
        {
            var set = SaveLoad<Settings>.Load("settings");
            if (set.IP != string.Empty) return set.IP;
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("Local IP Address Not Found!");
        }


        private void Public_Click(object sender, RoutedEventArgs e)
        {
            
            Clipboard.SetText($"http://{GetLocalIPAddress()}:{SaveLoad<Settings>.Load("settings").Port}/live/{subject.ChannelName}");
        }
        
        private void Del_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to delete this channel","Deleting channel", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                string ChanName = subject.ChannelName;
                Directory.Delete(Path.Combine(MainWindow.Channels, ChanName), true);
                Directory.Delete(Path.Combine(MainWindow.Schedules, ChanName), true);
                MainPage.Instance.Load();
            }
        }

        private void HardRe_Click(object sender, RoutedEventArgs e)
        {
            if(MessageBox.Show("A hard reset will delete all CTD and reruns(if applicable).\nAre you sure?","Hard Reset", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                DirectoryInfo AllShows = new(subject.ShowDirectory);
                while(AllShows.GetFiles().Length>0)
                {
                    if (subject.channel_Type == Channel_Type.TV_Like)
                    {
                        var TV = (TV_LikeChannel)subject;
                        TV.Cancel(AllShows.GetFiles()[0].Name.Replace(".shw",string.Empty));
                        SaveLoad<TV_LikeChannel>.Save(TV, Path.Combine(subject.HomeDirectory, "Channel.chan"));
                    }
                    else
                    {
                        subject.Cancel(AllShows.GetFiles()[0].FullName);
                        SaveLoad<Binge_LikeChannel>.Save((Binge_LikeChannel)subject, Path.Combine(subject.HomeDirectory, "Channel.chan"));
                    }
                }
            }
        }

        private void SoftRe_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("A soft reset will reset any progress made on CTD.\nAre you sure?", "Hard Reset", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                DirectoryInfo AllShows = new(subject.ShowDirectory);
                for (int i = 0; i < AllShows.GetFiles().Length; i++)
                {
                    var nam = AllShows.GetFiles()[i].FullName;
                    var shw =SaveLoad<Show>.Load(nam);
                    shw.Reset();
                    SaveLoad<Show>.Save(shw,nam);
                }
                if (subject.channel_Type == Channel_Type.TV_Like)
                {
                    var TV = (TV_LikeChannel)subject;
                    Directory.Delete(TV.RerunDirectory, true);
                    Directory.CreateDirectory(TV.RerunDirectory);
                    TV.rotation.ShowList.Clear();
                    SaveLoad<TV_LikeChannel>.Save(TV, Path.Combine(subject.HomeDirectory, "Channel.chan"));
                }
            }
        }
    }
    
}
