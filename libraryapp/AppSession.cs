using System.Linq;

namespace libraryapp
{
    public static class AppSession
    {
        public static AppUsers CurrentUser { get; private set; }

        public static void SetUser(AppUsers user)
        {
            CurrentUser = user;
        }

        public static void ClearUser()
        {
            CurrentUser = null;
        }

        public static void ReloadCurrentUser()
        {
            if (CurrentUser == null) return;
            CurrentUser = Core.Context.AppUsers.FirstOrDefault(u => u.UserId == CurrentUser.UserId);
        }

        public static bool IsAdmin => CurrentUser != null && CurrentUser.RoleId == RoleIds.Admin;

        public static bool IsAuthorRole => CurrentUser != null && CurrentUser.RoleId == RoleIds.Author;

        public static bool IsFrozen => CurrentUser != null && CurrentUser.IsFrozen;
    }
}
