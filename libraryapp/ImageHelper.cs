using System.IO;
using System.Windows.Media.Imaging;

namespace libraryapp
{
    public static class ImageHelper
    {
        public static BitmapImage ToBitmapImage(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return null;
            var image = new BitmapImage();
            using (var ms = new MemoryStream(bytes))
            {
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = ms;
                image.EndInit();
                image.Freeze();
            }
            return image;
        }
    }
}
