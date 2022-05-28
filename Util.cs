using System;
using System.Web;
using System.Net;
using System.IO;
namespace LuqinOfficialAccount
{
    public class Util
    {
        public static bool isDev = false;

        public static string workingPath = $"{Environment.CurrentDirectory}";

        public static void DownloadFile(string url, string fileName, string path)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "GET";
            HttpWebResponse res = (HttpWebResponse)req.GetResponse();
            Stream s = res.GetResponseStream();
            string fileNamePath = workingPath + path + "/" + fileName.Trim();
            if (File.Exists(fileNamePath))
            {
                File.Delete(fileNamePath);
            }
            using (FileStream fs = File.OpenWrite(fileNamePath))
            {
                int b = s.ReadByte();
                for (; b != -1;)
                {
                    fs.WriteByte((byte)b);
                    b = s.ReadByte();
                }

                fs.Close();
                fs.Dispose();
            }


            
            s.Close();
            
            res.Close();
            req.Abort();
            
        }

        public static string GetLongTimeStamp(DateTime currentDateTime)
        {
            TimeSpan ts = currentDateTime - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalMilliseconds).ToString();
        }

        public static DateTime GetDateTimeByTimeStamp(long timeStamp)
        {
            DateTime date = DateTime.Parse("1970-1-1 0:0:0");
            return date.AddMilliseconds(timeStamp);
        }

        public static string UrlEncode(string urlStr)
        {
            return HttpUtility.UrlEncode(urlStr.Trim().Replace(" ", "+").Replace("'", "\""));
        }
        public static string GetWebContent(string url)
        {
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "GET";
                HttpWebResponse res = (HttpWebResponse)req.GetResponse();
                Stream s = res.GetResponseStream();
                StreamReader sr = new StreamReader(s);
                string str = sr.ReadToEnd();
                sr.Close();
                s.Close();
                res.Close();
                req.Abort();
                return str;
            }
            catch
            {
                return "";
            }
        }
        public static string GetWebContent(string url, string postData)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "POST";
            Stream sPost = req.GetRequestStream();
            StreamWriter sw = new StreamWriter(sPost);
            sw.Write(postData);
            sw.Close();
            sPost.Close();
            HttpWebResponse res = (HttpWebResponse)req.GetResponse();
            Stream s = res.GetResponseStream();
            StreamReader sr = new StreamReader(s);
            string str = sr.ReadToEnd();
            sr.Close();
            s.Close();
            return str;
        }
    }
}
