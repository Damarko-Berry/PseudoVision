using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using PVLib;
using Microsoft.Win32;
using System.Threading.Channels;

namespace PVChannelManager
{
    /// <summary>
    /// Interaction logic for TV_Page.xaml
    /// </summary>
    public partial class TV_Page : Page
    {
        TV_LikeChannel subject;
        public TV_Page(TV_LikeChannel channel)
        {
            subject = channel;
            init = true;
            InitializeComponent();
            channame.Text = channel.ChannelName;
            TimeSetter.Value = subject.PrimeTime.Hour;
            var t = subject.PrimeTime.Hour;
            string a = (t >= 12) ? "pm" : "am";
            if (t > 12) t -= 12;
            else if (t == 0) t = 12;
            Time.Text = $"{t}{a}";
            Load();
            init = false;
        }
        bool init;
        private void TimeSetter_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (init) return;
            subject.PrimeTime.Hour = (int)TimeSetter.Value;
            var t = subject.PrimeTime.Hour;
            string a = (t >= 12) ? "pm" : "am";
            if(t > 12) t-=12;
            else if (t == 0) t = 12;
            Time.Text = $"{t}{a}";
        }
        void Load(string A = "")
        {
            this.Showlist.Children.Clear();
            var lit = subject.CTD;
            for (int i = 0; i < lit.Length; i++)
            {
                var butt = new OmniButton<string>(new DirectoryInfo(lit[i].HomeDirectory).Name, subject.Cancel);
                butt.GetClick += Load;
                butt.Content = new FileInfo(lit[i].HomeDirectory).Name;
                this.Showlist.Children.Add( butt );
            }
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
                    Show show = new();
                    show.HomeDirectory = showinfo.FullName;
                    SaveLoad<Show>.Save(show, Path.Combine(subject.ShowDirectory, showinfo.Name + ".shw"));
                    Load();
                }
            }
            MainPage.Instance.Load();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SaveLoad<TV_LikeChannel>.Save(subject, Path.Combine(subject.HomeDirectory, "Channel.chan"));
            MainPage.Instance.Load();
            MainWindow.Instance.Main.Content = MainPage.Instance;
        }
    }
}
