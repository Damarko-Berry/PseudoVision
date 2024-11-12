using Microsoft.Win32;
using PVLib;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace PVChannelManager
{
    /// <summary>
    /// Interaction logic for SeasonEditor.xaml
    /// </summary>
    public partial class SeasonEditor : Page
    {
        string Directory;
        Season subject;
        public SeasonEditor()
        {
            InitializeComponent();
            Directory= string.Empty;
            subject = new Season();
        }
        public SeasonEditor(string SD)
        {
            InitializeComponent();
            Directory = SD;
            subject = new Season();
        }
        public SeasonEditor(Season newSub, string SD)
        {
            InitializeComponent();
            Directory= SD;
            subject = newSub;
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
        }
    }
}
