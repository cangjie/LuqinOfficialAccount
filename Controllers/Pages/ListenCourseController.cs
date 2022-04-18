using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using LuqinOfficialAccount.Models;

namespace LuqinOfficialAccount.Controllers.Pages
{
    [Route("pages/[controller]/[action]")]
    public class ListenCourseController : Controller
    {

        

        private readonly AppDBContext _context;

        private readonly IConfiguration _config;

        private readonly Settings _settings;

        private readonly OAuthController _oauth;

        

        public ListenCourseController(AppDBContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
            _settings = Settings.GetSettings(_config);
            _oauth = new OAuthController(_context, _config);
        }

        [HttpGet("{id}")]
        public IActionResult Index(int id)
        {
            _oauth.AuthWithContext(Request, Response, "");
            ViewData["id"] = id.ToString();
            string token = "";
            try
            {
                token = HttpContext.Session.GetString("token").Trim();
            }
            catch
            {

            }
            //token = "56_cDlM6pi1edIjlY-CjoR6uKxeinu3sUpytk4QA5mScdmmQtmrdYG0p7AXq76jeCij8M8D3CdCGBPQQ661jDz8JNahCaBDfZ-8Y1tIOoyx0Rc";
            ViewData["token"] = token;
            
            return View("/Views/ListenCourse.cshtml");
        }
    }
}