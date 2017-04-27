using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Diagnostics;

namespace ZuggerWpf
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class About : Window
    {
        Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

        private Window ParentForm { get; set; }

        public About(Window window)
        {
            InitializeComponent();

            ParentForm = window;
        }

        private void btnCheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            string latestVersion;
            bool isUpdate = Util.GetLatestVersionNumber(out latestVersion);

            if (isUpdate)
            {
                lbUpdate.Text = string.Format("检测到新版本{0}{1}可至上面项目网址升级", latestVersion, Environment.NewLine);
            }
            else
            {
                lbUpdate.Text = "未检测到新版本";
            }

            lbUpdate.Visibility = System.Windows.Visibility.Visible;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            lbVer.Text = string.Format("Version: {0}.{1}.{2}", version.Major, version.Minor, version.Build);
        }

        private void Close_Up(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            Util.SetWindowsPosNextTo(ParentForm, this);
        }
            
        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(e.Uri.ToString());
            e.Handled = true;
        }
    }
}
