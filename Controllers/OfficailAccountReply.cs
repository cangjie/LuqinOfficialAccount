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
                            retStr = xmlD.InnerXml.Trim();
                            break;
                        case "click":
                            xmlD = CheckClick();
                            retStr = xmlD.InnerXml.Trim();
                            break;
                        default:
                            break;
                    }
                    
                    break;
                case "text":
                default:
                    switch (_message.Content.Trim().ToLower())
                    {
                        case "听课":
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

        public XmlDocument CheckClick()
        {
            XmlDocument xmlD = new XmlDocument();
            switch (_message.EventKey.ToLower())
            {
                case "free":
                    xmlD = FreeClick();
                    break;
                default:
                    break;
            }
            return xmlD;
        }

        public XmlDocument FreeClick()
        {
            UserController uc = new UserController(_context, _config);
            XmlDocument xmlD = new XmlDocument();
            int userId = uc.CheckUser(_message.FromUserName.Trim());
            var assetList = _context.userMediaAsset.Where(a => a.user_id == userId).ToList();
            if (assetList != null && assetList.Count > 0)
            {
                string message = "您可以<a href='https://mp.weixin.qq.com/s/tOUNhLcJMp4uqkDG4PTCKA' >点击此处</a>开始聆听卢老师的收费课程。";
                xmlD.LoadXml("<xml>"
                + "<ToUserName><![CDATA[" + _message.FromUserName.Trim() + "]]></ToUserName>"
                + "<FromUserName ><![CDATA[" + _settings.originalId.Trim() + "]]></FromUserName>"
                + "<CreateTime >" + Util.GetLongTimeStamp(DateTime.Now) + "</CreateTime>"
                + "<MsgType><![CDATA[text]]></MsgType>"
                + "<Content><![CDATA[" + message.Trim() + "]]></Content>"
                + "</xml>");
            }
            else
            {
                xmlD = GetPoster();
            }

            
            return xmlD;
        }

        public XmlDocument CheckSubscribe()
        {
            XmlDocument xmlD = new XmlDocument();
            UserController uc = new UserController(_context, _config);
            DateTime submitTime = Util.GetDateTimeByTimeStamp(1000 * long.Parse(_message.CreateTime)).AddHours(8);
            int subscriberId = uc.CheckUser(_message.FromUserName.Trim());
            
            bool fromPoster = true;
            PosterScanLog scan = new PosterScanLog()
            {
                id = 0
            };
            try
            {

                var scanList = _context.posterScanLog
                .Where(s => (s.deal == 0
                && s.create_date <= submitTime
                && s.create_date >= submitTime.AddMinutes(-5)
                ))
                .OrderBy(s => s.id).ToList();
                if (scanList != null && scanList.Count > 0)
                {
                    scan = scanList[0];
                }
                else
                {
                    fromPoster = false;
                }
                
                
            }
            catch(Exception err)
            {
                fromPoster = false;
                Console.WriteLine(err.ToString());
            }
            
            if (scan.id == 0)
            {
                fromPoster = false;
            }
            DateTime scanDate = scan.create_date;
            long scanTimeStamp = long.Parse(Util.GetLongTimeStamp(scan.create_date));
            long subsTimeStamp = 1000 * long.Parse(_message.CreateTime);
            

            if (fromPoster)
            {
                OAUser posterUser = _context.oAUser.Where(u => (
                    u.original_id.Trim().Equals(_message.ToUserName.Trim())
                    && u.user_id == scan.poster_user_id)).First();

                OAUser scanUser = _context.oAUser.Where(u => (
                    u.original_id.Trim().Equals(_message.ToUserName.Trim())
                    && u.open_id.Trim().Equals(_message.FromUserName.Trim()))).First();

                var promoteList = _context.promote.Where(p => (
                    p.original_id.Trim().Equals(_message.ToUserName.Trim())
                    //&&  p.promote_open_id.Trim().Equals(posterUser.open_id.Trim())
                    && p.follow_open_id.Trim().Equals(scanUser.open_id.Trim())
                )).ToList();
                if (promoteList.Count == 0)
                {
                    
                    Promote p = new Promote()
                    {
                        id = 0,
                        original_id = _message.ToUserName.Trim(),
                        promote_user_id = scan.poster_user_id,
                        promote_open_id = posterUser.open_id.Trim(),
                        follow_user_id = scan.scan_user_id,
                        follow_open_id = scanUser.open_id.Trim(),
                        create_date = DateTime.Now

                    };
                    _context.promote.Add(p);
                    try
                    {
                        _context.SaveChanges();
                        scan.deal = 1;
                        _context.Entry(scan);
                        try
                        {
                            _context.SaveChanges();
                        }
                        catch
                        { 
                        
                        }
                    }
                    catch
                    {
                        fromPoster = false;
                    }

                }
                else
                {
                    fromPoster = false;
                }
            }

            if (fromPoster)
            {

                xmlD.LoadXml("<xml>"
                + "<ToUserName><![CDATA[" + _message.FromUserName.Trim() + "]]></ToUserName>"
                + "<FromUserName ><![CDATA[" + _settings.originalId.Trim() + "]]></FromUserName>"
                + "<CreateTime >" + Util.GetLongTimeStamp(DateTime.Now) + "</CreateTime>"
                + "<MsgType><![CDATA[text]]></MsgType>"
                + "<Content><![CDATA[感谢您的关注，回复“听课”，立即参与「0元免费领」活动！]]></Content>"
                + "</xml>");
                OAUser poster = _context.oAUser
                    .Where(u => (u.user_id == scan.poster_user_id && u.original_id.Trim().Equals(_settings.originalId.Trim())))
                    .First();
                if (poster != null)
                {
                    OfficialAccountApi api = new OfficialAccountApi(_context, _config);
                    

                    //check promote num
                    var promoteTotal = _context.promote.Where(p => (p.original_id.Trim().Equals(_settings.originalId.Trim())
                    && p.promote_open_id.Trim().Equals(poster.open_id.Trim()))).ToList();
                    

                    if (promoteTotal != null)
                    {
                        if (promoteTotal.Count <= 3)
                        {
                            string msgText = "";
                            if (promoteTotal.Count == 3)
                            {
                                var umaList = _context.userMediaAsset.Where(u =>
                                (u.user_id == poster.user_id && u.media_id == 1)).ToList();
                                if (umaList == null || umaList.Count == 0)
                                {
                                    UserMediaAsset uma = new UserMediaAsset()
                                    {
                                        media_id = 1,
                                        user_id = scan.poster_user_id,

                                    };
                                    _context.userMediaAsset.Add(uma);
                                    _context.SaveChanges();
                                }
                                msgText = "已经有" + promoteTotal.Count.ToString() + "个朋友通过您的海报关注了我们的公众号，"
                                    + "您可以<a href='https://mp.weixin.qq.com/s/tOUNhLcJMp4uqkDG4PTCKA' >点击此处</a>开始聆听卢老师的收费课程。";

                            }
                            else
                            {
                                if (promoteTotal != null)
                                {
                                    msgText = "已经有" + promoteTotal.Count.ToString() + "个朋友通过您分享的海报关注了我们。";
                                }
                                else
                                {
                                    msgText = "又有一个朋友通过您分享的海报关注了我们。";
                                }
                            }

                            OASent sendMessage = new OASent()
                            {
                                id = 0,
                                MsgType = "text",
                                FromUserName = _settings.originalId,
                                ToUserName = poster.open_id,
                                Content = msgText.Trim()
                            };
                            api.SendServiceMessage(sendMessage);
                        }
                    }


                }
                
            }
            else
            {
                xmlD.LoadXml("<xml>"
                + "<ToUserName><![CDATA[" + _message.FromUserName.Trim() + "]]></ToUserName>"
                + "<FromUserName ><![CDATA[" + _settings.originalId.Trim() + "]]></FromUserName>"
                + "<CreateTime >" + Util.GetLongTimeStamp(DateTime.Now) + "</CreateTime>"
                + "<MsgType><![CDATA[text]]></MsgType>"
                + "<Content><![CDATA[感谢您的关注，回复“听课”，立即参与「0元免费领」活动！]]></Content>"
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
            //string landingPageUrl = "http://weixin.luqinwenda.com/service/pages/PosterLanding/Index/" + userId.ToString();
            //string imageUrl = "http://weixin.luqinwenda.com/subscribe/api/Image/CreatePersonalPosterWithTextQrCode?templatePath=%2Fimages%2Ftemplate2_small.jpg&x=580&y=975&scale=125&qrCodeText=" + Util.UrlEncode(landingPageUrl);
            string imageUrl = "http://weixin.luqinwenda.com/subscribe/show_poster.html?userid=" + userId.ToString();
            XmlDocument xmlD = new XmlDocument();
            xmlD.LoadXml("<xml>"
                + "<ToUserName><![CDATA[" + _message.FromUserName.Trim() + "]]></ToUserName>"
                + "<FromUserName ><![CDATA[" + _settings.originalId.Trim() + "]]></FromUserName>"
                + "<CreateTime >" + Util.GetLongTimeStamp(DateTime.Now) + "</CreateTime>"
                + "<MsgType><![CDATA[text]]></MsgType>"
                + "<Content><![CDATA[感谢您的关注！\r\n<a href=\"" + imageUrl + "\" >生成我的专属海报</a>\r\n下载海报后，将海报分享至朋友圈或微信群，邀请3位或3位以上好友关注我们即可解锁课程，在悦长大后台直接领取！]]></Content>"
                + "</xml>");
            return xmlD;
        }
    }
}
