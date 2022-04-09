using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using LuqinOfficialAccount.Models;

namespace LuqinOfficialAccount.Controllers
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
            //OfficialAccountApi oaa = new OfficialAccountApi(_context, _config);

            if (string.IsNullOrEmpty(HttpContext.Session.GetString("token")))
            {
                HttpContext.Session.SetString("token", "asdfasdfasdfasdwww");
            }
            
            return View("/Views/CommonPage.cshtml");
        }
        
    }
}