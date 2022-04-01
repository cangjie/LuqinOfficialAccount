using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using LuqinOfficialAccount.Models;
using System.Web;
namespace LuqinOfficialAccount.Controllers
{
    [Route("api/[controller]/[Action]")]
    [ApiController]
    public class OfficailAccountPageAuthController : ControllerBase
    {
        private readonly AppDBContext _context;

        private readonly IConfiguration _config;

        private readonly Settings _settings;
        public OfficailAccountPageAuthController(AppDBContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
            _settings = Settings.GetSettings(_config);
        }

        [HttpGet]
        public void RedirctToGetCode(string callBackUrl, string state="")
        {
            callBackUrl = Util.UrlEncode(callBackUrl);
            string redirectUrl = Request.Scheme.Trim() + "://" + Request.Host.ToString()
                + "/OfficailAccountPageAuthController/GetCode?callBack=" + callBackUrl.Trim();
            string url = "https://open.weixin.qq.com/connect/oauth2/authorize?appid=" + _settings.appId.Trim()
                + "redirect_uri=" + Util.UrlEncode(redirectUrl) 
                + "&response_type=code&scope=snsapi_base&state=" + state + "#wechat_redirect";
            Response.Redirect(url);
        }
    }
}
