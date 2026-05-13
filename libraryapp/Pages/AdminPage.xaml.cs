using System;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace libraryapp.Pages
{
    public partial class AdminPage : Page
    {
        public AdminPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (!AppSession.IsAdmin)
            {
                MessageBox.Show("Доступ запрещён.");
                NavigationService?.GoBack();
                return;
            }
            Rebuild();
        }

        private void Rebuild()
        {
            TabComplaints.Content = BuildComplaints();
            TabDisputes.Content = BuildDisputes();
            TabAuthorReq.Content = BuildAuthorRequests();
            TabFrozen.Content = BuildFrozenSummary();
            TabUsers.Content = BuildUsers();
        }

        private UIElement BuildComplaints()
        {
            var root = new StackPanel { Margin = new Thickness(8) };
            var refresh = new Button { Content = "Обновить список", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 8), Padding = new Thickness(12, 4, 12, 4) };
            refresh.Click += (_, __) => Rebuild();
            root.Children.Add(refresh);

            foreach (var c in Core.Context.Complaints.Where(x => x.Status == RequestStatus.Pending).OrderBy(x => x.ComplaintId).ToList())
            {
                var row = new Border { BorderBrush = System.Windows.Media.Brushes.LightGray, BorderThickness = new Thickness(1), Padding = new Thickness(8), Margin = new Thickness(0, 0, 0, 8) };
                var sp = new StackPanel();
                sp.Children.Add(new TextBlock { Text = FormatComplaint(c), TextWrapping = TextWrapping.Wrap });
                var btns = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
                var id = c.ComplaintId;
                var accept = new Button { Content = "Принять", Margin = new Thickness(0, 0, 8, 0) };
                accept.Click += (_, __) =>
                {
                    var entity = Core.Context.Complaints.First(x => x.ComplaintId == id);
                    entity.Status = RequestStatus.Accepted;
                    if (entity.TargetKind == ComplaintKinds.Book && entity.BookId.HasValue)
                    {
                        var b = Core.Context.Books.FirstOrDefault(x => x.BookId == entity.BookId.Value);
                        if (b != null)
                        {
                            b.IsFrozen = true;
                            var desc = entity.Description ?? "";
                            b.FreezeReason = "Жалоба: " + desc.Substring(0, Math.Min(200, desc.Length));
                        }
                    }
                    else if (entity.TargetKind == ComplaintKinds.Author && entity.AuthorUserId.HasValue)
                    {
                        var u = Core.Context.AppUsers.FirstOrDefault(x => x.UserId == entity.AuthorUserId.Value);
                        if (u != null)
                        {
                            u.IsFrozen = true;
                            u.FreezeReason = entity.Description;
                            u.FrozenAt = DateTime.UtcNow;
                        }
                    }
                    else if (entity.TargetKind == ComplaintKinds.Review && entity.ReviewId.HasValue)
                    {
                        var r = Core.Context.Reviews.FirstOrDefault(x => x.ReviewId == entity.ReviewId.Value);
                        if (r != null)
                        {
                            r.IsFrozen = true;
                            r.FreezeReason = entity.Description;
                        }
                    }
                    Core.Context.SaveChanges();
                    Rebuild();
                };
                var reject = new Button { Content = "Отклонить" };
                reject.Click += (_, __) =>
                {
                    var entity = Core.Context.Complaints.First(x => x.ComplaintId == id);
                    entity.Status = RequestStatus.Rejected;
                    Core.Context.SaveChanges();
                    Rebuild();
                };
                btns.Children.Add(accept);
                btns.Children.Add(reject);
                sp.Children.Add(btns);
                row.Child = sp;
                root.Children.Add(row);
            }
            if (root.Children.Count == 1)
                root.Children.Add(new TextBlock { Text = "Нет ожидающих жалоб.", Foreground = System.Windows.Media.Brushes.Gray });
            return new ScrollViewer { Content = root, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        }

        private static string FormatComplaint(Complaints c)
        {
            var kind = c.TargetKind == ComplaintKinds.Book ? "Книга" : c.TargetKind == ComplaintKinds.Author ? "Автор" : "Отзыв";
            var target = "";
            if (c.TargetKind == ComplaintKinds.Book && c.BookId.HasValue)
            {
                var b = Core.Context.Books.Include(x => x.AppUsers).FirstOrDefault(x => x.BookId == c.BookId.Value);
                if (b != null)
                    target = $"Жалоба на книгу: «{b.Title}» (автор: {b.AppUsers?.DisplayName ?? b.AppUsers?.Login ?? "—"})";
                else
                    target = $"Жалоба на книгу (id #{c.BookId.Value}, запись не найдена)";
            }
            else if (c.TargetKind == ComplaintKinds.Author && c.AuthorUserId.HasValue)
            {
                var a = Core.Context.AppUsers.FirstOrDefault(x => x.UserId == c.AuthorUserId.Value);
                if (a != null)
                    target = $"Жалоба на автора: {a.DisplayName} ({a.Login})";
                else
                    target = $"Жалоба на автора (id пользователя #{c.AuthorUserId.Value}, запись не найдена)";
            }
            else if (c.TargetKind == ComplaintKinds.Review && c.ReviewId.HasValue)
            {
                var r = Core.Context.Reviews.Include(x => x.Books).Include(x => x.AppUsers).FirstOrDefault(x => x.ReviewId == c.ReviewId.Value);
                if (r != null)
                    target = $"Жалоба на отзыв к книге «{r.Books?.Title ?? "—"}» (автор отзыва: {r.AppUsers?.DisplayName ?? r.AppUsers?.Login ?? "—"})";
                else
                    target = $"Жалоба на отзыв (id #{c.ReviewId.Value}, запись не найдена)";
            }

            return (string.IsNullOrEmpty(target) ? "" : target + "\r\n") +
                   $"#{c.ComplaintId} [{kind}] от пользователя #{c.ComplainantUserId}\r\n" +
                   c.Description;
        }

        private UIElement BuildDisputes()
        {
            var root = new StackPanel { Margin = new Thickness(8) };
            var refresh = new Button { Content = "Обновить список", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 8), Padding = new Thickness(12, 4, 12, 4) };
            refresh.Click += (_, __) => Rebuild();
            root.Children.Add(refresh);

            foreach (var d in Core.Context.FreezeDisputes.Where(x => x.Status == RequestStatus.Pending).OrderBy(x => x.DisputeId).ToList())
            {
                var row = new Border { BorderBrush = System.Windows.Media.Brushes.LightGray, BorderThickness = new Thickness(1), Padding = new Thickness(8), Margin = new Thickness(0, 0, 0, 8) };
                var sp = new StackPanel();
                var kind = d.DisputeKind == DisputeKinds.Book ? "Книга" : d.DisputeKind == DisputeKinds.Account ? "Аккаунт" : "Отзыв";
                sp.Children.Add(new TextBlock { Text = $"#{d.DisputeId} [{kind}] заявитель #{d.RequesterUserId}\r\n{d.Message}", TextWrapping = TextWrapping.Wrap });
                var btns = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
                var id = d.DisputeId;
                var accept = new Button { Content = "Принять (снять заморозку)", Margin = new Thickness(0, 0, 8, 0) };
                accept.Click += (_, __) =>
                {
                    var fd = Core.Context.FreezeDisputes.First(x => x.DisputeId == id);
                    fd.Status = RequestStatus.Accepted;
                    if (fd.DisputeKind == DisputeKinds.Book && fd.TargetBookId.HasValue)
                    {
                        var b = Core.Context.Books.FirstOrDefault(x => x.BookId == fd.TargetBookId.Value);
                        if (b != null)
                        {
                            b.IsFrozen = false;
                            b.FreezeReason = null;
                        }
                    }
                    else if (fd.DisputeKind == DisputeKinds.Review && fd.TargetReviewId.HasValue)
                    {
                        var r = Core.Context.Reviews.FirstOrDefault(x => x.ReviewId == fd.TargetReviewId.Value);
                        if (r != null)
                        {
                            r.IsFrozen = false;
                            r.FreezeReason = null;
                        }
                    }
                    else if (fd.DisputeKind == DisputeKinds.Account && fd.TargetUserId.HasValue)
                    {
                        var u = Core.Context.AppUsers.FirstOrDefault(x => x.UserId == fd.TargetUserId.Value);
                        if (u != null)
                        {
                            u.IsFrozen = false;
                            u.FreezeReason = null;
                            u.FrozenAt = null;
                        }
                    }
                    Core.Context.SaveChanges();
                    Rebuild();
                };
                var reject = new Button { Content = "Отклонить" };
                reject.Click += (_, __) =>
                {
                    var fd = Core.Context.FreezeDisputes.First(x => x.DisputeId == id);
                    fd.Status = RequestStatus.Rejected;
                    Core.Context.SaveChanges();
                    Rebuild();
                };
                btns.Children.Add(accept);
                btns.Children.Add(reject);
                sp.Children.Add(btns);
                row.Child = sp;
                root.Children.Add(row);
            }
            if (root.Children.Count == 1)
                root.Children.Add(new TextBlock { Text = "Нет заявок.", Foreground = System.Windows.Media.Brushes.Gray });
            return new ScrollViewer { Content = root, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        }

        private UIElement BuildAuthorRequests()
        {
            var root = new StackPanel { Margin = new Thickness(8) };
            var refresh = new Button { Content = "Обновить список", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 8), Padding = new Thickness(12, 4, 12, 4) };
            refresh.Click += (_, __) => Rebuild();
            root.Children.Add(refresh);

            foreach (var r in Core.Context.AuthorRoleRequests.Where(x => x.Status == RequestStatus.Pending).OrderBy(x => x.RequestId).ToList())
            {
                var row = new Border { BorderBrush = System.Windows.Media.Brushes.LightGray, BorderThickness = new Thickness(1), Padding = new Thickness(8), Margin = new Thickness(0, 0, 0, 8) };
                var sp = new StackPanel();
                sp.Children.Add(new TextBlock { Text = $"Пользователь #{r.UserId}\r\n{r.Message}", TextWrapping = TextWrapping.Wrap });
                var rid = r.RequestId;
                var uid = r.UserId;
                var btns = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
                var accept = new Button { Content = "Принять", Margin = new Thickness(0, 0, 8, 0) };
                accept.Click += (_, __) =>
                {
                    var req = Core.Context.AuthorRoleRequests.First(x => x.RequestId == rid);
                    req.Status = RequestStatus.Accepted;
                    var user = Core.Context.AppUsers.First(x => x.UserId == uid);
                    user.RoleId = RoleIds.Author;
                    Core.Context.SaveChanges();
                    Rebuild();
                };
                var reject = new Button { Content = "Отклонить" };
                reject.Click += (_, __) =>
                {
                    var req = Core.Context.AuthorRoleRequests.First(x => x.RequestId == rid);
                    req.Status = RequestStatus.Rejected;
                    Core.Context.SaveChanges();
                    Rebuild();
                };
                btns.Children.Add(accept);
                btns.Children.Add(reject);
                sp.Children.Add(btns);
                row.Child = sp;
                root.Children.Add(row);
            }
            if (root.Children.Count == 1)
                root.Children.Add(new TextBlock { Text = "Нет заявок.", Foreground = System.Windows.Media.Brushes.Gray });
            return new ScrollViewer { Content = root, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        }

        private UIElement BuildFrozenSummary()
        {
            var root = new StackPanel { Margin = new Thickness(8) };
            var refresh = new Button { Content = "Обновить", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 8), Padding = new Thickness(12, 4, 12, 4) };
            refresh.Click += (_, __) => Rebuild();
            root.Children.Add(refresh);

            root.Children.Add(new TextBlock { Text = "Замороженные книги", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 8, 0, 4) });
            foreach (var b in Core.Context.Books.Include(x => x.AppUsers).Where(x => x.IsFrozen).ToList())
                root.Children.Add(new TextBlock { Text = $"#{b.BookId} {b.Title} (автор: {b.AppUsers?.DisplayName}) — {b.FreezeReason}", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 0) });

            root.Children.Add(new TextBlock { Text = "Замороженные пользователи", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 16, 0, 4) });
            foreach (var u in Core.Context.AppUsers.Where(x => x.IsFrozen).ToList())
                root.Children.Add(new TextBlock { Text = $"#{u.UserId} {u.Login} — {u.FreezeReason}", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 0) });

            root.Children.Add(new TextBlock { Text = "Замороженные отзывы", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 16, 0, 4) });
            foreach (var r in Core.Context.Reviews.Include(x => x.Books).Where(x => x.IsFrozen).ToList())
                root.Children.Add(new TextBlock { Text = $"#{r.ReviewId} по книге «{r.Books?.Title}» — {r.FreezeReason}", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 0) });

            return new ScrollViewer { Content = root, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        }

        private UIElement BuildUsers()
        {
            var root = new StackPanel { Margin = new Thickness(8) };
            var refresh = new Button { Content = "Обновить", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 8), Padding = new Thickness(12, 4, 12, 4) };
            refresh.Click += (_, __) => Rebuild();
            root.Children.Add(refresh);

            foreach (var u in Core.Context.AppUsers.Include(x => x.Roles).OrderBy(x => x.UserId).ToList())
            {
                var row = new Border { BorderBrush = System.Windows.Media.Brushes.LightGray, BorderThickness = new Thickness(1), Padding = new Thickness(8), Margin = new Thickness(0, 0, 0, 8) };
                var sp = new StackPanel();
                var sb = new StringBuilder();
                sb.AppendLine($"#{u.UserId} {u.Login} — {u.DisplayName}");
                sb.AppendLine($"Почта: {u.Email}, роль: {u.Roles?.Name}");
                sp.Children.Add(new TextBlock { Text = sb.ToString(), TextWrapping = TextWrapping.Wrap });

                var roleRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
                var cb = new ComboBox { Width = 200 };
                cb.Items.Add(new ComboBoxItem { Content = "Читатель", Tag = RoleIds.Reader });
                cb.Items.Add(new ComboBoxItem { Content = "Автор", Tag = RoleIds.Author });
                cb.Items.Add(new ComboBoxItem { Content = "Администратор", Tag = RoleIds.Admin });
                cb.SelectedIndex = Math.Max(0, u.RoleId - 1);
                var setRole = new Button { Content = "Назначить роль", Margin = new Thickness(8, 0, 0, 0) };
                var uid = u.UserId;
                var isSelf = AppSession.CurrentUser != null && uid == AppSession.CurrentUser.UserId;
                if (isSelf)
                {
                    cb.IsEnabled = false;
                    setRole.IsEnabled = false;
                    setRole.ToolTip = "Нельзя изменить свою собственную роль.";
                    sp.Children.Add(new TextBlock
                    {
                        Text = "Смена собственной роли недоступна.",
                        Foreground = System.Windows.Media.Brushes.DimGray,
                        FontSize = 12,
                        Margin = new Thickness(0, 0, 0, 4)
                    });
                }
                else
                {
                    setRole.Click += (_, __) =>
                    {
                        if (AppSession.CurrentUser != null && uid == AppSession.CurrentUser.UserId)
                        {
                            MessageBox.Show("Нельзя изменить свою собственную роль.");
                            return;
                        }
                        var newRole = (int)((cb.SelectedItem as ComboBoxItem)?.Tag ?? RoleIds.Reader);
                        var user = Core.Context.AppUsers.First(x => x.UserId == uid);
                        user.RoleId = newRole;
                        Core.Context.SaveChanges();
                        MessageBox.Show("Роль обновлена.");
                        Rebuild();
                    };
                }
                roleRow.Children.Add(cb);
                roleRow.Children.Add(setRole);
                sp.Children.Add(roleRow);

                var pwd = new Button { Content = "Сменить пароль", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 8, 0, 0) };
                pwd.Click += (_, __) =>
                {
                    var np = UiPrompts.AskPassword("Новый пароль для " + u.Login);
                    if (string.IsNullOrEmpty(np)) return;
                    var user = Core.Context.AppUsers.First(x => x.UserId == uid);
                    user.PasswordHash = PasswordHelper.Hash(np);
                    Core.Context.SaveChanges();
                    MessageBox.Show("Пароль изменён.");
                };
                sp.Children.Add(pwd);

                row.Child = sp;
                root.Children.Add(row);
            }
            return new ScrollViewer { Content = root, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        }
    }
}
