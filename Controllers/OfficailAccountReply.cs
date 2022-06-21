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
                            //xmlD = GetPosterMApp();
                            xmlD = SubscribePoster();
                            retStr = xmlD.InnerXml.Trim();
                            break;
                        case "1":
                            xmlD = Help();
                            retStr = xmlD.InnerXml.Trim();
                            break;
                        case "治愈动画":
                            xmlD = ZhiyuCatoon();
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
                case "help":
                    xmlD = Help();
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
            var assetList = _context.userMediaAsset.Where(a => (a.user_id == userId && a.media_id == 4)).ToList();
            if (assetList != null && assetList.Count > 0)
            {
                //string message = "您可以<a href='https://mp.weixin.qq.com/s/tOUNhLcJMp4uqkDG4PTCKA' >点击此处</a>开始聆听卢老师的收费课程。";
                string message = "您可以<a data-miniprogram-appid=\"wx34bd31c8bf72b589\" data-miniprogram-path=\"pages/customer/media/quick_player?id=4\" href=\"https://mp.weixin.qq.com/s/tOUNhLcJMp4uqkDG4PTCKA\" >点击此处</a>开始聆听卢老师的收费课程。";
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
                xmlD = GetPosterMApp();
            }

            
            return xmlD;
        }

        public XmlDocument ZhiyuCatoon()
        {
            string message = "跳跳羊： https://b23.tv/41qUv6I\r\n"
                + "远在天边： https://b23.tv/2CSc8mP\r\n"
                + "狮子王：http://m.v.qq.com/x/cover/x/lh4cj69w87hfodz/a00136jovza.html?&url_from=share&second_share=0&share_from=copy\r\n"
                + "心灵奇旅：https://v.qq.com/x/cover/mzc002009xj5sve/m0039lbpnkr.html?n_version=2021\r\n"
                + "龙猫：http://m.iqiyi.com/v_19rqv721o4.html?social_platform=link&p1=2_22_221&_psc=58d4e8eb6ed44e79a4f4a9f942dba80f\r\n"
                + "寻梦环游记：http://m.v.qq.com/x/cover/x/vjrvzv3s517g6m8/w0025hgvzin.html?&url_from=share&second_share=0&share_from=copy&pgid=page_detail&mod_id=mod_toolbar_new";

            XmlDocument xmlD = new XmlDocument();
            xmlD.LoadXml("<xml>"
                + "<ToUserName><![CDATA[" + _message.FromUserName.Trim() + "]]></ToUserName>"
                + "<FromUserName ><![CDATA[" + _settings.originalId.Trim() + "]]></FromUserName>"
                + "<CreateTime >" + Util.GetLongTimeStamp(DateTime.Now) + "</CreateTime>"
                + "<MsgType><![CDATA[text]]></MsgType>"
                + "<Content><![CDATA[" + message.Trim() + "]]></Content>"
                + "</xml>");
            return xmlD;
        }

        public XmlDocument Help()
        {
            OfficialAccountApi api = new OfficialAccountApi(_context, _config);
            
            OASent sendMessage = new OASent()
            {
                id = 0,
                MsgType = "video",
                FromUserName = _settings.originalId,
                ToUserName = _message.FromUserName.Trim(),
                Content = "b4jyA__yqy1crwzwSktKUwMf04GKGqzAjPbM2PmGdmFY13sBy7otre5t23h55w33",
                Thumb = "DKLqmuqTxW5A3Bbrn9Ff78ARoT-N-m9sVRyTVD4n66vfEUeeb9jkW6WC2QWPxnUE"
            };
            api.SendServiceMessage(sendMessage);
            /*
            sendMessage = new OASent()
            {
                id = 0,
                MsgType = "text",
                FromUserName = _settings.originalId,
                ToUserName = _message.FromUserName.Trim(),
                Content = "如何下载专属海报？"
            };
            api.SendServiceMessage(sendMessage);
            */
            XmlDocument xmlD = new XmlDocument();
            xmlD.LoadXml("<xml>"
                + "<ToUserName><![CDATA[" + _message.FromUserName.Trim() + "]]></ToUserName>"
                + "<FromUserName ><![CDATA[" + _settings.originalId.Trim() + "]]></FromUserName>"
                + "<CreateTime >" + Util.GetLongTimeStamp(DateTime.Now) + "</CreateTime>"
                + "<MsgType><![CDATA[video]]></MsgType>"
                + "<Video><MediaId><![CDATA[b4jyA__yqy1crwzwSktKU2tm88qzh3s2Uka3RfwUZCK-t9MRViZJD25St7LRPe8J]]></MediaId><Title><![CDATA[]]></Title><Description><![CDATA[]]></Description ></Video>"
                + "</xml>");

            return xmlD;
        }


        public XmlDocument SubscribePoster()
        {
            UserController uc = new UserController(_context, _config);
            int userId = uc.CheckUser(_message.FromUserName.Trim());
            string landingPage = "https://mini.luqinwenda.com/mapp/customer/poster/landing?id=" + userId;
            string img_url = "http://weixin.luqinwenda.com/subscribe/api/Image/CreatePersonalPosterWithTextQrCode?templatePath=%2Fimages%2Ftemplate_new.jpg&x=1000&y=1695&scale=300&qrCodeText="
                + System.Web.HttpUtility.UrlEncode(landingPage) + "&rnd=" + Util.GetLongTimeStamp(DateTime.Now);
            string fileName = "poster_" + userId.ToString() + ".jpg";
            Util.DownloadFile(img_url, fileName.Trim(),  "/images");
            OfficialAccountApi api = new OfficialAccountApi(_context, _config);
            string mediaId = api.UploadImageToWeixin("/images/" + fileName.Trim(), "image");
            //string mediaId = "";

            if (mediaId.Trim().Equals(""))
            {
                return new XmlDocument();
            }

            OASent sendMessage = new OASent()
            {
                id = 0,
                MsgType = "text",
                FromUserName = _settings.originalId,
                ToUserName = _message.FromUserName.Trim(),
                Content = "感谢您的关注！\r\n下方海报保存到手机👇👇👇\r\n并分享至朋友圈或微信群，邀请3位好友关注我们，即可在悦长大后台直接领取课程！"
            };
            api.SendServiceMessage(sendMessage);
            XmlDocument xmlD = new XmlDocument();
            xmlD.LoadXml("<xml>"
                + "<ToUserName><![CDATA[" + _message.FromUserName.Trim() + "]]></ToUserName>"
                + "<FromUserName ><![CDATA[" + _settings.originalId.Trim() + "]]></FromUserName>"
                + "<CreateTime >" + Util.GetLongTimeStamp(DateTime.Now) + "</CreateTime>"
                + "<MsgType><![CDATA[image]]></MsgType>"
                + "<Image><MediaId><![CDATA[" + mediaId.Trim() + "]]></MediaId></Image>"
                + "</xml>");

            return xmlD;
        }

        public XmlDocument CheckSubscribe()
        {
            XmlDocument xmlD = new XmlDocument();
            UserController uc = new UserController(_context, _config);
            DateTime submitTime = Util.GetDateTimeByTimeStamp(1000 * long.Parse(_message.CreateTime)).AddHours(8);
            int subscriberId = uc.CheckUser(_message.FromUserName.Trim());

            OAUser scanUser = _context.oAUser.Where(u => (
                u.original_id.Trim().Equals(_message.ToUserName.Trim())
                && u.open_id.Trim().Equals(_message.FromUserName.Trim()))).First();

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
                && s.create_date >= submitTime.AddMinutes(-30)
                && s.scan_user_id == scanUser.user_id
                ))
                .OrderBy(s => s.id).ToList();
                if (scanList != null && scanList.Count > 0)
                {
                    scan = scanList[0];
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

                

                var promoteList = _context.promote.Where(p => (
                    p.original_id.Trim().Equals(_message.ToUserName.Trim())
                    &&  p.promote_open_id.Trim().Equals(posterUser.open_id.Trim())
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
                /*
                xmlD.LoadXml("<xml>"
                + "<ToUserName><![CDATA[" + _message.FromUserName.Trim() + "]]></ToUserName>"
                + "<FromUserName ><![CDATA[" + _settings.originalId.Trim() + "]]></FromUserName>"
                + "<CreateTime >" + Util.GetLongTimeStamp(DateTime.Now) + "</CreateTime>"
                + "<MsgType><![CDATA[text]]></MsgType>"
                + "<Content><![CDATA[感谢您的关注，回复“听课”，立即参与「0元免费领」活动！]]></Content>"
                + "</xml>");
                */
                xmlD = SubscribePoster();
                try
                {

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
                                            media_id = 4,
                                            user_id = scan.poster_user_id,

                                        };
                                        _context.userMediaAsset.Add(uma);
                                        _context.SaveChanges();
                                       
                                    }
                                    msgText = "已经有" + promoteTotal.Count.ToString() + "个朋友通过您的海报关注了我们的公众号，"
                                        + "您可以<a data-miniprogram-appid=\"wx34bd31c8bf72b589\" data-miniprogram-path=\"pages/customer/media/quick_player?id=4\" href=\"https://mp.weixin.qq.com/s/tOUNhLcJMp4uqkDG4PTCKA\" >点击此处</a>开始聆听卢老师的收费课程。";

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
                catch
                {

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
            string imageUrl = "http://weixin.luqinwenda.com/subscribe/show_poster.html?userid=" + userId.ToString() + "&rnd=" + Util.GetLongTimeStamp(DateTime.Now);
            XmlDocument xmlD = new XmlDocument();
            xmlD.LoadXml("<xml>"
                + "<ToUserName><![CDATA[" + _message.FromUserName.Trim() + "]]></ToUserName>"
                + "<FromUserName ><![CDATA[" + _settings.originalId.Trim() + "]]></FromUserName>"
                + "<CreateTime >" + Util.GetLongTimeStamp(DateTime.Now) + "</CreateTime>"
                + "<MsgType><![CDATA[text]]></MsgType>"
                + "<Content><![CDATA[感谢您的关注！\r\n免费领取卢勤家庭教育课请点击蓝字：<a href=\"" + imageUrl + "\" >生成我的专属海报</a>\r\n下载海报后，将海报分享至朋友圈或微信群，邀请3位或3位以上好友关注我们即可解锁课程，在悦长大后台直接领取！]]></Content>"
                + "</xml>");
            return xmlD;
        }

        public XmlDocument GetPosterMApp()
        {
            UserController user = new UserController(_context, _config);
            int userId = user.CheckUser(_message.FromUserName);
            if (userId == 0)
            {
                return new XmlDocument();
            }
            //string landingPageUrl = "http://weixin.luqinwenda.com/service/pages/PosterLanding/Index/" + userId.ToString();
            //string imageUrl = "http://weixin.luqinwenda.com/subscribe/api/Image/CreatePersonalPosterWithTextQrCode?templatePath=%2Fimages%2Ftemplate2_small.jpg&x=580&y=975&scale=125&qrCodeText=" + Util.UrlEncode(landingPageUrl);
            string imageUrl = "http://weixin.luqinwenda.com/subscribe/show_poster_mapp.html?userid=" + userId.ToString()+"&rnd=" + Util.GetLongTimeStamp(DateTime.Now);
            XmlDocument xmlD = new XmlDocument();
            xmlD.LoadXml("<xml>"
                + "<ToUserName><![CDATA[" + _message.FromUserName.Trim() + "]]></ToUserName>"
                + "<FromUserName ><![CDATA[" + _settings.originalId.Trim() + "]]></FromUserName>"
                + "<CreateTime >" + Util.GetLongTimeStamp(DateTime.Now) + "</CreateTime>"
                + "<MsgType><![CDATA[text]]></MsgType>"
                + "<Content><![CDATA[感谢您的关注！\r\n免费领取卢勤家庭教育课请点击蓝字：<a href=\"" + imageUrl + "\" >生成我的专属海报</a>\r\n下载海报后，将海报分享至朋友圈或微信群，邀请3位或3位以上好友关注我们即可解锁课程，在悦长大后台直接领取！]]></Content>"
                + "</xml>");
            return xmlD;
        }
    }
}
