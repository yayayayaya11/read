using System.Windows.Controls;

namespace libraryapp
{
    public static class AppNavigation
    {
        public static Frame MainFrame { get; set; }

        public static void Navigate(Page page)
        {
            MainFrame?.Navigate(page);
        }
    }
}
