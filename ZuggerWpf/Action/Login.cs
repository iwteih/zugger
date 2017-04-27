using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Reflection;
using log4net;

namespace ZuggerWpf
{
    partial class Login : ActionBase
    {
        public override bool Action()
        {
            bool isSuccess = false;

            ApplicationConfig appconfig = IOHelper.LoadIsolatedData();

            if (appconfig.IsConfigUpdated || LastGetSessionTime.AddMinutes(SessionTimeOut) <= DateTime.Now)
            {
                //请求得到session
                isSuccess = PMSUtil.GetSession(appconfig.SessionUrl, ref SessionName, ref SessionID, ref RandomNumber);

                if (isSuccess
                    && !string.IsNullOrEmpty(appconfig.UserName)
                    && !string.IsNullOrEmpty(appconfig.Password))
                {
                    isSuccess = PMSUtil.DoLogin(appconfig.LoginUrl, appconfig.UserName, appconfig.Password, RandomNumber, SessionName, SessionID);

                    if (isSuccess)
                    {
                        LastGetSessionTime = DateTime.Now;
                    }

                    appconfig.IsConfigUpdated = false;
                }               
            }
            else
            {
                isSuccess = true;
            }

            return isSuccess;
        }
    }
}
