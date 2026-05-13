using System.IO;
using System.Windows.Media.Imaging;

namespace libraryapp
{
    /// <summary>
    /// Статический класс-утилита для работы с изображениями.
    /// </summary>
    public static class ImageHelper
    {
        /// <summary>
        /// Преобразует массив байтов в WPF-изображение (BitmapImage).
        /// </summary>
        public static BitmapImage ToBitmapImage(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return null;
            var image = new BitmapImage();
            using (var ms = new MemoryStream(bytes))
            {
                image.BeginInit();
                // CacheOption.OnLoad загружает все данные сразу, позволяя закрыть поток
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = ms;
                image.EndInit();
                image.Freeze();
            }
            return image;
        }
    }
}
