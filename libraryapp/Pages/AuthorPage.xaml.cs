using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace libraryapp.Pages
{
    public partial class AuthorPage : Page
    {
        public AuthorPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e) => Reload();

        private void Reload()
        {
            Root.Children.Clear();
            AppSession.ReloadCurrentUser();
            var uid = AppSession.CurrentUser.UserId;

            Root.Children.Add(new TextBlock { Text = "Кабинет автора", FontSize = 22, FontWeight = FontWeights.Bold });
            var add = new Button { Content = "Добавить новую книгу", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 12, 0, 0) };
            add.Click += (_, __) => NavigationService?.Navigate(new BookEditPage(0));
            if (AppSession.IsFrozen)
            {
                add.IsEnabled = false;
                add.ToolTip = "Аккаунт заморожен: добавление новых книг недоступно.";
            }
            Root.Children.Add(add);
            if (AppSession.IsFrozen)
            {
                Root.Children.Add(new TextBlock
                {
                    Text = "Ваш аккаунт заморожен администратором. Добавление новых книг недоступно до снятия заморозки.",
                    Foreground = System.Windows.Media.Brushes.DarkRed,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 8, 0, 0),
                    MaxWidth = 640
                });
            }

            Root.Children.Add(new TextBlock { Text = "Опубликованные книги", FontSize = 18, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 20, 0, 0) });

            var books = Core.Context.Books.Where(b => b.AuthorUserId == uid && !b.IsFrozen).OrderBy(b => b.Title).ToList();
            var lb = new ListBox { ItemsSource = books, DisplayMemberPath = "Title", Margin = new Thickness(0, 8, 0, 0), MaxHeight = 200 };
            Root.Children.Add(lb);
            var edit = new Button { Content = "Редактировать выбранную", Margin = new Thickness(0, 6, 0, 0), HorizontalAlignment = HorizontalAlignment.Left };
            edit.Click += (_, __) =>
            {
                if (lb.SelectedItem is Books b)
                    NavigationService?.Navigate(new BookEditPage(b.BookId));
            };
            Root.Children.Add(edit);

            Root.Children.Add(new TextBlock { Text = "Замороженные книги", FontSize = 18, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 20, 0, 0) });
            var frozen = Core.Context.Books.Where(b => b.AuthorUserId == uid && b.IsFrozen).ToList();
            if (!frozen.Any())
            {
                Root.Children.Add(new TextBlock { Text = "Нет замороженных книг.", Margin = new Thickness(0, 8, 0, 0), Foreground = System.Windows.Media.Brushes.Gray });
            }
            else
            {
                foreach (var b in frozen)
                {
                    var bookId = b.BookId;
                    var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 0) };
                    row.Children.Add(new TextBlock { Text = b.Title + " — " + (b.FreezeReason ?? ""), VerticalAlignment = VerticalAlignment.Center, Width = 400, TextWrapping = TextWrapping.Wrap });
                    var dispute = new Button { Content = "Оспорить", Margin = new Thickness(8, 0, 0, 0) };
                    dispute.Click += (_, __) =>
                    {
                        var text = UiPrompts.AskMultiline("Оспаривание заморозки книги", "Опишите причину обращения");
                        if (string.IsNullOrWhiteSpace(text)) return;
                        var fd = new FreezeDisputes
                        {
                            DisputeKind = DisputeKinds.Book,
                            TargetBookId = bookId,
                            RequesterUserId = uid,
                            Message = text.Trim(),
                            Status = RequestStatus.Pending,
                            CreatedUtc = DateTime.UtcNow
                        };
                        Core.Context.FreezeDisputes.Add(fd);
                        Core.Context.SaveChanges();
                        MessageBox.Show("Заявка отправлена.");
                        Reload();
                    };
                    row.Children.Add(dispute);
                    Root.Children.Add(row);
                }
            }
        }
    }
}
