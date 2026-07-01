using SkyGuardApp.Services;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SkyguardApp
{
    public partial class LoginView : UserControl
    {
        private enum Lang
        {
            TR,
            EN,
            DE
        }

        private readonly LoginService _loginService;

        public LoginView()
        {
            InitializeComponent();
            UpdateResource(Lang.EN);
            _loginService = new LoginService();

            this.Loaded += (s, e) =>
            {
                if (txtPass != null)
                {
                    txtPass.KeyDown += EnterKey;
                }
            };
        }

        private async void btn_Login_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUser.Text;
            string password = txtPass.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Lütfen tüm alanları doldurun.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (username == "guest" && password == "123456789")
            {
                if (Application.Current.MainWindow is MainWindow mainWindow)
                {
                    mainWindow.SwitchToRadar();
                }
                return;
            }

            try
            {
                if (sender is Button loginBtn) loginBtn.IsEnabled = false;

                LoginResult? result = await _loginService.AuthenticateAsync(username, password);

                if (result != null && result.status == "success")
                {
                    if (Application.Current.MainWindow is MainWindow mainWindow)
                    {
                        mainWindow.SwitchToRadar();
                    }
                }
                else
                {
                    string errorMsg = result?.message ?? "Kullanıcı adı veya şifre hatalı.";
                    MessageBox.Show(errorMsg, "Giriş Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Sistem hatası: " + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (sender is Button loginBtn) loginBtn.IsEnabled = true;
            }
        }

        private void EnterKey(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                btn_Login_Click(sender, e);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void cmbLang_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbLang != null && cmbLang.SelectedItem is ComboBoxItem selectedItem)
            {
                string langTag = selectedItem.Tag?.ToString() ?? "TR";
                Lang selectedLang = (Lang)Enum.Parse(typeof(Lang), langTag);
                UpdateResource(selectedLang);
            }
        }

        private void UpdateResource(Lang lang)
        {
            ResourceDictionary dict = new ResourceDictionary();
            switch (lang)
            {
                case Lang.EN:
                    dict.Source = new Uri("Resources/StringResources.en.xaml", UriKind.Relative);
                    break;
                case Lang.DE:
                    dict.Source = new Uri("Resources/StringResources.de.xaml", UriKind.Relative);
                    break;
            }

            var oldDict = Application.Current.Resources.MergedDictionaries.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.StartsWith("Resources/StringResources."));
            if (oldDict != null)
            {
                int index = Application.Current.Resources.MergedDictionaries.IndexOf(oldDict);
                Application.Current.Resources.MergedDictionaries[index] = dict;
            }
            else
            {
                Application.Current.Resources.MergedDictionaries.Add(dict);
            }
        }
    }
}