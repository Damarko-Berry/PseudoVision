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

namespace PVChannelManager
{
    /// <summary>
    /// Interaction logic for ChannelMaker.xaml
    /// </summary>
    public partial class ChannelMaker : Window
    {
        public string ChanName;
        public Channel_Type ChannelType => (Channel_Type)TypeSelect.SelectedIndex;
        public ChannelMaker()
        {
            InitializeComponent();
            TypeSelect.ItemsSource = Enum.GetNames(typeof(Channel_Type));
            TypeSelect.SelectedIndex = 0;
        }

        private void CN_TextChanged(object sender, TextChangedEventArgs e)
        {
            ChanName = CN.Text;
        }

        private void Make_Click(object sender, RoutedEventArgs e)
        {
            if (ChanName == string.Empty) return;
            if (Directory.Exists(Path.Combine(MainWindow.Channels, ChanName))) return;
            Directory.CreateDirectory(Path.Combine(MainWindow.Channels, ChanName));
            Directory.CreateDirectory(Path.Combine(MainWindow.Channels, ChanName, "Shows"));
            switch (ChannelType)
            {
                case Channel_Type.Binge_Like:
                    Binge_LikeChannel Bchannel = new();
                    Bchannel.HomeDirectory = Path.Combine(MainWindow.Channels, ChanName);
                    SaveLoad<Binge_LikeChannel>.Save(Bchannel, Path.Combine(MainWindow.Channels, ChanName, "Channel.chan")); 
                    break;
                case Channel_Type.TV_Like:
                    TV_LikeChannel Tchannel = new();
                    Tchannel.HomeDirectory = Path.Combine(MainWindow.Channels, ChanName);
                    SaveLoad<TV_LikeChannel>.Save(Tchannel, Path.Combine(MainWindow.Channels, ChanName, "Channel.chan")); 
                    break;

            }
            MainPage.Instance.Load();
            Close();
        }

       
    }
}
