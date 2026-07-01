using System.Windows;
using System.Windows.Media.Imaging;

namespace SkyguardApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ViewDisplay.Content = new LoginView();
            this.Icon=BitmapFrame.Create(new Uri("pack://application:,,,/Images/Icon.ico", UriKind.Absolute));
        }

        public void SwitchToRadar()
        {
            ViewDisplay.Content = new RadarView();
        }
        public void SwitchToLoginView()
        {
            ViewDisplay.Content = new LoginView();
        }
    }
}