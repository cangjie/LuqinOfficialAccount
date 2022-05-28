using System;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Xml;
using Microsoft.Extensions.Configuration;
using LuqinOfficialAccount.Models;
using System.Security.Cryptography;
using Newtonsoft.Json;
using System.Runtime.Serialization.Json;
using System.Collections.Generic;
using System.Net;
using System.Linq;

namespace LuqinOfficialAccount.Controllers
{
    public class FormItem
    {
        public string Name { get; set; }
        public ParamType ParamType { get; set; }
        public string Value { get; set; }
    }

    public enum ParamType
    {
        ///
        /// 文本类型
        ///
        Text,
        ///
        /// 文件路径，需要全路径（例：C:\A.JPG)
        ///
        File
    }
    public static class Funcs
    {
        public static string PostFormData(List<FormItem> list, string uri)
        {
            string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
            //请求 
            WebRequest req = WebRequest.Create(uri);
            req.Method = "POST";
            req.ContentType = "multipart/form-data; boundary=" + boundary;
            //组织表单数据 
            StringBuilder sb = new StringBuilder();
            foreach (FormItem item in list)
            {
                switch (item.ParamType)
                {
                    case ParamType.Text:
                        sb.Append("--" + boundary);
                        sb.Append("\r\n");
                        sb.Append("Content-Disposition: form-data; name=\"" + item.Name + "\"");
                        sb.Append("\r\n\r\n");
                        sb.Append(item.Value);
                        sb.Append("\r\n");
                        break;
                    case ParamType.File:
                        sb.Append("--" + boundary);
                        sb.Append("\r\n");
                        sb.Append("Content-Disposition: form-data; name=\"media\"; filename=\"" + item.Value + "\"");
                        sb.Append("\r\n");
                        sb.Append("Content-Type: application/octet-stream");
                        sb.Append("\r\n\r\n");
                        break;
                }
            }
            string head = sb.ToString();
            //post字节总长度
            long length = 0;
            byte[] form_data = Encoding.UTF8.GetBytes(head);
            //结尾 
            byte[] foot_data = Encoding.UTF8.GetBytes("\r\n--" + boundary + "--\r\n");
            List<FormItem> fileList = list.Where(f => f.ParamType == ParamType.File).ToList();
            length = form_data.Length + foot_data.Length;
            foreach (FormItem fi in fileList)
            {
                FileStream fileStream = new FileStream(fi.Value, FileMode.Open, FileAccess.Read);
                length += fileStream.Length;
                fileStream.Close();
            }
            req.ContentLength = length;

            Stream requestStream = req.GetRequestStream();
            //发送表单参数 
            requestStream.Write(form_data, 0, form_data.Length);
            foreach (FormItem fd in fileList)
            {
                FileStream fileStream = new FileStream(fd.Value, FileMode.Open, FileAccess.Read);
                //文件内容 
                byte[] buffer = new Byte[checked((uint)Math.Min(4096, (int)fileStream.Length))];
                int bytesRead = 0;
                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                    requestStream.Write(buffer, 0, bytesRead);
                //结尾 
                requestStream.Write(foot_data, 0, foot_data.Length);
            }
            requestStream.Close();

            //响应 
            WebResponse pos = req.GetResponse();
            StreamReader sr = new StreamReader(pos.GetResponseStream(), Encoding.UTF8);
            string html = sr.ReadToEnd().Trim();
            sr.Close();
            if (pos != null)
            {
                pos.Close();
                pos = null;
            }
            if (req != null)
            {
                req = null;
            }
            return html;
        }
        ///
        /// 从URL地址下载文件到本地磁盘
        ///
        /// 本地磁盘地址
        /// URL网址
        ///
        public static string SaveFileFromUrl(string FileName, string Url)
        {
            WebResponse response = null;
            Stream stream = null;
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
                response = request.GetResponse();
                stream = response.GetResponseStream();

                if (!response.ContentType.ToLower().StartsWith("text/"))
                {
                    SaveBinaryFile(response, FileName);
                }
                else
                {
                    StreamReader sr = new StreamReader(stream, System.Text.Encoding.UTF8);
                    return sr.ReadToEnd();
                }

            }
            catch (Exception err)
            {
                return err.ToString();
            }
            return "complete";
        }
        ///
        /// 将二进制文件保存到磁盘
        ///
        /// 将二进制文件保存到磁盘
        // 将二进制文件保存到磁盘
        private static bool SaveBinaryFile(WebResponse response, string FileName)
        {
            bool Value = true;
            byte[] buffer = new byte[1024];

            try
            {
                if (File.Exists(FileName))
                    File.Delete(FileName);
                Stream outStream = System.IO.File.Create(FileName);
                Stream inStream = response.GetResponseStream();

                int l;
                do
                {
                    l = inStream.Read(buffer, 0, buffer.Length);
                    if (l > 0)
                        outStream.Write(buffer, 0, l);
                }
                while (l > 0);

                outStream.Close();
                inStream.Close();
            }
            catch
            {
                Value = false;
            }
            return Value;
        }
    }


    [Route("api/[controller]/[Action]")]
    [ApiController]
    public class OfficialAccountApi:ControllerBase
    {
        private readonly AppDBContext _context;

        private readonly IConfiguration _config;

        private readonly Settings _settings;

        public OfficialAccountApi(AppDBContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
            _settings = Settings.GetSettings(_config);
        }

        [HttpGet]
        public async Task<ActionResult<string>> PushMessage(string signature,
            string timestamp, string nonce, string echostr)
        {
            return echostr.Trim();
        }



        [HttpPost]
        public async Task<ActionResult<string>> PushMessage([FromQuery]string signature,
            [FromQuery] string timestamp, [FromQuery] string nonce)
        {
            string[] validStringArr = new string[] { _settings.token.Trim(), timestamp.Trim(), nonce.Trim() };
            Array.Sort(validStringArr);
            string validString = String.Join("", validStringArr);
            SHA1 sha = SHA1.Create();
            ASCIIEncoding enc = new ASCIIEncoding();
            byte[] bArr = enc.GetBytes(validString);
            bArr = sha.ComputeHash(bArr);
            string validResult = "";
            for (int i = 0; i < bArr.Length; i++)
            {
                validResult = validResult + bArr[i].ToString("x").PadLeft(2, '0');
            }
            if (validResult != signature)
            {
                return NoContent(); 
            }
            string body = "";
            var stream = Request.Body;
            if (stream != null)
            {
        
                using (var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, true))
                {
                    body = await reader.ReadToEndAsync();

                    string path = $"{Environment.CurrentDirectory}";
                    string dateStr = DateTime.Now.Year.ToString() + DateTime.Now.Month.ToString().PadLeft(2, '0')
                        + DateTime.Now.Day.ToString().PadLeft(2, '0');
                    if (path.StartsWith("/"))
                    {
                        path = path + "/";
                    }
                    else
                    {
                        path = path + "\\";
                    }
                    path = path + "wechat_post_" + dateStr + ".txt";
                    using (StreamWriter fw = new StreamWriter(path, true))
                    {
                        fw.WriteLine(body.Trim());
                        fw.Close();
                    }
                }
                
            }
            try
            {
                XmlDocument xmlD = new XmlDocument();
                xmlD.LoadXml(body);
                XmlNode root = xmlD.SelectSingleNode("//xml");

                string eventStr = "";
                string eventKey = "";
                string content = "";
                string msgId = "";
                string msgType = root.SelectSingleNode("MsgType").InnerText.Trim();

                if (msgType.Trim().Equals("event"))
                {
                    eventStr = root.SelectSingleNode("Event").InnerText.Trim();
                    eventKey = root.SelectSingleNode("EventKey").InnerText.Trim();
                }
                else
                {
                    content = root.SelectSingleNode("Content").InnerText.Trim();
                    msgId = root.SelectSingleNode("MsgId").InnerText.Trim();
                    msgType = root.SelectSingleNode("MsgType").InnerText.Trim();
                }

                OARecevie msg = new OARecevie()
                {
                    id = 0,
                    ToUserName = root.SelectSingleNode("ToUserName").InnerText.Trim(),
                    FromUserName = root.SelectSingleNode("FromUserName").InnerText.Trim(),
                    CreateTime = root.SelectSingleNode("CreateTime").InnerText.Trim(),
                    MsgType = msgType,
                    Event = eventStr,
                    EventKey = eventKey,
                    MsgId = msgId,
                    Content = content

                };
                _context.oARecevie.Add(msg);
                _context.SaveChanges();
                OfficailAccountReply reply = new OfficailAccountReply(_context, _config, msg);
                return reply.Reply().Trim();
            }
            catch
            {

            }
            return "success";
        }

        [HttpGet]
        public ActionResult<string> TestReply(int id)
        {
            OARecevie msg = _context.oARecevie.Find(id);
            OfficailAccountReply reply = new OfficailAccountReply(_context, _config, msg);
            return reply.Reply();
        }

        [HttpGet]
        public void RefreshAccessToken()
        {
            GetAccessToken();
        }

        [NonAction]
        public string GetAccessToken()
        {
            string tokenFilePath = $"{Environment.CurrentDirectory}";
            tokenFilePath = tokenFilePath + "/access_token.official_account";
            string token = "";
            string tokenTime = Util.GetLongTimeStamp(DateTime.Parse("1970-1-1"));
            string nowTime = Util.GetLongTimeStamp(DateTime.Now);
            bool fileExists = false;
            if (System.IO.File.Exists(tokenFilePath))
            {
                fileExists = true;
                using (StreamReader sr = new StreamReader(tokenFilePath))
                {
                    try
                    {
                        token = sr.ReadLine();
                    }
                    catch
                    {

                    }
                    try
                    {
                        tokenTime = sr.ReadLine();
                    }
                    catch
                    {

                    }
                    sr.Close();
                }
                long timeDiff = long.Parse(nowTime) - long.Parse(tokenTime);
                TimeSpan ts = new TimeSpan(0, 0, 0, 0, (int)timeDiff);
                //TimeSpan ts = new TimeSpan()
                if (ts.TotalSeconds > 3600)
                {
                    token = "";
                    if (fileExists)
                    {
                        System.IO.File.Delete(tokenFilePath);
                    }
                }
                else
                {
                    return token.Trim();
                    //return "";
                }
            }
            string getTokenUrl = "https://api.weixin.qq.com/cgi-bin/token?grant_type=client_credential&appid="
                + _settings.appId.Trim() + "&secret=" + _settings.appSecret.Trim();
            try
            {
                string ret = Util.GetWebContent(getTokenUrl);
                AccessToken at = JsonConvert.DeserializeObject<AccessToken>(ret);
                if (!at.access_token.Trim().Equals(""))
                {
                    System.IO.File.AppendAllText(tokenFilePath, at.access_token + "\r\n" + nowTime);
                    return at.access_token.Trim();
                    //return "";
                }
                else
                {
                    return "";
                }
            }
            catch
            {
                return "";
            }

        }

        [HttpGet]
        public ActionResult<string> GetUnionId(string openId)
        {
            string token = GetAccessToken().Trim();
            string getInfoUrl = "https://api.weixin.qq.com/cgi-bin/user/info?access_token="
                + token + "&openid=" + openId.Trim() + "&lang=zh_CN";
            string jsonStr = Util.GetWebContent(getInfoUrl);

            UserInfo info = JsonConvert.DeserializeObject<UserInfo>(jsonStr);

            return info.unionid;
        }

        
        [NonAction]
        public string SendServiceMessage(OASent message)
        {
            string result = "";
            string token = GetAccessToken().Trim();
            string sentUrl = "https://api.weixin.qq.com/cgi-bin/message/custom/send?access_token=" + token.Trim();
            string postJson = "";
            string messageJson = "";
            switch (message.MsgType.Trim())
            {
                case "text":
                default:
                    messageJson = "\"msgtype\": \"text\", \"text\": {\"content\":\"" + message.Content.Trim() + "\" }";
                    break;
            }
            postJson = "{\"touser\":\"" + message.ToUserName.Trim() + "\", " + messageJson.Trim() + " }";
            string resultJson = Util.GetWebContent(sentUrl, postJson);
            ApiResult resultObj = JsonConvert.DeserializeObject<ApiResult>(resultJson);
            message.err_code = resultObj.errcode.ToString();
            message.err_msg = resultObj.errmsg.ToString();
            message.is_service = 1;
            message.origin_message_id = 0;
            try
            {
                _context.oASent.Add(message);
                _context.SaveChanges();
            }
            catch
            {

            }
            return result.Trim();
        }


        [HttpGet]
        public ActionResult<string> TestSendServiceMessage(string openId)
        {
            OASent msg = new OASent()
            {
                id = 0,
                ToUserName = openId,
                FromUserName = _settings.originalId,
                MsgType = "text",
                Content = "测试"
            };
            return SendServiceMessage(msg);
        }

        [HttpGet]
        public ActionResult<string> GetDraft(int offSet)
        {
           
            string token = GetAccessToken();
            string url = "https://api.weixin.qq.com/cgi-bin/draft/batchget?access_token=" + token.Trim();
            string postJson = "{ \"offset\":" + offSet.ToString() + ", \"count\":20, \"no_content\":1 }";
            string resultJson = Util.GetWebContent(url, postJson);
            //DraftResult r = JsonConvert.DeserializeObject<DraftResult>(resultJson);
            return resultJson.Trim();
        }

        

        [HttpGet]
        public  string UploadImageToWeixin(string path, string type)
        {
            path = Util.workingPath + path;
            string token = GetAccessToken().Trim();
            List<FormItem> list = new List<FormItem>();

            list.Add(new FormItem()
            {
                Name = "access_token",
                ParamType = ParamType.Text,
                Value = token
            });
            //添加FORM表单中这条数据的类型，目前只做了两种，一种是文本，一种是文件
            list.Add(new FormItem()
            {
                Name = "type",
                Value = "image",
                ParamType = ParamType.Text
            });
            //添加Form表单中文件的路径，路径必须是基于硬盘的绝对路径
            list.Add(new FormItem()
            {
                Name = "media",
                Value = path,
                ParamType = ParamType.File
            });
            //通过Funcs静态类中的PostFormData方法，将表单数据发送至http://file.api.weixin.qq.com/cgi-bin/media/upload腾讯上传下载文件接口
            string jsonStr = Funcs.PostFormData(list, "https://api.weixin.qq.com/cgi-bin/media/upload?access_token="
                + token.Trim() + "&type=" + type.Trim() );

            Newtonsoft.Json.Linq.JObject ret = (Newtonsoft.Json.Linq.JObject)JsonConvert.DeserializeObject(jsonStr);

            try
            {
                return ret.GetValue("media_id").ToString().Trim();
            }
            catch
            {
                return "";
            }
        }

        protected class UserInfo
        {
            public int subscribe = 0;
            public string openid = "";
            public string language = "";
            public long subscribe_time = 0;
            public string unionid = "";
            public string remark = "";
            public int groupid = 0;
            public int[] tagid_list = new int[] { 0, 0 };
            public string subscribe_scene = "";
            public string qr_scene = "";
            public string qr_scene_str = "";
        }

        protected class UserToken
        {
            public string access_token = "";
            public int expires_in = 0;
            public string refresh_token = "";
            public string openid = "";
            public string scope = "";
        }

        protected class AccessToken
        {
            public string access_token = "";
            public int expires_in = 0;

        }

        protected class ApiResult
        {
            public int errcode = -1;
            public string errmsg = "";
        }

        public class DraftResult
        {
            public int total_count = 0;
            public int item_count = 0;
            public DraftItem[] item;
            public class DraftItem
            {
                public string media_id = "";
                public int update_time;
                public ContentStrct content;
                public class ContentStrct
                {

                    public int create_time = 0;
                    public int update_time = 0;
                    public NewsItem[] news_item;
                    public class NewsItem
                    {
                        public string title = "";
                        public string url = "";
                        public string thumb_url = "";
                    }
                }
            }
        }
    }
}
