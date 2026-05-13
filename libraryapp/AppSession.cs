using System.Linq;

namespace libraryapp
{
    /// <summary>
    /// —татический класс дл€ управлени€ сессией текущего пользовател€ приложени€.
    /// </summary>
    public static class AppSession
    {
        /// <summary>
        /// “екущий авторизованный пользователь.
        /// </summary>
        public static AppUsers CurrentUser { get; private set; }

        public static void SetUser(AppUsers user)
        {
            CurrentUser = user;
        }

        /// <summary>
        /// ѕерезагружает данные текущего пользовател€ из базы данных.
        /// </summary>
        public static void ClearUser()
        {
            CurrentUser = null;
        }

        public static void ReloadCurrentUser()
        {
            if (CurrentUser == null) return;
            CurrentUser = Core.Context.AppUsers.FirstOrDefault(u => u.UserId == CurrentUser.UserId);
        }

        /// <summary>
        /// ѕровер€ет, €вл€етс€ ли текущий пользователь администратором.
        /// </summary>
        /// ¬озвращает false, если пользователь не авторизован (CurrentUser == null).
        public static bool IsAdmin => CurrentUser != null && CurrentUser.RoleId == RoleIds.Admin;

        /// <summary>
        /// ѕровер€ет, €вл€етс€ ли текущий пользователь автором.
        /// </summary>
        /// ¬озвращает false, если пользователь не авторизован.
        public static bool IsAuthorRole => CurrentUser != null && CurrentUser.RoleId == RoleIds.Author;

        /// <summary>
        /// ѕровер€ет, заблокирован ли текущий пользователь (заморожен).
        /// </summary>
        /// ¬озвращает false, если пользователь не авторизован.
        public static bool IsFrozen => CurrentUser != null && CurrentUser.IsFrozen;
    }
}
