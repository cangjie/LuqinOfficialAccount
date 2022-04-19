using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using LuqinOfficialAccount.Models;
using Microsoft.AspNetCore.Http;
using LuqinOfficialAccount.Controllers.Api;
// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace LuqinOfficialAccount.Controllers.Pages
{
    [Route("pages/[controller]/[action]")]
    public class PosterLandingController : Controller
    {
        private readonly AppDBContext _context;

        private readonly IConfiguration _config;

        private readonly Settings _settings;

        private readonly OAuthController _oauth;

        public PosterLandingController(AppDBContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
            _settings = Settings.GetSettings(_config);
            _oauth = new OAuthController(_context, _config);
        }

        // GET: /<controller>/
        [HttpGet("{userId}")]
        public async Task<IActionResult> Index(int userId)
        {
            _oauth.AuthWithContext(Request, Response, "");
            string token = "";
            try
            {
                token = HttpContext.Session.GetString("token").Trim();
            }
            catch
            {

            }

            //token = "56_2isAzKqNt6ns0ks5ASqpItUUv8wRxDWzwJVU4OPfIWqgrUeXighChqAZKpvKJvjbO_hCbjkSMpNUY2VYvSMFJfQia6r71fqV97RBectMCc";


            if (!token.Trim().Equals(""))
            {
                UserController user = new UserController(_context, _config);
                string openId = user.GetUserOpenId(token);
                if (!openId.Trim().Equals(""))
                {
                    int scanUserId = user.CheckUser(openId);
                    PosterScanLog log = new PosterScanLog()
                    {
                        id = 0,
                        poster_user_id = userId,
                        scan_user_id = scanUserId,
                        original_id = _settings.originalId.Trim(),
                        open_id = openId
                    };
                    try
                    {
                        _context.Add(log);
                        await _context.SaveChangesAsync();
                    }
                    catch
                    {

                    }
                }
                
            }
            return View("/Views/PosterLanding.cshtml");
        }
    }
}
