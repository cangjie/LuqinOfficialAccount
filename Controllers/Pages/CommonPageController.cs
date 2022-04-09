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

        private readonly OAuthController _oauth;
        
        public CommonPageController(AppDBContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
            _settings = Settings.GetSettings(_config);
            _oauth = new OAuthController(_context, _config);
        }

        [HttpGet]
        public IActionResult Index()
        {
            _oauth.AuthWithContext(Request, Response, "");
            return View("/Views/CommonPage.cshtml");
        }

        [HttpGet]
        public void Test()
        {

        }
        
    }
}