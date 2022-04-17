using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using LuqinOfficialAccount.Models;
using System.IO;
using System.IO.Pipelines;

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

        [HttpGet]
        public void GetMediaData()
        {
            string realTemplatePath = Util.workingPath + "/medias/test.mp3";
            Response.ContentType = "audio/mp3";
            PipeWriter pw = Response.BodyWriter;
            Stream s = pw.AsStream();
            using (FileStream fs = System.IO.File.OpenRead(realTemplatePath))
            {
                int b = fs.ReadByte();
                for (; b >= 0;)
                {
                    s.WriteByte((byte)b);
                    b = fs.ReadByte();
                }
                fs.Close();
            }
            s.Close();

        }
    }
}
