/*
 * MIT License
 *
 * Copyright (c) 2017 Dr. Christian Benjamin Ries
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */
using System;
using System.IO;

using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color = System.Drawing.Color;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace RailwayEssentialCore
{
    public static class Utils
    {
        public const string ThemeName = @"\Themes\RailwayEssential";
        public const string TrackplansEditor = @"\Trackplans\Webeditor";
        
        public static string ExpandRailwayEssential(this string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            var p = Directory.GetCurrentDirectory();
            var pp = path.TrimStart('/', '\\');

            return Path.Combine(p, pp);
        }

        public static string GenerateUniqueName(this string fmt, string dirname=null)
        {
            for (int i = 0; i < 1000; ++i)
            {
                if (!string.IsNullOrEmpty(dirname))
                {
                    var name = Path.Combine(dirname, string.Format(fmt, i));
                    if (!File.Exists(name))
                        return name;
                }
                else
                {
                    var name = string.Format(fmt, i);
                    if (!File.Exists(name))
                        return name;
                }
            }

            return null;
        }

    }

    public static class ImageHelper
    {
        public static string ImageToBase64(Image image, System.Drawing.Imaging.ImageFormat format)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                // Convert Image to byte[]
                image.Save(ms, format);
                byte[] imageBytes = ms.ToArray();

                // Convert byte[] to Base64 String
                string base64String = Convert.ToBase64String(imageBytes);
                return base64String;
            }
        }

        public static Image Base64ToImage(string base64String)
        {
            // Convert Base64 String to byte[]
            byte[] imageBytes = Convert.FromBase64String(base64String);
            using (var ms = new MemoryStream(imageBytes, 0, imageBytes.Length))
            {

                // Convert byte[] to Image
                ms.Write(imageBytes, 0, imageBytes.Length);
                Image image = Image.FromStream(ms, true);
                return image;
            }
        }

        public static ImageSource ToImageSource(this Image img, System.Drawing.Imaging.ImageFormat fmt)
        {
            try
            {
                using (var ms = new MemoryStream())
                {
                    img.Save(ms, fmt);
                    ms.Seek(0, SeekOrigin.Begin);

                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = ms;
                    bitmapImage.EndInit();

                    return bitmapImage;
                }
            }
            catch
            {

            }
            return null;
        }

        public static ImageSource Base64ToImageSource(string base64String)
        {
            if (string.IsNullOrEmpty(base64String))
                return null;

            try
            {
                byte[] imageBytes = Convert.FromBase64String(base64String);
                using (var ms = new MemoryStream(imageBytes, 0, imageBytes.Length))
                {
                    ms.Write(imageBytes, 0, imageBytes.Length);
                    Image image = Image.FromStream(ms, true);
                    return image.ToImageSource(ImageFormat.Bmp);
                }
            }
            catch
            {
                return null;
            }
        }

        // Source: https://stackoverflow.com/questions/6501797/resize-image-proportionally-with-maxheight-and-maxwidth-constraints
        public static Image ResizeImage(Image image, int maxWidth, int maxHeight)
        {
            var ratioX = (double)maxWidth / image.Width;
            var ratioY = (double)maxHeight / image.Height;
            var ratio = Math.Min(ratioX, ratioY);

            var newWidth = (int)(image.Width * ratio);
            var newHeight = (int)(image.Height * ratio);

            var newImage = new Bitmap(newWidth, newHeight);

            using (var graphics = Graphics.FromImage(newImage))
                graphics.DrawImage(image, 0, 0, newWidth, newHeight);

            return newImage;
        }

        // Source: https://stackoverflow.com/questions/1922040/resize-an-image-c-sharp
        public static Image ResizeImageInPictureBox(int newWidth, int newHeight, string stPhotoPath, bool keepProportion = true)
        {
            Image imgPhoto = Image.FromFile(stPhotoPath);

            int sourceWidth = imgPhoto.Width;
            int sourceHeight = imgPhoto.Height;

            // Consider vertical pics
            if (sourceWidth < sourceHeight)
            {
                int buff = newWidth;

                newWidth = newHeight;
                newHeight = buff;
            }

            int sourceX = 0, sourceY = 0, destX = 0, destY = 0;
            float nPercent = 1.0f, nPercentW = 1.0f, nPercentH = 1.0f;

            nPercentW = ((float)newWidth / (float)sourceWidth);
            nPercentH = ((float)newHeight / (float)sourceHeight);
            if (nPercentH < nPercentW)
            {
                nPercent = nPercentH;
                destX = System.Convert.ToInt16((newWidth - (sourceWidth * nPercent)) / 2);
            }
            else
            {
                nPercent = nPercentW;
                destY = System.Convert.ToInt16((newHeight - (sourceHeight * nPercent)) / 2);
            }

            int destWidth = (int)(sourceWidth * nPercent);
            int destHeight = (int)(sourceHeight * nPercent);

            if (!keepProportion)
            {
                if (destWidth > destHeight)
                    destHeight = destWidth;
                else
                    destWidth = destHeight;

                destX = destY = 0;
            }

            Bitmap bmPhoto = new Bitmap(newWidth, newHeight, PixelFormat.Format24bppRgb);
            bmPhoto.SetResolution(imgPhoto.HorizontalResolution, imgPhoto.VerticalResolution);

            Graphics grPhoto = Graphics.FromImage(bmPhoto);
            grPhoto.Clear(Color.White);
            grPhoto.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

            grPhoto.DrawImage(imgPhoto,
                new Rectangle(destX, destY, destWidth, destHeight),
                new Rectangle(sourceX, sourceY, sourceWidth, sourceHeight),
                GraphicsUnit.Pixel);

            grPhoto.Dispose();
            imgPhoto.Dispose();

            return bmPhoto;
        }
    }
}
