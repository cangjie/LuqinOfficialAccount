using System;
using System.Xml;
using Microsoft.Extensions.Configuration;
using System.Linq;
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
            switch(_message.MsgType.Trim().ToLower())
            {
                case "event":
                    switch (_message.Event.Trim().ToLower())
                    {
                        case "subscribe":
                            xmlD = CheckSubscribe();
                            break;
                        default:
                            break;
                    }
                    
                    break;
                case "text":
                default:
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
                    break;
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

        public XmlDocument CheckSubscribe()
        {
            XmlDocument xmlD = new XmlDocument();
            UserController uc = new UserController(_context, _config);
            int subscriberId = uc.CheckUser(_message.FromUserName.Trim());
            
            bool fromPoster = true;
            PosterScanLog scan = new PosterScanLog()
            {
                id = 0
            };
            try
            {
                scan =  _context.posterScanLog
                .Where(s => (s.scan_user_id == subscriberId))
                .OrderByDescending(s => s.id)
                .First();
            }
            catch
            {

            }
            
            if (scan.id == 0)
            {
                fromPoster = false;
            }
            DateTime scanDate = scan.create_date;
            long scanTimeStamp = long.Parse(Util.GetLongTimeStamp(scan.create_date));
            long subsTimeStamp = 1000 * long.Parse(_message.CreateTime);
            if (subsTimeStamp - scanTimeStamp > 1000 * 3600)
            {
                fromPoster = false;
            }

            if (fromPoster)
            {
                xmlD.LoadXml("<xml>"
                + "<ToUserName><![CDATA[" + _message.FromUserName.Trim() + "]]></ToUserName>"
                + "<FromUserName ><![CDATA[" + _settings.originalId.Trim() + "]]></FromUserName>"
                + "<CreateTime >" + Util.GetLongTimeStamp(DateTime.Now) + "</CreateTime>"
                + "<MsgType><![CDATA[text]]></MsgType>"
                + "<Content><![CDATA[感谢您通过您的朋友分享的海报关注到我们，您也可以回复“海报”来和您的其他朋友分享。]]></Content>"
                + "</xml>");
                OAUser poster = _context.oAUser
                    .Where(u => (u.user_id == scan.poster_user_id && u.original_id.Trim().Equals(_settings.originalId.Trim())))
                    .First();
                if (poster != null)
                {
                    OfficialAccountApi api = new OfficialAccountApi(_context, _config);
                    OASent sendMessage = new OASent()
                    {
                        id = 0,
                        MsgType = "text",
                        FromUserName = _settings.originalId,
                        ToUserName = poster.open_id,
                        Content = "有一个朋友通过您分享的海报关注了我们，在此表示万分感谢。"
                    };
                    api.SendServiceMessage(sendMessage);
                }
                
            }
            else
            {
                xmlD.LoadXml("<xml>"
                + "<ToUserName><![CDATA[" + _message.FromUserName.Trim() + "]]></ToUserName>"
                + "<FromUserName ><![CDATA[" + _settings.originalId.Trim() + "]]></FromUserName>"
                + "<CreateTime >" + Util.GetLongTimeStamp(DateTime.Now) + "</CreateTime>"
                + "<MsgType><![CDATA[text]]></MsgType>"
                + "<Content><![CDATA[感谢关注。]]></Content>"
                + "</xml>");
            }

                    

            return xmlD;
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
