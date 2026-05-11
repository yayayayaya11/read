using System;
using System.Data.Entity;
using System.Linq;

namespace libraryapp
{
    public static class Seed
    {
        public static void Ensure()
        {
            var db = Core.Context;

            if (!db.Roles.Any())
            {
                db.Roles.Add(new Roles { Name = "Читатель" });
                db.Roles.Add(new Roles { Name = "Автор" });
                db.Roles.Add(new Roles { Name = "Администратор" });
                db.SaveChanges();
            }

            if (!db.Genres.Any())
            {
                db.Genres.Add(new Genres { Name = "Фантастика" });
                db.Genres.Add(new Genres { Name = "Детектив" });
                db.Genres.Add(new Genres { Name = "Роман" });
                db.SaveChanges();
            }

            if (!db.AppUsers.Any(u => u.Login == "admin"))
            {
                var admin = new AppUsers
                {
                    Login = "admin",
                    PasswordHash = PasswordHelper.Hash("admin"),
                    Email = "admin@local",
                    DisplayName = "Администратор",
                    RoleId = RoleIds.Admin,
                    IsFrozen = false
                };
                db.AppUsers.Add(admin);
                db.SaveChanges();
            }

            var demoAuthor = db.AppUsers.FirstOrDefault(u => u.Login == "demoauthor");
            if (demoAuthor == null)
            {
                demoAuthor = new AppUsers
                {
                    Login = "demoauthor",
                    PasswordHash = PasswordHelper.Hash("author"),
                    Email = "author@local",
                    DisplayName = "Демо Автор",
                    RoleId = RoleIds.Author,
                    IsFrozen = false
                };
                db.AppUsers.Add(demoAuthor);
                db.SaveChanges();
            }

            if (!db.Books.Any())
            {
                var g1 = db.Genres.First();
                var g2 = db.Genres.OrderBy(x => x.GenreId).Skip(1).FirstOrDefault() ?? g1;

                var b1 = new Books
                {
                    Title = "Введение в космические путешествия",
                    Description = "Учебное пособие для начинающих путешественников во Вселенной.",
                    AuthorUserId = demoAuthor.UserId,
                    FullText = "Глава 1. Космос вокруг нас...\n\n(Демонстрационный текст для чтения в приложении.)",
                    IsFrozen = false
                };
                var b2 = new Books
                {
                    Title = "Тайна старого маяка",
                    Description = "Детективная история на побережье.",
                    AuthorUserId = demoAuthor.UserId,
                    FullText = "Пролог. Ночь была тёмной...\n\n(Демонстрационный текст.)",
                    IsFrozen = false
                };
                db.Books.Add(b1);
                db.Books.Add(b2);
                db.SaveChanges();

                b1.Genres.Add(g1);
                b2.Genres.Add(g2);
                db.SaveChanges();
            }
        }
    }
}
