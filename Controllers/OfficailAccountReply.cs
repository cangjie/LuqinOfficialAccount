using System;
using System.Xml;
using Microsoft.Extensions.Configuration;
using LuqinOfficialAccount.Models;
namespace LuqinOfficialAccount.Controllers
{
    public class OfficailAccountReply
    {
        private readonly AppDBContext _context;

        private readonly IConfiguration _config;

        private readonly Settings _settings;

        private readonly OARecevie _message;

        public OfficailAccountReply(AppDBContext context,
            IConfiguration config, OARecevie message)
        {
            _context = context;
            _config = config;
            _settings = Settings.GetSettings(_config);
            _message = message;
        }

        public string Reply()
        {
            string retStr = "success";
            XmlDocument xmlD = new XmlDocument();
            if (_message.MsgType.Trim().ToLower().Equals("text"))
            {
                switch (_message.Content.Trim().ToLower())
                {
                    case "海报":
                        xmlD = GetPoster();
                        retStr = xmlD.InnerXml.Trim();
                        break;
                    default:
                        retStr = "success";
                        break;
                }
            }

            try
            {
                OASent sent = new OASent()
                {
                    id = 0,
                    FromUserName = xmlD.SelectSingleNode("//xml/FromUserName").InnerText.Trim(),
                    ToUserName = xmlD.SelectSingleNode("//xml/ToUserName").InnerText.Trim(),
                    is_service = 0,
                    origin_message_id = _message.id,
                    MsgType = xmlD.SelectSingleNode("//xml/MsgType").InnerText.Trim(),
                    Content = xmlD.SelectSingleNode("//xml/Content").InnerXml.Trim(),
                    err_code = "",
                    err_msg = ""

                };
                _context.oASent.Add(sent);
                _context.SaveChanges();
            }
            catch
            {

            }
            return retStr.Trim();
        }

        public XmlDocument GetPoster()
        {
            UserController user = new UserController(_context, _config);
            int userId = user.CheckUser(_message.FromUserName);
            if (userId == 0)
            {
                return new XmlDocument();
            }
            string landingPageUrl = "http://weixin.luqinwenda.com/service/pages/PosterLanding/Index/" + userId.ToString();
            string imageUrl = "http://weixin.luqinwenda.com/subscribe/api/Image/CreatePersonalPosterWithTextQrCode?templatePath=%2Fimages%2Ftemplate.jpg&x=310&y=660&scale=160&qrCodeText=" + Util.UrlEncode(landingPageUrl);
            XmlDocument xmlD = new XmlDocument();
            xmlD.LoadXml("<xml>"
                + "<ToUserName><![CDATA[" + _message.FromUserName.Trim() + "]]></ToUserName>"
                + "<FromUserName ><![CDATA[" + _settings.originalId.Trim() + "]]></FromUserName>"
                + "<CreateTime >" + Util.GetLongTimeStamp(DateTime.Now) + "</CreateTime>"
                + "<MsgType><![CDATA[text]]></MsgType>"
                + "<Content><![CDATA[<a href=\"" + imageUrl + "\" >点击分享海报</a>]]></Content>"
                + "</xml>");
            return xmlD;
        }
    }
}
