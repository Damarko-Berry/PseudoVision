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
                    Show show = new();
                    show.HomeDirectory = showinfo.FullName;
                    SaveLoad<Show>.Save(show, Path.Combine(subject.ShowDirectory, showinfo.Name+".shw"));
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
        }

        private void Local_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText($"{GetLocalIPAddress()}:{SaveLoad<Settings>.Load("settings").Port }/watch/{subject.ChannelName}");
        }
        static string GetLocalIPAddress()
        {
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
        static async Task<IPAddress?> GetExternalIpAddress()
        {
            var externalIpString = (await new HttpClient().GetStringAsync("http://icanhazip.com"))
                .Replace("\r\n", "").Replace("\n", "").Trim();
            if (!IPAddress.TryParse(externalIpString, out var ipAddress))
                return null;
            return ipAddress;
        }
    }
    
}
