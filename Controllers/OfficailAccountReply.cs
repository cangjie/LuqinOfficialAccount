﻿using System;
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
                var scanList = _context.posterScanLog
                .Where(s => (s.scan_user_id == subscriberId
                && s.create_date <  Util.GetDateTimeByTimeStamp(1000 * long.Parse(_message.CreateTime)).AddHours(8)
                ))
                .OrderByDescending(s => s.id).ToList();
                if (scanList.Count > 0)
                {
                    scan = scanList[0];
                }
                
            }
            catch(Exception err)
            {
                Console.WriteLine(err.ToString());
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
                OAUser posterUser = _context.oAUser.Where(u => (
                    u.original_id.Trim().Equals(_message.ToUserName.Trim())
                    && u.user_id == scan.poster_user_id)).First();

                OAUser scanUser = _context.oAUser.Where(u => (
                    u.original_id.Trim().Equals(_message.ToUserName.Trim())
                    && u.user_id == scan.scan_user_id)).First();

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
                + "<Content><![CDATA[感谢您通过您的朋友分享的海报关注到我们，您也可以回复“听课”来和您的其他朋友分享。]]></Content>"
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
                    string msgText = "";
                    if (promoteTotal != null && promoteTotal.Count >= 1)
                    {
                        try
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
                            
                        }
                        catch
                        {

                        }
                        msgText = "已经有" + promoteTotal.Count.ToString() + "个朋友通过您的海报关注了我们的公众号，"
                            + "您可以<a href='https://mp.weixin.qq.com/s/Vy3EhVGCTA7LpR3U0TTMeg' >点击此处</a>开始聆听卢老师的收费课程。";
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
            //string landingPageUrl = "http://weixin.luqinwenda.com/service/pages/PosterLanding/Index/" + userId.ToString();
            //string imageUrl = "http://weixin.luqinwenda.com/subscribe/api/Image/CreatePersonalPosterWithTextQrCode?templatePath=%2Fimages%2Ftemplate2_small.jpg&x=578&y=945&scale=125&qrCodeText=" + Util.UrlEncode(landingPageUrl);
            string imageUrl = "http://weixin.luqinwenda.com/subscribe/show_poster.html?userid=" + userId.ToString();
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
