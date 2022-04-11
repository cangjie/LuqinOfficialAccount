using System;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Pipelines;
using System.Collections;
using ThoughtWorks.QRCode.Codec;
namespace LuqinOfficialAccount.Controllers.Api
{
    [Route("api/[controller]/[Action]")]
    public class ImageController:ControllerBase
    {
        [HttpGet]
        public void CreatePersonalPosterWithTextQrCode(string templatePath, int x, int y, int scale, string qrCodeText)
        {
            string realTemplatePath = Util.workingPath + templatePath;
            //Bitmap bmpTemplate = Bitmap.FromFile(realTemplatePath);
            Image imgTemplate = Bitmap.FromFile(realTemplatePath);
            QRCodeEncoder enc = new QRCodeEncoder();
            Bitmap bmpQr = enc.Encode(qrCodeText);
            Graphics g = Graphics.FromImage(imgTemplate);
            g.DrawImage(bmpQr, x, y, scale, scale);
            g.Save();
            Response.ContentType = "image/jpeg";
            PipeWriter pw = Response.BodyWriter;
            Stream s = pw.AsStream();
            imgTemplate.Save(s, ImageFormat.Jpeg);
            s.Close();
            g.Dispose();
            bmpQr.Dispose();
            imgTemplate.Dispose();
        }
        [HttpGet]
        public void DrawQrCodeTest()
        {
            string imgPath = Util.workingPath + "/images/a.jpg";
            ArrayList fileArr = new ArrayList();
            using (FileStream fs = System.IO.File.OpenRead(imgPath))
            {
                int b = fs.ReadByte();
                for (; b >= 0;)
                {
                    fileArr.Add((byte)b);
                    b = fs.ReadByte();
                }
                fs.Close();
            }
            byte[] bArr = new byte[fileArr.Count];
            for (int i = 0; i < bArr.Length; i++)
            {
                bArr[i] = (byte)fileArr[i];
            }
            Response.ContentType = "image/jpeg";
            PipeWriter pw = Response.BodyWriter;
            Stream s = pw.AsStream();
            for (int i = 0; i < bArr.Length; i++)
            {
                s.WriteByte(bArr[i]);
            }
            s.Close();

        }
    }
}
