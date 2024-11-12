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
    /// Interaction logic for SeasonEditor.xaml
    /// </summary>
    public partial class SeasonEditor : Page
    {
        string Directory;
        Season subject;
        Page Previous;
        public SeasonEditor()
        {
            InitializeComponent();
            Directory= string.Empty;
            subject = new Season();
        }
        public SeasonEditor(string SD, Page prev)
        {
            InitializeComponent();
            Directory = SD;
            subject = new Season();
            Previous = prev;
        }
        public SeasonEditor(Season newSub, string SD, Page previous)
        {
            InitializeComponent();
            Directory = SD;
            subject = newSub;
            nbx.Text = subject.Name;
            end.SelectedDate = subject.End;
            strt.SelectedDate = subject.Start;
            load();
            Previous = previous;
        }
        private void AddSPecial_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = true;
            ofd.Filter = "Video Files|*.mp4;*.avi;*.mov;*.mkv;*.flv";
            if (ofd.ShowDialog() == true)
            {
                subject.Specials.AddRange(ofd.FileNames);
            }
            load();
        }
        void load()
        {
            Specials.Children.Clear();
            for (int i = 0; i < subject.Specials.Count; i++)
            {
                var butt = new OmniButton<int>(i, subject.Specials.RemoveAt);
                butt.Content = new FileInfo(subject.Specials[i]).Name;
                Specials.Children.Add( butt );
            }
        }
        private void savese_Click(object sender, RoutedEventArgs e)
        {
            subject.Start = strt.SelectedDate.Value;
            subject.End = end.SelectedDate.Value;
            subject.Name = nbx.Text;
            if(subject.Name.Trim() != string.Empty)
            SaveLoad<Season>.Save(subject, Path.Combine(Directory, subject.Name));
            MainWindow.Instance.Main.Content = Previous;
        }
    }
}
