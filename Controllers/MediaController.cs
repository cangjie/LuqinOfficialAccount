using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using LuqinOfficialAccount.Models;
using System.IO;
using System.IO.Pipelines;
using System;

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
        public  void GetMediaData()
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
            s.Write(buffer);
            
            s.Close();
            s.Dispose();
            
            

        }
    }
}
