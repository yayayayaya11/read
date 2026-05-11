using System.Linq;
using System.Windows;

namespace libraryapp
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void DoLogin(object sender, RoutedEventArgs e)
        {
            var login = LoginLogin.Text.Trim();
            var password = LoginPassword.Password;
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Введите логин и пароль.", "Вход", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var user = Core.Context.AppUsers.FirstOrDefault(u => u.Login == login);
            if (user == null || !PasswordHelper.Verify(password, user.PasswordHash))
            {
                MessageBox.Show("Неверный логин или пароль.", "Вход", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            AppSession.SetUser(user);
            var main = new MainWindow();
            main.Show();
            Close();
        }

        private void DoRegister(object sender, RoutedEventArgs e)
        {
            var login = RegLogin.Text.Trim();
            var password = RegPassword.Password;
            var email = RegEmail.Text.Trim();
            var name = RegName.Text.Trim();
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Заполните все поля.", "Регистрация", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Core.Context.AppUsers.Any(u => u.Login == login))
            {
                MessageBox.Show("Пользователь с таким логином уже существует.", "Регистрация", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var user = new AppUsers
            {
                Login = login,
                PasswordHash = PasswordHelper.Hash(password),
                Email = email,
                DisplayName = name,
                RoleId = RoleIds.Reader,
                IsFrozen = false
            };
            Core.Context.AppUsers.Add(user);
            Core.Context.SaveChanges();

            MessageBox.Show("Регистрация выполнена. Перейдите на вкладку «Вход».", "Регистрация", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
