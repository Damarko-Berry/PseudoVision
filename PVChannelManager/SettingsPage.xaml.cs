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
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage : Page
    {
        public Settings NSettings
        {
            get
            {
                Settings settings = new Settings();
                settings.Port = (int)Port;
                settings.useUPNP = (bool)UPNPbool.IsChecked;
                settings.upnp.DeviceName = UPNPName.Text;
                settings.upnp.ModelName = UPNPModelName.Text;
                settings.upnp.ModelNumber = UPNPModelNumber;
                settings.Archive_Output = OutPath.Text;
                settings.upnp.Major = (int)Major;
                settings.upnp.Minor = (int)Minor;
                settings.upnp.Manufacturer = UPNPManufacturer.Text;
                settings.playlistFormat = (PlaylistFormat)PlstFormat.SelectedIndex;
                settings.securityLevel = (SecurityLevel)SecurityType.SelectedIndex;
                return settings;
            }
        }
        public SettingsPage()
        {
            InitializeComponent();
            var sets = new Settings();
            try
            {
                sets = SaveLoad<Settings>.Load("settings");
            }
            catch
            {
                sets = Settings.Default;
            }
            Port.Text = sets.Port.ToString();
            OutPath.Text = sets.Archive_Output;
            UPNPbool.IsChecked = sets.useUPNP;
            UPNPName.Text = sets.upnp.DeviceName;
            UPNPModelName.Text = sets.upnp.ModelName;
            UPNPModelNumber.Text = sets.upnp.ModelNumber.ToString();
            Major.Text = sets.upnp.Major.ToString();
            Minor.Text = sets.upnp.Minor.ToString();
            UPNPManufacturer.Text = sets.upnp.Manufacturer;
            PlstFormat.ItemsSource = Enum.GetValues(typeof(PlaylistFormat));
            PlstFormat.SelectedIndex = (int)sets.playlistFormat;
            SecurityType.ItemsSource = Enum.GetValues(typeof(SecurityLevel));
            SecurityType.SelectedIndex = (int)sets.securityLevel;
            UPNPStuff.IsEnabled = (bool)UPNPbool.IsChecked;
        }

        private void UPNPbool_Checked(object sender, RoutedEventArgs e)
        {
            UPNPStuff.IsEnabled = true;
        }

        private void UPNPbool_Unchecked(object sender, RoutedEventArgs e)
        {
            UPNPStuff.IsEnabled = false;
        }

        private void UPNPbool_Click(object sender, RoutedEventArgs e)
        {
            
            if(UPNPStuff.IsEnabled)
            {
                UPNPbool.IsChecked = false;
            }
            else
            {
                UPNPbool.IsChecked = true;
            }
        }

        private void save_Click(object sender, RoutedEventArgs e)
        {
            SaveLoad<Settings>.Save(NSettings, "settings");
            MainWindow.Instance.Main.GoBack();
        }
    }
}
