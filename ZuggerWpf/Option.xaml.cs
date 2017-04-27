using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.Diagnostics;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using log4net;

namespace ZuggerWpf
{
    /// <summary>
    /// Option.xaml 的交互逻辑
    /// </summary>
    public partial class Option : Window
    {
        private static readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public Option(Window window)
        {
            InitializeComponent();

            ParentForm = window;
        }

        ApplicationConfig appconfig = null;

        //配置的备份，点取消时，使用备份复原
        ApplicationConfig appconfigBack = null;

        private Window ParentForm { get; set; }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            appconfig = IOHelper.LoadIsolatedData();

            if (appconfig != null)
            {
                this.DataContext = appconfig;

                //backup config
                appconfigBack = new ApplicationConfig();
                appconfigBack.IsAutoRun = appconfig.IsAutoRun;
                appconfigBack.IsTaskNotify = appconfig.IsTaskNotify;
                appconfigBack.IsPATH_INFORequest = appconfig.IsPATH_INFORequest;
                appconfigBack.Password = appconfig.Password;
                appconfigBack.PMSHost = appconfig.PMSHost;
                appconfigBack.RequestInterval = appconfig.RequestInterval;
                appconfigBack.UserName = appconfig.UserName;
                appconfigBack.ShowBug = appconfig.ShowBug;
                appconfigBack.ShowOpendByMe = appconfig.ShowOpendByMe;
                appconfigBack.ShowTask = appconfig.ShowTask;
                appconfigBack.ShowStory = appconfig.ShowStory;

                txtPwd.Password = appconfig.Password;

                if (appconfig.IsPATH_INFORequest)
                {
                    ckpi.IsChecked = true;
                }
                else
                {
                    ckget.IsChecked = true;
                }
            }
        }

        private void btnYes_Click(object sender, RoutedEventArgs e)
        {            
            if (!Validation.GetHasError(txtInterval) && CheckDisplayMode() && CheckConn())
            {
                appconfig.Password = txtPwd.Password;

                appconfig.IsConfigUpdated = true;

                string appStartPath = System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

                bool cansetautorun = RunWhenStart(ckAutoRun.IsChecked.Value, "Zugger", System.IO.Path.Combine(appStartPath, "Zugger.exe"));

                IOHelper.SaveIsolatedData(appconfig);

                this.Close();
            }
        }

        public bool RunWhenStart(bool started, string name, string path)
        {
            bool success = false;

            RegistryKey HKLM = null;
            RegistryKey Run = null;

            try
            {
                HKLM = Registry.LocalMachine;
                Run = HKLM.CreateSubKey(@"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run\");

                if (started)
                {
                    Run.SetValue(name, path);
                }
                else
                {
                    if (IsRegeditExit(name))
                    {
                        Run.DeleteValue(name);
                    }
                }

                success = true;
            }
            catch (Exception exp)
            {
                logger.Error(string.Format("自启动设置失败：{0}", exp.ToString()));
            }
            finally
            {
                if (Run != null)
                {
                    Run.Close();
                }

                if (HKLM != null)
                {
                    HKLM.Close();
                }
            }

            return success;
        }

        private bool IsRegeditExit(string name)
        {
            bool _exit = false;

            RegistryKey hkml = Registry.LocalMachine;
            RegistryKey software = null;

            try
            {
                string[] subkeyNames;

                software = hkml.OpenSubKey(@"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run\", true);
                subkeyNames = software.GetValueNames();
                foreach (string keyName in subkeyNames)
                {
                    if (keyName == name)
                    {
                        _exit = true;
                    }
                }
            }
            catch (Exception exp)
            {
                logger.Error(string.Format("注册表操作失败：{0}", exp.ToString()));
            }
            finally
            {
                if (hkml != null)
                {
                    hkml.Close();
                }

                if (software != null)
                {
                    software.Close();
                }
            }

            return _exit;
        }


        private void btnNo_Click(object sender, RoutedEventArgs e)
        {
            //revert the config to default
            appconfig.IsAutoRun = appconfigBack.IsAutoRun;
            appconfig.IsTaskNotify = appconfigBack.IsTaskNotify;
            appconfig.IsPATH_INFORequest = appconfigBack.IsPATH_INFORequest;
            appconfig.Password = appconfigBack.Password;
            appconfig.PMSHost = appconfigBack.PMSHost;
            appconfig.RequestInterval = appconfigBack.RequestInterval;
            appconfig.UserName = appconfigBack.UserName;
            appconfig.ShowBug = appconfigBack.ShowBug;
            appconfig.ShowOpendByMe = appconfigBack.ShowOpendByMe;
            appconfig.ShowTask = appconfigBack.ShowTask;
            appconfig.ShowStory = appconfigBack.ShowStory;

            IOHelper.SaveIsolatedData(appconfig);

            this.Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //this.DragMove();
        }

        private void ckget_Checked(object sender, RoutedEventArgs e)
        {
            appconfig.IsPATH_INFORequest = false;
        }

        private void ckpi_Checked(object sender, RoutedEventArgs e)
        {
            appconfig.IsPATH_INFORequest = true;
        }

        private void txtInterval_KeyDown(object sender, KeyEventArgs e)
        {

        }

        private void txtInterval_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void DisplayCheckBox_Click(object sender, RoutedEventArgs e)
        {
            CheckDisplayMode();
        }

        private bool CheckDisplayMode()
        {
            bool selected = false;

            foreach (var v in pnDispalyMode.Children)
            {
                CheckBox ck = v as CheckBox;

                if (ck != null && ck.IsChecked.Value)
                {
                    selected = true;
                    break;
                }
            }

            if (!selected)
            {
                chooseModeTip.Visibility = Visibility.Visible;
            }
            else
            {
                chooseModeTip.Visibility = Visibility.Collapsed;
            }

            return selected;
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            Util.SetWindowsPosNextTo(ParentForm, this);
        }       

        private void btnCheckCon_Click(object sender, RoutedEventArgs e)
        {
            CheckConn();
        }

        private bool CheckConn()
        {
            bool success = CheckRequesetType();
            if (success)
            {

                string sessionname = string.Empty;
                string sessionid = string.Empty;
                string randomnum = string.Empty;

                //ApplicationConfig appconfig = IOHelper.LoadIsolatedData();

                success = PMSUtil.GetSession(appconfig.SessionUrl, ref sessionname, ref sessionid, ref randomnum);

                if (success)
                {
                    success = PMSUtil.DoLogin(appconfig.LoginUrl, txtUser.Text, txtPwd.Password, randomnum, sessionname, sessionid);

                    lbConnStatus.Content = success ? "登录成功" : "登录失败";
                }
                else
                {
                    lbConnStatus.Content = "PMS地址错误";
                    success = false;
                }                
            }

            lbConnStatus.Visibility = Visibility.Visible;

            return success;
        }

        private bool CheckRequesetType()
        {
            bool result = false;

            //ApplicationConfig config = IOHelper.LoadIsolatedData();

            string configUrl = appconfig.GetPMSConfigUrl;
            //string configUrl = config.BuildPMSConfigUrl(txtPMSUrl.Text.Trim());

            try
            {
                string json = WebTools.Download(configUrl);
                if (json != null && json.IndexOf("requestType") != -1)
                {
                    var jsObj = JsonConvert.DeserializeObject(json) as JObject;

                    if (jsObj != null && jsObj["requestType"].Value<string>().ToUpper() == "GET")
                    {
                        appconfig.IsPATH_INFORequest = false;
                        result = true;
                    }
                    else if (jsObj != null && jsObj["requestType"].Value<string>().ToUpper() == "PATH_INFO")
                    {
                        appconfig.IsPATH_INFORequest = true;
                        result = true;
                    }                    
                }
                else
                {
                    lbConnStatus.Content = "无法获得PMS请求类型";
                }
            }
            catch (Exception exp)
            {
                logger.Error(string.Format("GetPMSConfig Error:{0}", exp.ToString()));
                lbConnStatus.Content = "PMS地址错误";
            }

            return result;
        }      

    }
}
