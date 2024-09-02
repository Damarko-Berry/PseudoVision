using System.IO;
using System.Windows;


namespace PVChannelManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow Instance = null;
        public static string Channels = Path.Combine(Directory.GetCurrentDirectory(), "Channels");

        public MainWindow()
        {
            InitializeComponent();
            Instance ??= this;
            Directory.CreateDirectory(Channels);
            Main.Content= new MainPage();
        }   
    }
}