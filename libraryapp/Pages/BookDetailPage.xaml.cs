using System;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace libraryapp.Pages
{
    public partial class BookDetailPage : Page
    {
        private readonly int _bookId;

        public BookDetailPage(int bookId)
        {
            _bookId = bookId;
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            RootPanel.Children.Clear();
            var book = Core.Context.Books
                .Include(b => b.AppUsers)
                .Include(b => b.Genres)
                .Include(b => b.Reviews.Select(r => r.AppUsers))
                .FirstOrDefault(b => b.BookId == _bookId);

            if (book == null)
            {
                RootPanel.Children.Add(new TextBlock { Text = "Книга не найдена.", FontSize = 16 });
                return;
            }

            if (book.IsFrozen)
            {
                RootPanel.Children.Add(new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(254, 226, 226)),
                    Padding = new Thickness(10),
                    Margin = new Thickness(0, 0, 0, 12),
                    Child = new TextBlock
                    {
                        TextWrapping = TextWrapping.Wrap,
                        Text = "Книга заморожена администратором. Причина: " + (book.FreezeReason ?? "—")
                    }
                });
            }

            var header = new DockPanel();
            var cover = new Image { Width = 160, Height = 220, Stretch = Stretch.Uniform, Margin = new Thickness(0, 0, 16, 0) };
            var bmp = ImageHelper.ToBitmapImage(book.CoverImage);
            if (bmp != null) cover.Source = bmp;
            DockPanel.SetDock(cover, Dock.Left);
            header.Children.Add(cover);

            var info = new StackPanel();
            info.Children.Add(new TextBlock { Text = book.Title, FontSize = 22, FontWeight = FontWeights.Bold, TextWrapping = TextWrapping.Wrap });
            info.Children.Add(new TextBlock { Text = "Автор: " + (book.AppUsers?.DisplayName ?? ""), Margin = new Thickness(0, 8, 0, 0), FontSize = 14 });
            var genres = string.Join(", ", book.Genres.Select(g => g.Name));
            info.Children.Add(new TextBlock { Text = "Жанры: " + (string.IsNullOrEmpty(genres) ? "—" : genres), Margin = new Thickness(0, 4, 0, 0) });
            var avg = book.Reviews.Any(r => !r.IsFrozen)
                ? book.Reviews.Where(r => !r.IsFrozen).Average(r => r.Rating)
                : (double?)null;
            info.Children.Add(new TextBlock
            {
                Text = avg.HasValue ? $"Средняя оценка: {avg:0.00}" : "Оценок пока нет",
                Margin = new Thickness(0, 8, 0, 0)
            });
            info.Children.Add(new TextBlock { Text = book.Description ?? "", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 12, 0, 0) });
            header.Children.Add(info);
            RootPanel.Children.Add(header);

            var readExp = new Expander { Header = "Читать текст", Margin = new Thickness(0, 16, 0, 0), IsExpanded = false };
            readExp.Content = new TextBox
            {
                Text = book.FullText ?? "",
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                MinHeight = 200,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            RootPanel.Children.Add(readExp);

            var act = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 16, 0, 0) };
            var complainBook = new Button { Content = "Жалоба на книгу", Margin = new Thickness(0, 0, 8, 0) };
            complainBook.Click += (_, __) => SubmitComplaint(ComplaintKinds.Book, book.BookId, book.AuthorUserId, null);
            act.Children.Add(complainBook);

            var complainAuthor = new Button { Content = "Жалоба на автора", Margin = new Thickness(0, 0, 8, 0) };
            complainAuthor.Click += (_, __) => SubmitComplaint(ComplaintKinds.Author, null, book.AuthorUserId, null);
            act.Children.Add(complainAuthor);

            if (AppSession.IsAdmin)
            {
                var freezeBook = new Button { Content = "Заморозить книгу", Margin = new Thickness(16, 0, 0, 0) };
                freezeBook.Click += (_, __) =>
                {
                    var reason = UiPrompts.AskMultiline("Причина заморозки книги");
                    if (string.IsNullOrWhiteSpace(reason)) return;
                    book.IsFrozen = true;
                    book.FreezeReason = reason;
                    Core.Context.SaveChanges();
                    MessageBox.Show("Книга заморожена.");
                    NavigationService?.Navigate(new BookDetailPage(book.BookId));
                };
                act.Children.Add(freezeBook);
            }

            RootPanel.Children.Add(act);

            RootPanel.Children.Add(new Separator { Margin = new Thickness(0, 16, 0, 16) });
            RootPanel.Children.Add(new TextBlock { Text = "Отзывы", FontSize = 18, FontWeight = FontWeights.SemiBold });

            if (!AppSession.IsFrozen)
            {
                var addPanel = new StackPanel { Margin = new Thickness(0, 8, 0, 8) };
                addPanel.Children.Add(new TextBlock { Text = "Ваш отзыв (оценка 1–5 и комментарий)" });
                var ratingBox = new ComboBox { Width = 80, Margin = new Thickness(0, 4, 0, 0) };
                for (var i = 1; i <= 5; i++) ratingBox.Items.Add(i);
                ratingBox.SelectedIndex = 4;
                addPanel.Children.Add(ratingBox);
                var comment = new TextBox { MinHeight = 60, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0) };
                addPanel.Children.Add(comment);
                var send = new Button { Content = "Отправить отзыв", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 6, 0, 0) };
                send.Click += (_, __) =>
                {
                    var r = ratingBox.SelectedIndex + 1;
                    var rev = new Reviews
                    {
                        BookId = book.BookId,
                        UserId = AppSession.CurrentUser.UserId,
                        Rating = r,
                        Comment = comment.Text,
                        IsFrozen = false
                    };
                    Core.Context.Reviews.Add(rev);
                    Core.Context.SaveChanges();
                    MessageBox.Show("Отзыв добавлен.");
                    NavigationService?.Navigate(new BookDetailPage(book.BookId));
                };
                addPanel.Children.Add(send);
                RootPanel.Children.Add(addPanel);
            }

            foreach (var rev in book.Reviews.OrderByDescending(r => r.ReviewId))
            {
                var box = new Border
                {
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(8),
                    Margin = new Thickness(0, 6, 0, 0)
                };
                var sp = new StackPanel();
                sp.Children.Add(new TextBlock { Text = (rev.AppUsers?.DisplayName ?? "Пользователь") + " — оценка: " + rev.Rating, FontWeight = FontWeights.SemiBold });
                if (rev.IsFrozen)
                    sp.Children.Add(new TextBlock { Text = "Отзыв заморожен: " + (rev.FreezeReason ?? ""), Foreground = Brushes.DarkRed, FontSize = 12 });
                sp.Children.Add(new TextBlock { Text = rev.Comment ?? "", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0) });

                var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 0) };
                var cRev = new Button { Content = "Пожаловаться на отзыв", Margin = new Thickness(0, 0, 8, 0) };
                cRev.Click += (_, __) => SubmitComplaint(ComplaintKinds.Review, null, null, rev.ReviewId);
                row.Children.Add(cRev);

                if (AppSession.IsAdmin)
                {
                    var fr = new Button { Content = "Заморозить отзыв" };
                    var revId = rev.ReviewId;
                    fr.Click += (_, __) =>
                    {
                        var reason = UiPrompts.AskMultiline("Причина заморозки отзыва");
                        if (string.IsNullOrWhiteSpace(reason)) return;
                        var r2 = Core.Context.Reviews.First(x => x.ReviewId == revId);
                        r2.IsFrozen = true;
                        r2.FreezeReason = reason;
                        Core.Context.SaveChanges();
                        NavigationService?.Navigate(new BookDetailPage(book.BookId));
                    };
                    row.Children.Add(fr);
                }
                sp.Children.Add(row);
                box.Child = sp;
                RootPanel.Children.Add(box);
            }
        }

        private static void SubmitComplaint(byte kind, int? bookId, int? authorId, int? reviewId)
        {
            var text = UiPrompts.AskMultiline("Текст жалобы");
            if (string.IsNullOrWhiteSpace(text)) return;
            var c = new Complaints
            {
                TargetKind = kind,
                BookId = bookId,
                AuthorUserId = authorId,
                ReviewId = reviewId,
                ComplainantUserId = AppSession.CurrentUser.UserId,
                Description = text.Trim(),
                Status = RequestStatus.Pending,
                CreatedUtc = DateTime.UtcNow
            };
            Core.Context.Complaints.Add(c);
            Core.Context.SaveChanges();
            MessageBox.Show("Жалоба отправлена.");
        }
    }
}
