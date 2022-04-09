using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using LuqinOfficialAccount.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using LuqinOfficialAccount.Controllers.Api;
namespace LuqinOfficialAccount.Controllers.Pages
{
    [Route("pages/[controller]/[action]")]
    public class OAuthController : Controller
    {
        private readonly AppDBContext _context;

        private readonly IConfiguration _config;

        private readonly Settings _settings;

        private ISession _session;

        private string _token = "";

        private readonly string _openId;

        public HttpRequest _request;

        public HttpResponse _response;

        public string _state;

      
        /*
        public OAuthController(AppDBContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
            _settings = Settings.GetSettings(_config);
            _session = HttpContext.Session;
        }
        */

        public OAuthController(AppDBContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
            _settings = Settings.GetSettings(_config);
           
        }

        [NonAction]
        public void AuthWithContext(HttpRequest request, HttpResponse response, string state)
        {
            
            _request = request;
            _response = response;
            _state = state.Trim();
            _session = _request.HttpContext.Session;
            if (!string.IsNullOrEmpty(_session.GetString("token")))
            {
                _token = _session.GetString("token").Trim();
            }

            UserController user = new UserController(_context, _config);
            if (user.CheckToken(_token).Result.Value)
            {
                _session.SetString("token", _token);
            }
            else
            {
                //Auth();
                /*
                string[] pathArr = _request.Path.ToUriComponent().Split('/');

                string newPath = "";

                foreach (string s in pathArr)
                {
                    if (!s.Trim().Equals("pages"))
                    {
                        newPath = newPath + "/" + s.Trim();
                    }
                    else
                    {
                        break;
                    }
                }
                */

                string currentUrl = _request.Scheme.Trim() + "://" + _request.Host.ToString().Trim()
                    + _request.PathBase.ToString().Trim() + _request.Path.ToString().Trim()
                    + (_request.QueryString.ToString().Equals("") ? "" : "?"
                    + _request.QueryString.ToString().Trim());
                _session.SetString("callback", currentUrl);
                string redirectUrl = _request.Scheme.Trim() + "://"
                    + _request.Host.ToString().Trim() + _request.PathBase.ToString() + "/pages/OAuth/CallBack";
                string url = "https://open.weixin.qq.com/connect/oauth2/authorize?appid=" + _settings.appId.Trim()
                    + "&redirect_uri=" + Util.UrlEncode(redirectUrl)
                    + "&response_type=code&scope=snsapi_base&state=" + _state.Trim() + "#wechat_redirect";
                _response.Redirect(url);

            }
        }

        

        [HttpGet]
        public void CallBack(string code, string state)
        {
            string callBack = "";
            try
            {
                callBack = HttpContext.Session.GetString("callback").Trim();
            }
            catch
            {

            }
            Response.Redirect(callBack, true);
        }
        
    }
}