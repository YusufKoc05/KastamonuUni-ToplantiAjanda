using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using KastamonuMeetingRoom.Helpers;

namespace KastamonuMeetingRoom.Controllers
{
    public class CaptchaController : Controller
    {
        [HttpGet]
        public IActionResult Image()
        {
            var code = CaptchaGenerator.GenerateCode();
            HttpContext.Session.SetString("CaptchaCode", code);

            using var bmp = new Bitmap(160, 60);
            using var gfx = Graphics.FromImage(bmp);

            // Arka plan renkli degrade
            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            using var brush = new LinearGradientBrush(rect,
                Color.FromArgb(255, 180, 200, 255),
                Color.FromArgb(255, 140, 170, 220),
                45f);

            gfx.FillRectangle(brush, rect);

            // Noise çizgiler
            var rand = new Random();
            for (int i = 0; i < 8; i++)
            {
                var pen = new Pen(Color.FromArgb(rand.Next(50, 150), rand.Next(50, 150), rand.Next(50, 150)), 2);
                gfx.DrawBezier(pen,
                    new Point(rand.Next(160), rand.Next(60)),
                    new Point(rand.Next(160), rand.Next(60)),
                    new Point(rand.Next(160), rand.Next(60)),
                    new Point(rand.Next(160), rand.Next(60)));
            }

            // CAPTCHA yazısı (bozulma efekti ile)
            var font = new Font("Arial", 28, FontStyle.Bold | FontStyle.Italic);
            var path = new GraphicsPath();

            path.AddString(code,
                font.FontFamily,
                (int)font.Style,
                32,
                new Point(10, 10),
                StringFormat.GenericDefault);

            // Distortion efekt
            float warp = 5;
            PointF[] points =
            {
                new PointF(rand.Next(5), rand.Next(20)),
                new PointF(150 + rand.Next(5), rand.Next(20)),
                new PointF(rand.Next(5), 40 + rand.Next(20)),
                new PointF(150 + rand.Next(5), 40 + rand.Next(20))
            };
            var m = new Matrix();
            path.Warp(points, rect, m, WarpMode.Perspective, 0.3f);

            gfx.FillPath(new SolidBrush(Color.FromArgb(20, 20, 60)), path);

            // JPEG olarak dön
            using var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Jpeg);
            return File(ms.ToArray(), "image/jpeg");
        }
    }
}
