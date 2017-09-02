using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

namespace SvgThemeIconGenerator
{
    class Program
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

        static void Main(string[] args)
        {
            var cwd = Directory.GetCurrentDirectory();
            var prjDir = Path.Combine(cwd, "..\\..\\..\\");
            var svgTpl = Path.Combine(prjDir, "RailwayEssentialMdi\\Resources\\Theme\\RailwayEssential\\trackicon.svg.tpl");
            string svgCnt = null;
            if (File.Exists(svgTpl))
                svgCnt = File.ReadAllText(svgTpl, Encoding.UTF8);
            if (string.IsNullOrEmpty(svgCnt))
                return;

            var svgTplDir = Path.GetDirectoryName(svgTpl);
            var pngFiles = Directory.GetFiles(svgTplDir, "*.png", SearchOption.TopDirectoryOnly);
            var svgFiles = Directory.GetFiles(svgTplDir, "*.svg", SearchOption.TopDirectoryOnly);

            foreach (var svgName in svgFiles)
            {
                if (string.IsNullOrEmpty(svgName))
                    continue;
                if (File.Exists(svgName))
                    continue;

                File.Delete(svgName);
            }

            foreach (var pngName in pngFiles)
            {
                if (string.IsNullOrEmpty(pngName))
                    continue;
                if (!File.Exists(pngName))
                    continue;

                Image img = Image.FromFile(pngName);
                var imgBase64 = ImageToBase64(img, ImageFormat.Png);

                var name = Path.GetFileNameWithoutExtension(pngName);
                var targetSvgName = Path.Combine(svgTplDir, name + ".svg");

                string targetSvgCnt = svgCnt;
                targetSvgCnt = targetSvgCnt.Replace("{{WIDTH}}", $"{img.Width}");
                targetSvgCnt = targetSvgCnt.Replace("{{BASE64IMAGEDATA}}", imgBase64);

                Console.WriteLine($"Generate {Path.GetFileName(targetSvgName)}");
                File.WriteAllText(targetSvgName, targetSvgCnt, Encoding.UTF8);
            }

            Console.WriteLine("Enter any key...");
            Console.ReadLine();
        }
    }
}
