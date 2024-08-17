using Microsoft.Win32;
using PVLib;
using System.IO;
using System.Windows.Controls;
using System.Windows;

namespace PVChannelManager
{
    /// <summary>
    /// Interaction logic for ChannelInfo.xaml
    /// </summary>
    public partial class ChannelInfo : UserControl
    {
        public Channel subject;
        bool isInitialized = false;
        
        public ChannelInfo(Channel subject)
        {
            InitializeComponent();
            this.subject = subject;
            Hour.Text = subject.PrimeTime.Hour.ToString();
            HourSlider.Value = subject.PrimeTime.Hour;
            NameBlock.Text = subject.ChannelName;
            UpdateShowList();
            isInitialized = true;
        }

        private void HourSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!isInitialized) return;
            var f = (int)HourSlider.Value;
            Hour.Text = (f > 12) switch
            {
                true => $"{f - 12}pm",
                _ => f switch
                {
                    12 => $"{f}pm",
                    _ => $"{f}am"
                }

            };
            subject.PrimeTime.Hour = f;
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
                    SaveLoad<Show>.Save(show, Path.Combine(subject.HomeDirectory, "Shows", showinfo.Name+".shw"));
                    UpdateShowList();
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
            MainWindow.Instance.SaveChans();
        }
    }
    
}
