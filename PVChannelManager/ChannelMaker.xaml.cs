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
        public Channel_Type ChannelType;
        public ChannelMaker()
        {
            InitializeComponent();
            TypeSelect.ItemsSource = Enum.GetNames(typeof(Channel_Type));
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
            Channel channel = new Channel();
            channel.HomeDirectory = Path.Combine(MainWindow.Channels, ChanName);
            channel.Channel_Type = ChannelType;
            SaveLoad<Channel>.Save(channel, Path.Combine(MainWindow.Channels, ChanName, "Channel.chan"));
            MainPage.Instance.Load();
            Close();
        }

        private void TypeSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ChannelType = (Channel_Type)TypeSelect.SelectedIndex;
        }
    }
}
