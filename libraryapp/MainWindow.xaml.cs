using System.Windows;
using libraryapp.Pages;

namespace libraryapp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            AppNavigation.MainFrame = RootFrame;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            BtnAdmin.Visibility = AppSession.IsAdmin ? Visibility.Visible : Visibility.Collapsed;
            BtnAuthor.Visibility = AppSession.IsAuthorRole ? Visibility.Visible : Visibility.Collapsed;
            UpdateFreezeBanner();
            RootFrame.Navigate(new CatalogPage());
        }

        private void RootFrame_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            UpdateFreezeBanner();
        }

        private void UpdateFreezeBanner()
        {
            AppSession.ReloadCurrentUser();
            FreezeBanner.Visibility = AppSession.IsFrozen ? Visibility.Visible : Visibility.Collapsed;
        }

        private void NavCatalog(object sender, RoutedEventArgs e) => RootFrame.Navigate(new CatalogPage());

        private void NavLists(object sender, RoutedEventArgs e) => RootFrame.Navigate(new BookListsPage());

        private void NavAdmin(object sender, RoutedEventArgs e)
        {
            if (!AppSession.IsAdmin) return;
            RootFrame.Navigate(new AdminPage());
        }

        private void NavAuthor(object sender, RoutedEventArgs e)
        {
            if (!AppSession.IsAuthorRole) return;
            RootFrame.Navigate(new AuthorPage());
        }

        private void NavProfile(object sender, RoutedEventArgs e) => RootFrame.Navigate(new ProfilePage());

        private void NavLogout(object sender, RoutedEventArgs e)
        {
            AppSession.ClearUser();
            var login = new LoginWindow();
            login.Show();
            Close();
        }
    }
}
