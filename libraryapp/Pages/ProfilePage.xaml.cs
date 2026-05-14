using System;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace libraryapp.Pages
{
    public partial class ProfilePage : Page
    {
        public ProfilePage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e) => Reload();

        private void Reload()
        {
            Root.Children.Clear();
            var u = Core.Context.AppUsers.Include(x => x.Roles).First(x => x.UserId == AppSession.CurrentUser.UserId);

            Root.Children.Add(new TextBlock { Text = "Профиль", FontSize = 22, FontWeight = FontWeights.Bold });

            var nameRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 12, 0, 0) };
            nameRow.Children.Add(new TextBlock { Text = "Имя: " + u.DisplayName, VerticalAlignment = VerticalAlignment.Center });
            var editBtn = new Button { Content = "Редактировать", Margin = new Thickness(8, 0, 0, 0), HorizontalAlignment = HorizontalAlignment.Left };
            editBtn.Click += (_, __) =>
            {
                var newName = UiPrompts.AskMultiline("Новое имя", "Введите новое отображаемое имя");
                if (string.IsNullOrWhiteSpace(newName)) return;
                u.DisplayName = newName.Trim();
                Core.Context.SaveChanges();
                MessageBox.Show("Имя обновлено.");
                Reload();
            };
            nameRow.Children.Add(editBtn);
            Root.Children.Add(nameRow);

            Root.Children.Add(new TextBlock { Text = "Логин: " + u.Login, Margin = new Thickness(0, 4, 0, 0) });
            Root.Children.Add(new TextBlock { Text = "Электронная почта: " + u.Email, Margin = new Thickness(0, 4, 0, 0) });
            Root.Children.Add(new TextBlock { Text = "Роль: " + (u.Roles?.Name ?? ""), Margin = new Thickness(0, 4, 0, 0) });

            if (u.IsFrozen)
            {
                var warn = new Border
                {
                    Background = System.Windows.Media.Brushes.MistyRose,
                    Padding = new Thickness(10),
                    Margin = new Thickness(0, 16, 0, 0)
                };
                var sp = new StackPanel();
                sp.Children.Add(new TextBlock { Text = "Аккаунт заморожен.", FontWeight = FontWeights.Bold });
                sp.Children.Add(new TextBlock { Text = "Причина: " + (u.FreezeReason ?? "—"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 0) });
                var tb = new TextBox { MinHeight = 60, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0), ToolTip = "Обоснование для оспаривания" };
                sp.Children.Add(tb);
                var btn = new Button { Content = "Оспорить заморозку", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 6, 0, 0) };
                btn.Click += (_, __) =>
                {
                    if (string.IsNullOrWhiteSpace(tb.Text))
                    {
                        MessageBox.Show("Введите текст обращения.");
                        return;
                    }
                    var fd = new FreezeDisputes
                    {
                        DisputeKind = DisputeKinds.Account,
                        TargetUserId = u.UserId,
                        RequesterUserId = u.UserId,
                        Message = tb.Text.Trim(),
                        Status = RequestStatus.Pending,
                        CreatedUtc = DateTime.UtcNow
                    };
                    Core.Context.FreezeDisputes.Add(fd);
                    Core.Context.SaveChanges();
                    MessageBox.Show("Заявка отправлена администратору.");
                };
                sp.Children.Add(btn);
                warn.Child = sp;
                Root.Children.Add(warn);
            }

            Root.Children.Add(new Separator { Margin = new Thickness(0, 20, 0, 20) });
            Root.Children.Add(new TextBlock { Text = "Мои отзывы", FontSize = 18, FontWeight = FontWeights.SemiBold });

            var reviews = Core.Context.Reviews
                .Include(r => r.Books)
                .Where(r => r.UserId == u.UserId)
                .OrderByDescending(r => r.ReviewId)
                .ToList();

            var cellTextStyle = new Style(typeof(TextBlock));
            cellTextStyle.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap));
            cellTextStyle.Setters.Add(new Setter(TextBlock.TextTrimmingProperty, TextTrimming.None));
            cellTextStyle.Setters.Add(new Setter(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center));

            var dg = new DataGrid
            {
                ItemsSource = reviews,
                AutoGenerateColumns = false,
                IsReadOnly = true,
                MinHeight = 160,
                MaxHeight = 420,
                Margin = new Thickness(0, 8, 0, 0),
                CanUserResizeColumns = true,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            dg.Columns.Add(new DataGridTextColumn
            {
                Header = "Книга",
                Binding = new System.Windows.Data.Binding("Books.Title") { TargetNullValue = "—" },
                Width = new DataGridLength(2, DataGridLengthUnitType.Star),
                MinWidth = 160,
                ElementStyle = cellTextStyle
            });
            dg.Columns.Add(new DataGridTextColumn
            {
                Header = "Оценка",
                Binding = new System.Windows.Data.Binding("Rating"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Auto),
                MinWidth = 72,
                ElementStyle = cellTextStyle
            });
            dg.Columns.Add(new DataGridTextColumn
            {
                Header = "Комментарий",
                Binding = new System.Windows.Data.Binding("Comment"),
                Width = new DataGridLength(3, DataGridLengthUnitType.Star),
                MinWidth = 200,
                ElementStyle = cellTextStyle
            });
            Root.Children.Add(dg);

            if (u.RoleId == RoleIds.Reader)
            {
                Root.Children.Add(new Separator { Margin = new Thickness(0, 20, 0, 20) });
                Root.Children.Add(new TextBlock { Text = "Заявка на роль автора", FontSize = 18, FontWeight = FontWeights.SemiBold });
                var hasPending = Core.Context.AuthorRoleRequests.Any(r => r.UserId == u.UserId && r.Status == RequestStatus.Pending);
                if (hasPending)
                {
                    Root.Children.Add(new TextBlock { Text = "Заявка уже на рассмотрении.", Margin = new Thickness(0, 8, 0, 0), Foreground = System.Windows.Media.Brushes.DarkOrange });
                }
                else
                {
                    var msg = new TextBox { MinHeight = 80, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) };
                    Root.Children.Add(msg);
                    var apply = new Button { Content = "Подать заявку", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 8, 0, 0) };
                    apply.Click += (_, __) =>
                    {
                        var req = new AuthorRoleRequests
                        {
                            UserId = u.UserId,
                            Message = msg.Text,
                            Status = RequestStatus.Pending,
                            CreatedUtc = DateTime.UtcNow
                        };
                        Core.Context.AuthorRoleRequests.Add(req);
                        Core.Context.SaveChanges();
                        MessageBox.Show("Заявка отправлена.");
                        Reload();
                    };
                    Root.Children.Add(apply);
                }
            }
        }
    }
}
