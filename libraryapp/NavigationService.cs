using System.Windows.Controls;

namespace libraryapp
{
    /// <summary>
    /// Статический класс для управления навигацией между страницами приложения.
    /// </summary>
    public static class AppNavigation
    {
        public static Frame MainFrame { get; set; }

        public static void Navigate(Page page)
        {
            // Если MainFrame == null, метод завершится без ошибки
            MainFrame?.Navigate(page);
        }
    }
}
