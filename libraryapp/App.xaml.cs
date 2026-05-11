using System;
using System.Windows;

namespace libraryapp
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                Seed.Ensure();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось подключиться к базе данных или выполнить начальное заполнение.\r\n" +
                    "Убедитесь, что выполнен скрипт Sql\\CreateLibraryDatabase.sql и строка подключения в App.config верна.\r\n\r\n" +
                    ex.Message,
                    "Библиотека",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
                return;
            }

            var login = new LoginWindow();
            login.Show();
        }
    }
}
