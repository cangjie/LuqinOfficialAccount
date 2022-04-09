using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using LuqinOfficialAccount.Models;
using Microsoft.AspNetCore.Hosting;

namespace LuqinOfficialAccount.Controllers.Pages
{
    [Route("pages/[controller]/[action]")]
    public class CommonPageController : Controller
    {
        private readonly AppDBContext _context;

        private readonly IConfiguration _config;

        private readonly Settings _settings;

        
        public CommonPageController(AppDBContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
            _settings = Settings.GetSettings(_config);
            
        }

        [HttpGet]
        public IActionResult Index()
        {
            OAuthController oath = new OAuthController(_context, _config);//, HttpContext.Session, Request, Response, "");
            oath.AuthWithContext(Request, Response, "");
            return View("/Views/CommonPage.cshtml");
        }

        [HttpGet]
        public void Test()
        {

        }
        
    }
}