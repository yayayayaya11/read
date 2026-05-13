using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace libraryapp.Pages
{
    public partial class BookEditPage : Page
    {
        private readonly int _bookId;
        private byte[] _coverBytes;
        private readonly Dictionary<int, CheckBox> _genreChecks = new Dictionary<int, CheckBox>();

        public BookEditPage(int bookId)
        {
            _bookId = bookId;
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            GenreList.Items.Clear();
            _genreChecks.Clear();
            foreach (var g in Core.Context.Genres.OrderBy(x => x.Name).ToList())
            {
                var cb = new CheckBox { Content = g.Name, Tag = g.GenreId, Margin = new Thickness(0, 2, 0, 2) };
                _genreChecks[g.GenreId] = cb;
                GenreList.Items.Add(cb);
            }

            if (_bookId == 0)
            {
                TitleHeader.Text = "Новая книга";
                CoverInfo.Text = "Обложка не выбрана.";
                AppSession.ReloadCurrentUser();
                if (AppSession.IsFrozen)
                {
                    MessageBox.Show("Аккаунт заморожен: создавать новые книги нельзя.");
                    NavigationService?.GoBack();
                }
                return;
            }

            var book = Core.Context.Books
                .Include(b => b.Genres)
                .FirstOrDefault(b => b.BookId == _bookId);
            if (book == null)
            {
                MessageBox.Show("Книга не найдена.");
                NavigationService?.GoBack();
                return;
            }

            var uid = AppSession.CurrentUser.UserId;
            if (book.AuthorUserId != uid && !AppSession.IsAdmin)
            {
                MessageBox.Show("Нет прав на редактирование.");
                NavigationService?.GoBack();
                return;
            }

            TitleHeader.Text = "Редактирование книги";
            FTitle.Text = book.Title;
            FDescription.Text = book.Description;
            FFull.Text = book.FullText;
            _coverBytes = book.CoverImage;
            CoverInfo.Text = _coverBytes != null && _coverBytes.Length > 0 ? "Обложка загружена." : "Обложка не задана.";

            foreach (var gid in book.Genres.Select(g => g.GenreId))
            {
                if (_genreChecks.TryGetValue(gid, out var cb))
                    cb.IsChecked = true;
            }
        }

        private void PickCover_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "Изображения|*.png;*.jpg;*.jpeg;*.bmp|Все файлы|*.*" };
            if (dlg.ShowDialog() != true) return;
            _coverBytes = System.IO.File.ReadAllBytes(dlg.FileName);
            CoverInfo.Text = "Выбран файл: " + dlg.FileName;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var title = FTitle.Text?.Trim();
            if (string.IsNullOrEmpty(title))
            {
                MessageBox.Show("Укажите название.");
                return;
            }

            if (_bookId == 0)
            {
                AppSession.ReloadCurrentUser();
                if (AppSession.IsFrozen)
                {
                    MessageBox.Show("Аккаунт заморожен: создавать новые книги нельзя.");
                    return;
                }
                var book = new Books
                {
                    Title = title,
                    Description = FDescription.Text,
                    FullText = FFull.Text,
                    AuthorUserId = AppSession.CurrentUser.UserId,
                    CoverImage = _coverBytes,
                    IsFrozen = false
                };
                Core.Context.Books.Add(book);
                Core.Context.SaveChanges();
                ApplyGenres(book);
                Core.Context.SaveChanges();
                MessageBox.Show("Книга создана.");
                NavigationService?.Navigate(new AuthorPage());
                return;
            }

            var existing = Core.Context.Books.Include(b => b.Genres).First(b => b.BookId == _bookId);
            existing.Title = title;
            existing.Description = FDescription.Text;
            existing.FullText = FFull.Text;
            if (_coverBytes != null)
                existing.CoverImage = _coverBytes;

            foreach (var g in existing.Genres.ToList())
                existing.Genres.Remove(g);
            ApplyGenres(existing);
            Core.Context.SaveChanges();
            MessageBox.Show("Сохранено.");
            NavigationService?.Navigate(new AuthorPage());
        }

        private void ApplyGenres(Books book)
        {
            foreach (var kv in _genreChecks)
            {
                if (kv.Value.IsChecked != true) continue;
                var gEntity = Core.Context.Genres.First(x => x.GenreId == kv.Key);
                if (!book.Genres.Any(x => x.GenreId == kv.Key))
                    book.Genres.Add(gEntity);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new AuthorPage());
        }
    }
}
