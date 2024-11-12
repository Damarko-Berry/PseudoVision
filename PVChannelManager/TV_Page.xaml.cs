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
            MovieModeBox.ItemsSource = Enum.GetValues(typeof(MovieMode));
            MovieModeBox.SelectedIndex = (int)channel.movieMode;
            TimeFillCheck.IsChecked = channel.FillTime;
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
            ShortsList.Children.Clear();
            var shrtsD = new DirectoryInfo(subject.ShortsDirectory).GetFiles();
            for (int i = 0;i < shrtsD.Length; i++)
            {
                var butt = new OmniButton<string>(shrtsD[i].Name, DeletShrtD);
                butt.GetClick += Load;
                butt.Content = shrtsD[i].Name;
                this.ShortsList.Children.Add(butt);
            }
            SeasonsList.Children.Clear();
            var Seas=subject.Seasons;
            for (int i = 0; i < Seas.Length; i++)
            {
                var but = new OmniButton<Season>(Seas[i], EditSeason);
                but.Content = Seas[i].Name;
                SeasonsList.Children.Add(but);

            }
        }
        void DeletShrtD(string shorts)
        {
            File.Delete(Path.Combine(subject.ShortsDirectory, shorts));
            Load();
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
                        Load();
                    }
                }
            }
        }
        void EditSeason(Season season)
        {
            SeasonEditor seasonEditor = new(season ,subject.SeasonsDirectory);
            MainPage.Instance.Content = seasonEditor;
        }
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SaveLoad<TV_LikeChannel>.Save(subject, Path.Combine(subject.HomeDirectory, "Channel.chan"));
            MainPage.Instance.Load();
            MainWindow.Instance.Main.Content = MainPage.Instance;
        }

        private void TimeFillCheck_Checked(object sender, RoutedEventArgs e)
        {
            subject.FillTime = true;
        }

        private void TimeFillCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            subject.FillTime = false;
        }

        private void MovieMode_Selected(object sender, RoutedEventArgs e)
        {
            subject.movieMode = (MovieMode)MovieModeBox.SelectedIndex;
        }

        private void AddShorts_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog folderDialog = new OpenFolderDialog();
            folderDialog.DefaultDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
            folderDialog.Multiselect = false;
            if ((bool)folderDialog.ShowDialog())
            {
                DirectoryInfo showinfo = new(folderDialog.FolderName);
                MovieDirectory show = new();
                show.HomeDirectory = showinfo.FullName;
                SaveLoad<MovieDirectory>.Save(show, Path.Combine(subject.ShortsDirectory, showinfo.Name + ".shw"));
                Load();
            }
            MainPage.Instance.Load();
        }

        private void AddSeasons_Click(object sender, RoutedEventArgs e)
        {
            SeasonEditor seasonEditor = new SeasonEditor(subject.SeasonsDirectory);
            MainPage.Instance.Content = seasonEditor;
        }
    }
}
