using System;
using Microsoft.AspNetCore.Mvc;
using ThoughtWorks.QRCode.Codec;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;

namespace LuqinOfficialAccount.Controllers.Api
{
    [Route("api/[controller]/[Action]")]
    [ApiController]
    public class QRCodeController : ControllerBase
    {
        
        public QRCodeController()
        {
        }

        [HttpGet]
        public ActionResult<string> GetQrCodeUrl(string qrCodeString)
        {
            QRCodeEncoder enc = new QRCodeEncoder();
            Bitmap bmp = enc.Encode(qrCodeString);
            string path = $"{Environment.CurrentDirectory}";
            if (!Directory.Exists(path + "/QRCode"))
            {
                Directory.CreateDirectory(path + "/QRCode");
            }
            string dateStr = DateTime.Now.Year.ToString() + DateTime.Now.Month.ToString().PadLeft(2, '0')
                + DateTime.Now.Day.ToString().PadLeft(2, '0');
            if (!Directory.Exists(path + "/QRCode" + "/" + dateStr.Trim()))
            {
                Directory.CreateDirectory(path + "/QRCode" + "/" + dateStr.Trim());
            }
            string timeStamp = Util.GetLongTimeStamp(DateTime.Now);
            bmp.Save(path + "/QRCode/" + dateStr + "/" + timeStamp + ".jpg", ImageFormat.Jpeg);
            return "/QRCode/" + dateStr + "/" + timeStamp + ".jpg";
        }


    }
}
