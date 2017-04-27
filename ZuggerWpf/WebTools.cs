using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Reflection;
using System.Net.Cache;
using log4net;

namespace ZuggerWpf
{
    public class WebTools
    {
        private static readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
       
        public static string Download(string url)
        {
            string json = string.Empty;           

            WebClient client = new WebClient();
            try
            {
                json = client.DownloadString(url);
            }
            catch (Exception exp)
            {
                logger.Error(string.Format("发送服务器请求错误:{0}", exp.ToString()));
            }
            finally
            {
                client.Dispose();
            }

            return json;
        }
        
        /*         
        public static string Download(string url)
        {
            return DownloadNew(url, "utf-8", 10000);
        }
          
        public static string DownloadNew(string url, string coder, int timeout)
        {
            HttpWebRequest req = null;
            HttpWebResponse resp = null;
            StreamReader reader = null;
            Stream s = null;

            try
            {
                req = HttpWebRequest.Create(url) as HttpWebRequest;
                req.KeepAlive = true;
                req.Timeout = timeout;
                req.Proxy = null;
                req.CachePolicy = new RequestCachePolicy(RequestCacheLevel.BypassCache);

                using (resp = (HttpWebResponse)req.GetResponse())
                {
                    Encoding encoder = Encoding.GetEncoding(coder);//Encoding.UTF8;

                    if (!string.IsNullOrEmpty(resp.ContentEncoding))
                    {
                        encoder = Encoding.GetEncoding(resp.ContentEncoding);
                    }

                    s = resp.GetResponseStream();

                    StringBuilder sb = new StringBuilder();
                    reader = new StreamReader(s, encoder);

                    using (TextWriter writer = new StringWriter(sb))
                    {
                        writer.WriteLine(reader.ReadToEnd());
                        writer.Flush();
                        writer.Close();
                    }

                    return sb.ToString();
                }
            }
            catch(Exception exp)
            {
                logger.Error(string.Format("发送服务器请求错误:{0}", exp.ToString()));
                return string.Empty;
            }
            finally
            {
                if (s != null)
                {
                    s.Close();
                }

                if (reader != null)
                {
                    reader.Close();
                }

                if (req != null)
                {
                    req.Abort();
                }

                if (resp != null)
                {
                    resp.Close();
                }
            }
        }
        */
    }
}
