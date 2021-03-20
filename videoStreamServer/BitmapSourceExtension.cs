// Licensed under the MIT License
// File: BitmapSourceExtension.cs

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Emgu.CV;
using Emgu.CV.CvEnum;

namespace videoStreamServer
{
    public static class BitmapSourceExtension
    {
        [DllImport("gdi32")]
        private static extern int DeleteObject(IntPtr o);

        public static BitmapSource ToBitmapSource(this IInputArray image)
        {
            using var ia = image.GetInputArray();
            using var m = ia.GetMat();
            using var source = m.ToBitmap();
            var ptr = source.GetHbitmap(); //obtain the Hbitmap
            var bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                ptr,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            DeleteObject(ptr); //release the HBitmap
            return bs;
        }

        public static Mat ToMat(this BitmapSource source)
        {
            if (source.Format == PixelFormats.Bgra32)
            {
                var result = new Mat();
                result.Create(source.PixelHeight, source.PixelWidth, DepthType.Cv8U, 4);
                source.CopyPixels(Int32Rect.Empty, result.DataPointer, result.Step * result.Rows, result.Step);
                return result;
            }

            if (source.Format == PixelFormats.Bgr24)
            {
                var result = new Mat();
                result.Create(source.PixelHeight, source.PixelWidth, DepthType.Cv8U, 3);
                source.CopyPixels(Int32Rect.Empty, result.DataPointer, result.Step * result.Rows, result.Step);
                return result;
            }

            throw new Exception(string.Format("Conversion from BitmapSource of format {0} is not supported.", source.Format));
        }
    }
}