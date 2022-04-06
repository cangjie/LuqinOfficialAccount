using System;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Pipelines;
using System.Collections;
namespace LuqinOfficialAccount.Controllers
{
    [Route("api/[controller]/[Action]")]
    public class ImageController:ControllerBase
    {
        [HttpGet]
        public void DrawQrCode()
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
