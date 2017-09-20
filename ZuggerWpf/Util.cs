using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace ZuggerWpf
{
    enum ItemOperation
    { 
        View,
        Edit,
        Resole,
        Active
    }

    enum ItemType
    { 
        OpenedByme,
        Bug,
        Task,
        Story
    }

    class ConstValue
    { 
        internal const string ZuggerProjectAddress = "http://zugger.codeplex.com/";
    }

    class Util
    {
        //static Regex VersionReg = new Regex(@"<td\s+id\s*=\s*""ReleaseName""\s+class\s*=\s*""ActivityData""\s*>\s*Zugger\s*(?<key>[\d\.]+)\s*</td>", RegexOptions.IgnoreCase);
        static Regex VersionReg = new Regex(@"<a href=.+>Zugger\s*(?<key>[\d\.]+)\s*</a>", RegexOptions.IgnoreCase);

        /// <summary>
        /// 判断是不是有更新，并返回最新的版本号
        /// </summary>
        /// <param name="latestVersion"></param>
        /// <returns></returns>
        public static bool GetLatestVersionNumber(out string latestVersion)
        {
            bool isUpdate = false;

            latestVersion = null;

            string content = WebTools.Download("https://github.com/iwteih/zugger/releases");

            if (!string.IsNullOrEmpty(content))
            {
                Match match = VersionReg.Match(content);

                if (match.Success)
                {
                    latestVersion = match.Groups["key"].Value;

                    Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                    string currentVerson = string.Format("{0}.{1}.{2}", version.Major, version.Minor, version.Build);

                    if (latestVersion != currentVerson)
                    {
                        isUpdate = true;
                    }
                }
            }

            return isUpdate;
        }


        /// <summary>
        /// 设置显示位置
        /// </summary>
        /// <param name="parentWindow">主窗口</param>
        /// <param name="showWindow">要显示的窗口</param>
        public static void SetWindowsPosNextTo(Window parentWindow, Window showWindow)
        {
            //this.Top = ParentForm.Top;
            showWindow.Left = parentWindow.Left;

            //设置位置
            //如果位置超过任务栏，在上显示
            if (parentWindow.Top + parentWindow.ActualHeight + showWindow.ActualHeight >= SystemParameters.WorkArea.Height
                && parentWindow.Top - showWindow.ActualHeight >= 0)
            {
                showWindow.Top = parentWindow.Top - showWindow.ActualHeight;

            }
            //在下显示
            else if (parentWindow.Top + parentWindow.ActualHeight + showWindow.ActualHeight <= SystemParameters.WorkArea.Height)
            {
                showWindow.Top = parentWindow.Top + parentWindow.ActualHeight;
            }

            if (showWindow.Left < 0 || (showWindow.Left + showWindow.ActualWidth > SystemParameters.WorkArea.Width))
            {
                showWindow.Top = parentWindow.Top;

                //在左边显示
                if (parentWindow.Left - showWindow.ActualWidth >= 0)
                {
                    showWindow.Left = parentWindow.Left - showWindow.ActualWidth;

                    if (parentWindow.Top + parentWindow.ActualHeight + showWindow.ActualHeight >= SystemParameters.WorkArea.Height
                && parentWindow.Top - showWindow.ActualHeight >= 0)
                    {
                        showWindow.Top = parentWindow.Top - showWindow.ActualHeight;
                        showWindow.Left = parentWindow.Left - (showWindow.ActualWidth - parentWindow.ActualWidth);

                    }
                }
                //在右边显示
                else if (parentWindow.Left + showWindow.ActualWidth <= SystemParameters.WorkArea.Width)
                {
                    showWindow.Left = parentWindow.Left + parentWindow.ActualWidth;
                }
            }
        }

        public static string URLCombine(string baseUrl, string relativeUrl)
        {
            if (baseUrl.Length == 0)
                return relativeUrl;
            if (relativeUrl.Length == 0)
                return baseUrl;
            return string.Format("{0}/{1}", baseUrl.TrimEnd(new char[] { '/', '\\' }), relativeUrl.TrimStart(new char[] { '/', '\\' }));
        }

        public static string UrlEncode(string input)
        {
            if (!string.IsNullOrEmpty(input))
            {
                return System.Web.HttpUtility.UrlEncode(input);
            }
            else
            {
                return string.Empty;
            }
        }


        /// <summary>
        /// 与下面的PHPMd5效果相同
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string Md5(string input)
        {
            return System.Web.Security.FormsAuthentication.HashPasswordForStoringInConfigFile(input, "MD5");
        }


        /// <summary>
        /// 模拟php中的md5编码，但是对于特殊字符的似乎编码不对
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [Obsolete]
        public static string PHPMd5Encode(string input)
        {
            string Hashpass = input;

            // UTF7 Version (probably won't work across languages)   
            System.Security.Cryptography.MD5CryptoServiceProvider md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            UTF7Encoding encoder = new UTF7Encoding();
            Byte[] bytes;

            bytes = encoder.GetBytes(Hashpass);
            bytes = md5.ComputeHash(bytes);

            string strHex = string.Empty;

            foreach (byte b in bytes)
            {
                strHex = string.Concat(strHex, String.Format("{0:x2}", b));
            }

            return strHex;
        }

        /// <summary>
        /// 模拟php中的md5编码
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string PHPMd5(string plainText)
        {
            // Convert the original password to bytes; then create the hash
            MD5 md5 = new MD5CryptoServiceProvider();
            var originalBytes = Encoding.Default.GetBytes(plainText);
            var encodedBytes = md5.ComputeHash(originalBytes);
            return BitConverter.ToString(encodedBytes).Replace("-", string.Empty).ToLowerInvariant();
        }

        public static string EscapeXmlTag(string input)
        {
            input = input.Replace("&amp;", "&");
            input = input.Replace("&lt;", "<");
            input = input.Replace("&gt;", ">");
            input = input.Replace("&quot;", "\"");
            input = input.Replace("&apos;", "'");

            return input;
        }

        /// <summary>
        /// 打开对应的网页
        /// </summary>
        /// <param name="item"></param>
        internal static void OpenItem(ItemBase item, ItemOperation operation)
        {
            string url = string.Empty;

            if (item != null)
            {
                if (item is BugItem)
                {
                    url = GetBugOperationUrl(operation, item);
                }
                else if (item is TaskItem)
                {
                    url = GetTaskOperationUrl(operation, item);
                }
                else if (item is StoryItem)
                {
                    url = GetStoryOperationUrl(operation, item);
                }

                OpenWebBrowser(url);
            }
        }

        internal static void OpenItem(int newItemsId, ItemType newItemsType, ItemOperation operation)
        {
            string url = string.Empty;

            if (newItemsId > 0)
            {
                if (newItemsType == ItemType.Bug)
                {
                    url = GetBugOperationUrl(operation, newItemsId);
                }
                else if (newItemsType == ItemType.Task)
                {
                    url = GetTaskOperationUrl(operation, newItemsId);
                }
                else if (newItemsType == ItemType.Story)
                {
                    url = GetStoryOperationUrl(operation, newItemsId);
                }

                OpenWebBrowser(url);
            }
        }

        private static void OpenWebBrowser(string url)
        {
            if (!string.IsNullOrEmpty(url))
            {
                ApplicationConfig config = IOHelper.LoadIsolatedData();

                url = string.Concat(url, string.Format("{0}{1}={2}", config.IsPATH_INFORequest ? "?" : "&", ActionBase.SessionName, ActionBase.SessionID));

                System.Diagnostics.Process.Start(url);
            }
        }

        private static string GetBugOperationUrl(ItemOperation operation, int itemId)
        {
            ApplicationConfig appconfig = IOHelper.LoadIsolatedData();

            string url = string.Empty;
            switch (operation)
            {
                case ItemOperation.View:
                    url = string.Format(appconfig.ViewBugUrl, itemId);
                    break;
                case ItemOperation.Edit:
                    url = string.Format(appconfig.EditBugUrl, itemId);
                    break;
                case ItemOperation.Resole:
                    url = string.Format(appconfig.ResolveBugUrl, itemId);
                    break;
                case ItemOperation.Active:
                    url = string.Format(appconfig.ResolveBugUrl, itemId);
                    break;
                default:
                    break;
            }

            return url;
        }

        private static string GetBugOperationUrl(ItemOperation operation, ItemBase item)
        {
            return GetBugOperationUrl(operation, item.ID);
        }

        private static string GetTaskOperationUrl(ItemOperation operation, int itemId)
        {
            ApplicationConfig appconfig = IOHelper.LoadIsolatedData();

            string url = string.Empty;
            switch (operation)
            {
                case ItemOperation.View:
                    url = string.Format(appconfig.ViewTaskUrl, itemId);
                    break;
                case ItemOperation.Edit:
                    url = string.Format(appconfig.EditTaskUrl, itemId);
                    break;
                default:
                    break;
            }

            return url;
        }

        private static string GetTaskOperationUrl(ItemOperation operation, ItemBase item)
        {
            return GetTaskOperationUrl(operation, item.ID);
        }

        private static string GetStoryOperationUrl(ItemOperation operation, int itemId)
        {
            ApplicationConfig appconfig = IOHelper.LoadIsolatedData();

            string url = string.Empty;
            switch (operation)
            {
                case ItemOperation.View:
                    url = string.Format(appconfig.ViewStoryUrl, itemId);
                    break;
                case ItemOperation.Edit:
                    url = string.Format(appconfig.EditStoryUrl, itemId);
                    break;
                default:
                    break;
            }

            return url;
        }

        private static string GetStoryOperationUrl(ItemOperation operation, ItemBase item)
        {
            return GetStoryOperationUrl(operation, item.ID);
        }

        [DllImport("user32")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, SetWindowPosFlags uFlags);

        internal static void SetFormPos(Window wpfwin, IntPtr taskbar)
        {
            IntPtr handle = (new WindowInteropHelper(wpfwin)).Handle;

            SetWindowPos(handle, taskbar, 0, 0, 0, 0, SetWindowPosFlags.IgnoreMove | SetWindowPosFlags.IgnoreResize);
        }
    }


    enum SetWindowPosFlags : uint
    {
        /// <summary>If the calling thread and the thread that owns the window are attached to different input queues, 
        /// the system posts the request to the thread that owns the window. This prevents the calling thread from 
        /// blocking its execution while other threads process the request.</summary>
        /// <remarks>SWP_ASYNCWINDOWPOS</remarks>
        SynchronousWindowPosition = 0x4000,
        /// <summary>Prevents generation of the WM_SYNCPAINT message.</summary>
        /// <remarks>SWP_DEFERERASE</remarks>
        DeferErase = 0x2000,
        /// <summary>Draws a frame (defined in the window's class description) around the window.</summary>
        /// <remarks>SWP_DRAWFRAME</remarks>
        DrawFrame = 0x0020,
        /// <summary>Applies new frame styles set using the SetWindowLong function. Sends a WM_NCCALCSIZE message to 
        /// the window, even if the window's size is not being changed. If this flag is not specified, WM_NCCALCSIZE 
        /// is sent only when the window's size is being changed.</summary>
        /// <remarks>SWP_FRAMECHANGED</remarks>
        FrameChanged = 0x0020,
        /// <summary>Hides the window.</summary>
        /// <remarks>SWP_HIDEWINDOW</remarks>
        HideWindow = 0x0080,
        /// <summary>Does not activate the window. If this flag is not set, the window is activated and moved to the 
        /// top of either the topmost or non-topmost group (depending on the setting of the hWndInsertAfter 
        /// parameter).</summary>
        /// <remarks>SWP_NOACTIVATE</remarks>
        DoNotActivate = 0x0010,
        /// <summary>Discards the entire contents of the client area. If this flag is not specified, the valid 
        /// contents of the client area are saved and copied back into the client area after the window is sized or 
        /// repositioned.</summary>
        /// <remarks>SWP_NOCOPYBITS</remarks>
        DoNotCopyBits = 0x0100,
        /// <summary>Retains the current position (ignores X and Y parameters).</summary>
        /// <remarks>SWP_NOMOVE</remarks>
        IgnoreMove = 0x0002,
        /// <summary>Does not change the owner window's position in the Z order.</summary>
        /// <remarks>SWP_NOOWNERZORDER</remarks>
        DoNotChangeOwnerZOrder = 0x0200,
        /// <summary>Does not redraw changes. If this flag is set, no repainting of any kind occurs. This applies to 
        /// the client area, the nonclient area (including the title bar and scroll bars), and any part of the parent 
        /// window uncovered as a result of the window being moved. When this flag is set, the application must 
        /// explicitly invalidate or redraw any parts of the window and parent window that need redrawing.</summary>
        /// <remarks>SWP_NOREDRAW</remarks>
        DoNotRedraw = 0x0008,
        /// <summary>Same as the SWP_NOOWNERZORDER flag.</summary>
        /// <remarks>SWP_NOREPOSITION</remarks>
        DoNotReposition = 0x0200,
        /// <summary>Prevents the window from receiving the WM_WINDOWPOSCHANGING message.</summary>
        /// <remarks>SWP_NOSENDCHANGING</remarks>
        DoNotSendChangingEvent = 0x0400,
        /// <summary>Retains the current size (ignores the cx and cy parameters).</summary>
        /// <remarks>SWP_NOSIZE</remarks>
        IgnoreResize = 0x0001,
        /// <summary>Retains the current Z order (ignores the hWndInsertAfter parameter).</summary>
        /// <remarks>SWP_NOZORDER</remarks>
        IgnoreZOrder = 0x0004,
        /// <summary>Displays the window.</summary>
        /// <remarks>SWP_SHOWWINDOW</remarks>
        ShowWindow = 0x0040,
    }

    class FindWindow
    {
        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        //IMPORTANT : LPARAM  must be a pointer (InterPtr) in VS2005, otherwise an exception will be thrown
        private static extern bool EnumChildWindows(IntPtr window, EnumWindowProc callback, IntPtr i);
        //the callback function for the EnumChildWindows
        private delegate bool EnumWindowProc(IntPtr hWnd, IntPtr parameter);

        //if found  return the handle , otherwise return IntPtr.Zero
        [DllImport("user32.dll", EntryPoint = "FindWindowEx")]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        private string m_classname; // class name to look for
        private string m_caption; // caption name to look for

        private DateTime start;
        private int m_timeout;//If exceed the time. Indicate no windows found.

        private IntPtr m_hWnd; // HWND if found
        public IntPtr FoundHandle
        {
            get { return m_hWnd; }
        }

        private bool m_IsTimeOut;
        public bool IsTimeOut
        {
            get { return m_IsTimeOut; }
            set { m_IsTimeOut = value; }
        }

        // ctor does the work--just instantiate and go
        public FindWindow(IntPtr hwndParent, string classname, string caption, int timeout)
        {
            m_hWnd = IntPtr.Zero;
            m_classname = classname;
            m_caption = caption;
            m_timeout = timeout;
            start = DateTime.Now;
            FindChildClassHwnd(hwndParent, IntPtr.Zero);
        }

        /**/
        /// <summary>
        /// Find the child window, if found m_classname will be assigned 
        /// </summary>
        /// <param name="hwndParent">parent's handle</param>
        /// <param name="lParam">the application value, nonuse</param>
        /// <returns>found or not found</returns>
        //The C++ code is that  lParam is the instance of FindWindow class , if found assign the instance's m_hWnd
        private bool FindChildClassHwnd(IntPtr hwndParent, IntPtr lParam)
        {
            EnumWindowProc childProc = new EnumWindowProc(FindChildClassHwnd);
            IntPtr hwnd = FindWindowEx(hwndParent, IntPtr.Zero, m_classname, m_caption);
            if (hwnd != IntPtr.Zero)
            {
                this.m_hWnd = hwnd; // found: save it
                m_IsTimeOut = false;
                return false; // stop enumerating
            }

            DateTime end = DateTime.Now;

            if (start.AddSeconds(m_timeout) > end)
            {
                m_IsTimeOut = true;
                return false;
            }

            EnumChildWindows(hwndParent, childProc, IntPtr.Zero); // recurse  redo FindChildClassHwnd
            return true;// keep looking
        }
    }
    
}
