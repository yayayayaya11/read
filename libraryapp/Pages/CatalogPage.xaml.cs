using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace libraryapp.Pages
{
    public partial class CatalogPage : Page
    {
        private bool _filterEventsAttached;

        public CatalogPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (GenreBox.Items.Count == 0)
            {
                GenreBox.Items.Add(new ComboBoxItem { Content = "Все жанры", Tag = null });
                foreach (var g in Core.Context.Genres.OrderBy(x => x.Name).ToList())
                    GenreBox.Items.Add(new ComboBoxItem { Content = g.Name, Tag = g.GenreId });
                GenreBox.SelectedIndex = 0;
            }

            // Не подписывать SelectionChanged из XAML: при инициализации SortBox срабатывает раньше,
            // чем присвоено поле GenreBox → NullReferenceException.
            if (!_filterEventsAttached)
            {
                SortBox.SelectionChanged += (_, __) => Refresh_Click(null, EventArgs.Empty);
                GenreBox.SelectionChanged += (_, __) => Refresh_Click(null, EventArgs.Empty);
                _filterEventsAttached = true;
            }

            Refresh_Click(sender, e);
        }

        private void Refresh_Click(object sender, System.EventArgs e)
        {
            var search = (SearchBox.Text ?? string.Empty).Trim().ToLowerInvariant();
            int? genreId = (GenreBox.SelectedItem as ComboBoxItem)?.Tag as int?;

            var sortRating = (SortBox.SelectedItem as ComboBoxItem)?.Tag as string == "rating";

            var books = Core.Context.Books
                .Include(b => b.AppUsers)
                .Include(b => b.Genres)
                .Include(b => b.Reviews)
                .ToList();

            IEnumerable<Books> q = books;
            if (!string.IsNullOrEmpty(search))
            {
                q = q.Where(b =>
                    (b.Title ?? "").ToLowerInvariant().Contains(search) ||
                    ((b.AppUsers?.DisplayName ?? "") + (b.AppUsers?.Login ?? "")).ToLowerInvariant().Contains(search));
            }

            if (genreId.HasValue)
                q = q.Where(b => b.Genres.Any(g => g.GenreId == genreId.Value));

            var list = q.Select(b => new
            {
                Book = b,
                Avg = b.Reviews.Any(r => !r.IsFrozen)
                    ? b.Reviews.Where(r => !r.IsFrozen).Average(r => (double)r.Rating)
                    : (double?)null
            }).ToList();

            if (sortRating)
                list = list.OrderByDescending(x => x.Avg ?? 0).ThenBy(x => x.Book.Title).ToList();
            else
                list = list.OrderBy(x => x.Book.Title).ToList();

            BooksHost.Items.Clear();
            foreach (var x in list)
                BooksHost.Items.Add(CreateCard(x.Book, x.Avg));
        }

        private UIElement CreateCard(Books b, double? avg)
        {
            var root = new Border
            {
                Width = 168,
                Margin = new Thickness(6),
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Background = Brushes.White,
                Effect = new DropShadowEffect
                {
                    BlurRadius = 14,
                    ShadowDepth = 4,
                    Opacity = 0.12
                },
                Cursor = Cursors.Hand,
                Tag = b.BookId
            };
            root.MouseLeftButtonUp += (s, ev) =>
            {
                if (s is Border bd && bd.Tag is int id)
                    NavigationService?.Navigate(new BookDetailPage(id));
            };

            var sp = new StackPanel { Margin = new Thickness(6) };
            var bi = ImageHelper.ToBitmapImage(b.CoverImage);
            if (bi != null)
            {
                sp.Children.Add(new Image { Height = 120, Stretch = Stretch.UniformToFill, Source = bi });
            }
            else
            {
                sp.Children.Add(new Border
                {
                    Height = 120,
                    Background = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                    Child = new TextBlock
                    {
                        Text = "Нет обложки",
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Foreground = Brushes.Gray
                    }
                });
            }

            sp.Children.Add(new TextBlock
            {
                Text = b.Title,
                TextWrapping = TextWrapping.Wrap,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 6, 0, 0)
            });

            sp.Children.Add(new TextBlock
            {
                Text = b.AppUsers?.DisplayName ?? "",
                Foreground = Brushes.Gray,
                FontSize = 12
            });

            sp.Children.Add(new TextBlock
            {
                Text = avg.HasValue ? $"Оценка: {avg:0.0}" : "Нет оценок",
                Margin = new Thickness(0, 4, 0, 0),
                FontSize = 12
            });

            if (b.IsFrozen)
            {
                sp.Children.Add(new TextBlock
                {
                    Text = "Заморожена",
                    Foreground = Brushes.DarkRed,
                    FontSize = 11,
                    Margin = new Thickness(0, 4, 0, 0)
                });
            }

            var shelfPanel = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
            var cb = new ComboBox { Height = 24, FontSize = 11 };
            cb.Items.Add(new ComboBoxItem { Content = "В список: Заброшено", Tag = ShelfTypes.Abandoned });
            cb.Items.Add(new ComboBoxItem { Content = "В планах", Tag = ShelfTypes.Planned });
            cb.Items.Add(new ComboBoxItem { Content = "Читаю", Tag = ShelfTypes.Reading });
            cb.Items.Add(new ComboBoxItem { Content = "Прочитано", Tag = ShelfTypes.Read });
            cb.SelectedIndex = 1;
            var addBtn = new Button { Content = "+", Width = 50, Margin = new Thickness(4, 0, 0, 0), Tag = b.BookId };
            addBtn.Click += (s, ev) =>
            {
                var uid = AppSession.CurrentUser.UserId;
                var shelf = (byte)((cb.SelectedItem as ComboBoxItem)?.Tag ?? ShelfTypes.Planned);
                var existing = Core.Context.UserBookShelves.FirstOrDefault(x => x.UserId == uid && x.BookId == b.BookId);
                if (existing == null)
                {
                    var shelfEntry = new UserBookShelves { UserId = uid, BookId = b.BookId, ShelfType = shelf };
                    Core.Context.UserBookShelves.Add(shelfEntry);
                }
                else
                    existing.ShelfType = shelf;
                Core.Context.SaveChanges();
                MessageBox.Show("Книга добавлена в выбранный список.", "Каталог", MessageBoxButton.OK, MessageBoxImage.Information);
            };
            var shelfDock = new DockPanel();
            DockPanel.SetDock(addBtn, Dock.Right);
            shelfDock.Children.Add(addBtn);
            shelfDock.Children.Add(cb);
            shelfPanel.Children.Add(shelfDock);
            sp.Children.Add(shelfPanel);

            root.Child = sp;
            return root;
        }
    }
}
