using System.Drawing;
using System.Drawing.Imaging;

namespace LombdaAgentSDK
{
    public static class ImageConverterUtil
    {
        /// <summary>
        /// Converts a <see cref="Bitmap"/> to a byte array using the specified image format.
        /// </summary>
        /// <param name="bitmap">The <see cref="Bitmap"/> to convert. Cannot be <see langword="null"/>.</param>
        /// <param name="format">The <see cref="ImageFormat"/> to use for conversion. Defaults to PNG if <see langword="null"/>.</param>
        /// <returns>A byte array representing the bitmap in the specified image format.</returns>
        public static byte[] BitmapToByteArray(Bitmap bitmap, ImageFormat format = null)
        {
            format ??= ImageFormat.Png; // default to PNG
            using (var ms = new MemoryStream())
            {
                bitmap.Save(ms, format);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Converts a <see cref="Bitmap"/> image to a Base64-encoded data URL string.
        /// </summary>
        /// <remarks>The method supports JPEG, BMP, GIF, and PNG formats. The MIME type in the data URL is
        /// determined by the specified image format.</remarks>
        /// <param name="bitmap">The <see cref="Bitmap"/> image to convert. Cannot be null.</param>
        /// <param name="format">The <see cref="ImageFormat"/> to use for encoding the image. If null, the default format is PNG.</param>
        /// <returns>A string representing the image as a Base64-encoded data URL. The string includes the appropriate MIME type
        /// based on the specified image format.</returns>
        public static string ImageToBase64DataUrl(Bitmap bitmap, ImageFormat format = null)
        {
            string base64String = "";

            format ??= ImageFormat.Png;

            var mimeType = format == ImageFormat.Jpeg ? "image/jpeg" :
                           format == ImageFormat.Bmp ? "image/bmp" :
                           format == ImageFormat.Gif ? "image/gif" :
                           "image/png";

            using (var ms = new MemoryStream())
            {
                bitmap.Save(ms, format);
                base64String = Convert.ToBase64String(ms.ToArray());
                return $"data:{mimeType};base64,{base64String}";
            }
        }
    }
}
