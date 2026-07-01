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
using System.Windows.Shapes;

namespace Login
{
    /// <summary>
    /// Login.xaml etkileşim mantığı
    /// </summary>
    public partial class Login : Window
    {
        public Login()
        {
            InitializeComponent();
            AnaCerceve.Content = new LoginControl();
        }
        public void HaritayaGec()
        {
            AnaCerceve.Content = new HaritaControl();
        }
    }
}
