using System.Drawing;
using System.Drawing.Imaging;

namespace LombdaAgentSDK
{
    public static class ImageConverterUtil
    {
        public static byte[] BitmapToByteArray(Bitmap bitmap, ImageFormat format = null)
        {
            format ??= ImageFormat.Png; // default to PNG
            using (var ms = new MemoryStream())
            {
                bitmap.Save(ms, format);
                return ms.ToArray();
            }
        }

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
