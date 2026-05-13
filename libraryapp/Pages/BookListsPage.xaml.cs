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
    public partial class BookListsPage : Page
    {
        private byte _shelf;
        private bool _filterEventsAttached;

        public BookListsPage()
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

            if (!_filterEventsAttached)
            {
                SortBox.SelectionChanged += (_, __) => SortOrGenre_Changed(null, null);
                GenreBox.SelectionChanged += (_, __) => SortOrGenre_Changed(null, null);
                _filterEventsAttached = true;
            }

            Tabs.SelectedIndex = 1;
            _shelf = ShelfTypes.Planned;
            Reload();
        }

        private void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Tabs.SelectedItem is TabItem ti && ti.Tag != null && byte.TryParse(ti.Tag.ToString(), out var s))
            {
                _shelf = s;
                Reload();
            }
        }

        private void SortOrGenre_Changed(object sender, SelectionChangedEventArgs e) => Reload();

        private void ReloadClick(object sender, RoutedEventArgs e) => Reload();

        private void Reload()
        {
            var uid = AppSession.CurrentUser.UserId;
            var search = (SearchBox.Text ?? string.Empty).Trim().ToLowerInvariant();
            int? genreId = (GenreBox.SelectedItem as ComboBoxItem)?.Tag as int?;
            var sortRating = (SortBox.SelectedItem as ComboBoxItem)?.Tag as string == "rating";

            var shelfBookIds = Core.Context.UserBookShelves
                .Where(s => s.UserId == uid && s.ShelfType == _shelf)
                .Select(s => s.BookId)
                .ToList();

            var books = Core.Context.Books
                .Include(b => b.AppUsers)
                .Include(b => b.Genres)
                .Include(b => b.Reviews)
                .Where(b => shelfBookIds.Contains(b.BookId) && !b.IsFrozen)
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
                Width = 180,
                Margin = new Thickness(6),
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Background = Brushes.White,
                Cursor = Cursors.Hand,
                Tag = b.BookId,
                Effect = new DropShadowEffect
                {
                    BlurRadius = 14,
                    ShadowDepth = 4,
                    Opacity = 0.12
                }
            };
            var sp = new StackPanel { Margin = new Thickness(6) };
            var bi = ImageHelper.ToBitmapImage(b.CoverImage);
            if (bi != null)
                sp.Children.Add(new Image { Height = 110, Stretch = Stretch.UniformToFill, Source = bi });
            else
            {
                sp.Children.Add(new Border
                {
                    Height = 110,
                    Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                    Child = new TextBlock { Text = "Нет обложки", VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center }
                });
            }
            sp.Children.Add(new TextBlock { Text = b.Title, TextWrapping = TextWrapping.Wrap, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 6, 0, 0) });
            sp.Children.Add(new TextBlock { Text = b.AppUsers?.DisplayName ?? "", Foreground = Brushes.Gray, FontSize = 12 });
            sp.Children.Add(new TextBlock { Text = avg.HasValue ? $"Оценка: {avg:0.0}" : "Нет оценок", FontSize = 12, Margin = new Thickness(0, 4, 0, 0) });

            var moveRow = new DockPanel { Margin = new Thickness(0, 8, 0, 0) };
            var cb = new ComboBox();
            foreach (var opt in new[]
            {
                Tuple.Create("Заброшено", ShelfTypes.Abandoned),
                Tuple.Create("В планах", ShelfTypes.Planned),
                Tuple.Create("Читаю", ShelfTypes.Reading),
                Tuple.Create("Прочитано", ShelfTypes.Read)
            })
            {
                cb.Items.Add(new ComboBoxItem { Content = opt.Item1, Tag = opt.Item2 });
            }
            cb.SelectedIndex = _shelf;
            var mv = new Button { Content = "Переместить", Margin = new Thickness(6, 0, 0, 0) };
            mv.Click += (_, __) =>
            {
                var newShelf = (byte)((cb.SelectedItem as ComboBoxItem)?.Tag ?? _shelf);
                var uid = AppSession.CurrentUser.UserId;
                var row = Core.Context.UserBookShelves.First(x => x.UserId == uid && x.BookId == b.BookId);
                row.ShelfType = newShelf;
                Core.Context.SaveChanges();
                MessageBox.Show("Список обновлён.");
                Reload();
            };
            DockPanel.SetDock(mv, Dock.Right);
            moveRow.Children.Add(mv);
            moveRow.Children.Add(cb);
            sp.Children.Add(moveRow);

            root.MouseLeftButtonUp += (_, ev) =>
            {
                if (IsInteractiveNavigationSource(ev.OriginalSource as DependencyObject))
                    return;
                NavigationService?.Navigate(new BookDetailPage(b.BookId));
            };

            root.Child = sp;
            return root;
        }

        private static bool IsInteractiveNavigationSource(DependencyObject src)
        {
            while (src != null)
            {
                if (src is System.Windows.Controls.Primitives.ButtonBase || src is ComboBox || src is ComboBoxItem || src is TextBox)
                    return true;
                src = VisualTreeHelper.GetParent(src);
            }
            return false;
        }
    }
}
