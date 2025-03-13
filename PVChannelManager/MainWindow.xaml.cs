using System.IO;
using System.Windows;
using PVLib;

namespace PVChannelManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow Instance = null;
        public static string Channels = FileSystem.Channels;
        public static string Schedules = FileSystem.Schedules;

        public MainWindow()
        {
#if DEBUG
            Directory.SetCurrentDirectory(@"C:\Users\marko\source\repos\PseudoVision\PVChannelManager\bin\Release\net8.0-windows");
#endif
            InitializeComponent();
            Instance ??= this;
            Directory.CreateDirectory(Channels);
            Directory.CreateDirectory(Schedules);
            Main.Content= new MainPage();
        }   
    }
}