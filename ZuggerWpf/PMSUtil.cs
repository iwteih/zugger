using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using log4net;


namespace ZuggerWpf
{
    class PMSUtil
    {
        //private static readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static bool GetSession(string loginurl, ref string sessionname, ref string sessionid, ref string randomnum)
        {
            bool isSuccess = false;

            try
            {
                //请求得到session
                string content = WebTools.Download(loginurl);

                if (!string.IsNullOrEmpty(content))
                {
                    var jsObj = JsonConvert.DeserializeObject(content) as JObject;

                    if (jsObj != null && jsObj["status"].Value<string>() == "success")
                    {
                        content = jsObj["data"].Value<string>();

                        jsObj = JsonConvert.DeserializeObject(content) as JObject;

                        if (jsObj.ToString().IndexOf("sessionID") != -1)
                        {
                            //得到session，赋值
                            sessionname = jsObj["sessionName"].Value<string>();
                            sessionid = jsObj["sessionID"].Value<string>();
                            randomnum = jsObj["rand"].Value<string>();

                            isSuccess = true;
                        }
                    }
                }
            }
            catch (Exception exp)
            {
                //logger.Error(string.Format("GetSession Error: {0}", exp.ToString()));
            }

            return isSuccess;
        }

        public static bool DoLogin(string loginurl, string username, string password, string randomnum, string sessoinname, string sessonid)
        {
            bool isSuccess = false;

            try
            {
                if (!string.IsNullOrEmpty(username)
                    && !string.IsNullOrEmpty(password))
                {
                    string pwd = Util.PHPMd5(Util.PHPMd5(password) + randomnum);

                    CookieContainer cc = new CookieContainer();
                    string postData = string.Format("account={0}&password={1}", Util.UrlEncode(username), Util.UrlEncode(pwd));
                    byte[] byteArray = Encoding.UTF8.GetBytes(postData);

                    HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(new Uri(loginurl));
                    cc.Add(new Uri(loginurl), new Cookie(sessoinname, sessonid));
                    webRequest.CookieContainer = cc;
                    webRequest.Method = "POST";
                    webRequest.Accept = "text/javascript, text/html, application/xml, text/xml, */*";
                    webRequest.ContentType = "application/x-www-form-urlencoded";
                    webRequest.UserAgent = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.2; .NET CLR 1.1.4322; .NET CLR 2.0.50727; .NET CLR 3.0.04506.648; .NET CLR 3.5.21022)";
                    webRequest.ContentLength = byteArray.Length;

                    using (Stream newStream = webRequest.GetRequestStream())
                    {
                        newStream.Write(byteArray, 0, byteArray.Length);    //写入参数
                    }

                    using (HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse())
                    {
                        using (StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                        {
                            string json = sr.ReadToEnd();

                            if (json.IndexOf("failed") != -1
                                || json.IndexOf("失败") != -1)
                            {
                                isSuccess = false;
                            }
                            else
                            {
                                isSuccess = true;
                            }
                        }
                    }
                }
            }
            catch (Exception exp)
            {
                //logger.Error(string.Format("Login Error:{0}", exp.ToString()));
            }

            return isSuccess;
        }
    }
}
