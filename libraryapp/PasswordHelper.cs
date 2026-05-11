using System.Security.Cryptography;
using System.Text;

namespace libraryapp
{
    public static class PasswordHelper
    {
        public static string Hash(string password)
        {
            if (password == null) password = string.Empty;
            using (var sha = SHA256.Create())
            {
                return System.Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(password)));
            }
        }

        public static bool Verify(string password, string hash)
        {
            return Hash(password) == hash;
        }
    }
}
