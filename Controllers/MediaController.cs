using System;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using LuqinOfficialAccount.Models;


namespace LuqinOfficialAccount.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class MediaController : ControllerBase
    {
        private readonly AppDBContext _context;

        private readonly IConfiguration _config;

        private readonly Settings _settings;

        public MediaController(AppDBContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
            _settings = Settings.GetSettings(_config);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> PlayMedia(int id, string contentType, string token)
        {
            
            //token = Util.UrlEncode(token);
            string realTemplatePath = Util.workingPath + "/medias/test.mp3";

            //DateTime expireTime = Util.GetDateTimeByTimeStamp(1650301333270);
            /*
            long nowTimeStamp = long.Parse(Util.GetLongTimeStamp(DateTime.Now));
            var tokenList = _context.token.Where(t => ( t.token.Trim().Equals(token.Trim())
                && t.state == 1 && t.expire_timestamp > nowTimeStamp))
                .OrderByDescending(t => t.id).ToList();
            if (tokenList == null || tokenList.Count == 0)
            {
                return NotFound();
            }
            int userId = tokenList[0].user_id;
            var umaList = _context.userMediaAsset.Where(u => (u.media_id == id
                && u.user_id == userId )).ToList();
            if (umaList == null || umaList.Count == 0)
            {
                return NotFound();
            }
            //contentType = Util.UrlEncode(contentType);
            */
            Response.ContentType = contentType;
            PipeWriter pw = Response.BodyWriter;
            Stream s = pw.AsStream();
            System.IO.FileInfo mediaFileInfo = new System.IO.FileInfo(realTemplatePath);
            byte[] buffer = new byte[mediaFileInfo.Length];
            FileStream fs = System.IO.File.OpenRead(realTemplatePath);
            int seg = 1024 * 1024;
            for (int i = 0; (long)(i * seg) < mediaFileInfo.Length; i++)
            {
                int count = seg;
                if ((long)((i + 1) * seg) > mediaFileInfo.Length)
                {
                    count = (int)(mediaFileInfo.Length - i * seg);
                }
                fs.Read(buffer, i * seg, count);
            }
            fs.Close();
            fs.Dispose();
            //s.Write(buffer);
            await s.WriteAsync(buffer);
            s.Close();
            s.Dispose();
            return NoContent();
        }
        

        [HttpGet]
        public async Task<IActionResult> GetMediaData()
        {
            string realTemplatePath = Util.workingPath + "/medias/test.mp3";
            Response.ContentType = "audio/mp3";
            PipeWriter pw = Response.BodyWriter;
            Stream s = pw.AsStream();
            System.IO.FileInfo mediaFileInfo = new System.IO.FileInfo(realTemplatePath);
            byte[] buffer = new byte[mediaFileInfo.Length];
            FileStream fs = System.IO.File.OpenRead(realTemplatePath);
            int seg = 1024*1024;
            for (int i = 0; (long)(i * seg) < mediaFileInfo.Length; i++)
            {
                int count = seg;
                if ((long)((i + 1) * seg) > mediaFileInfo.Length)
                {
                    count = (int)(mediaFileInfo.Length - i * seg);
                }
                fs.Read(buffer, i * seg, count);
            }
            fs.Close();
            fs.Dispose();
            //s.Write(buffer);
            await s.WriteAsync(buffer);
            s.Close();
            s.Dispose();

            return NoContent();
            

        }
    }
}
