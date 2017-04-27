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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.Windows.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Net;
using System.IO;
using System.Windows.Media.Animation;
using System.Media;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using log4net;

namespace ZuggerWpf
{
    /// <summary>
    /// Window1.xaml 的交互逻辑
    /// </summary>
    public partial class Zugger : Window
    {
        DispatcherTimer timer = new DispatcherTimer();

        ApplicationConfig appconfig = null;        

        Action checkAction = null;

        DoubleAnimation animation = null;

        bool firstLoad = true;

        bool checkedUpdate = false;

        private static readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public Zugger()
        {
            InitializeComponent();

            log4net.Config.XmlConfigurator.Configure();
        }

        private List<ActionBase> ActionList = new List<ActionBase>();

        private int ShownNotifyFormCount = 0;

        private IntPtr TaskbarPtr = IntPtr.Zero;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //找到任务栏句柄
            FindWindow fw = new FindWindow(IntPtr.Zero, "Shell_TrayWnd", string.Empty, 100);
            TaskbarPtr = fw.FoundHandle;

            appconfig = IOHelper.LoadIsolatedData();

            SetSelectMode();
            RegisterAction();            

            checkAction = new Action(CheckPMS);

            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(hwnd).AddHook(new HwndSourceHook(WndProc));

            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 2);
            timer.Tick += new EventHandler(timer_Tick);
            timer.Start();
        }

        //限制窗口不能超过WorkAear
        IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case 0x46://WM_WINDOWPOSCHANGING
                    Rect screenRect = SystemParameters.WorkArea;

                    WINDOWPOS windowPos = (WINDOWPOS)Marshal.PtrToStructure(lParam, typeof(WINDOWPOS));

                    if (windowPos.x + windowPos.cx > screenRect.Right)
                    {
                        windowPos.x = (int)screenRect.Right - windowPos.cx;
                    }

                    if (windowPos.y + windowPos.cy > screenRect.Bottom)
                    {
                        windowPos.y = (int)screenRect.Bottom - windowPos.cy;
                    }

                    if (windowPos.x < screenRect.Top)
                    {
                        windowPos.x = (int)screenRect.Top;
                    }

                    if (windowPos.y < screenRect.Left)
                    {
                        windowPos.y = (int)screenRect.Left;
                    }

                    Marshal.StructureToPtr(windowPos, lParam, false);
                    break;
                default:
                    break;
            }

            return IntPtr.Zero;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct WINDOWPOS
        {
            internal IntPtr hWnd;
            internal IntPtr hWndInsertAfter;
            internal int x;
            internal int y;
            internal int cx;
            internal int cy;
            internal int flags;
        }

        //延迟初始化到需要的时候
        private DoubleAnimation CreateSingleAnimation()
        {
            if (animation == null)
            {
                animation = new DoubleAnimation();
                animation.From = 1;
                animation.To = 0;
                animation.AutoReverse = true;
                animation.RepeatBehavior = new RepeatBehavior(3);
                animation.Duration = new Duration(TimeSpan.FromMilliseconds(1000));
            }

            return animation;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (timer != null)
            {
                timer.Stop();
            }

            Properties.Settings.Default.Save();
        }

        private void frmzugger_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, checkAction);
            }
        }

        private void SetSelectMode()
        {
            //TODO:再优雅点
            lbBugCount.Visibility = appconfig.ShowBug ? Visibility.Visible : Visibility.Collapsed;
            lbTaskCount.Visibility = appconfig.ShowTask ? Visibility.Visible : Visibility.Collapsed;
            lbStoryCount.Visibility = appconfig.ShowStory ? Visibility.Visible : Visibility.Collapsed;
            lbOpenedByMeBugCount.Visibility = appconfig.ShowOpendByMe ? Visibility.Visible : Visibility.Collapsed;

            //对于之前的版本因为已经存在配置了，所以默认的三个bool值都为false
            if (!appconfig.ShowOpendByMe
                && !appconfig.ShowBug
                && !appconfig.ShowTask
                && !appconfig.ShowStory)
            {
                lbBugCount.Visibility = Visibility.Visible;
                lbTaskCount.Visibility = Visibility.Visible;

                appconfig.ShowBug = true;
                appconfig.ShowTask = true;
            }
        }

        /// <summary>
        /// 注册向PMS取数据的所有action到队列中
        /// </summary>
        private void RegisterAction()
        {
            ActionList.Add(new Login());

            #region getbug
            if (appconfig.ShowBug)
            {
                ZuggerObservableCollection<BugItem> bugCollection = new ZuggerObservableCollection<BugItem>();
                GetBug gb = new GetBug(bugCollection);
                lbBugCount.DataContext = bugCollection;
                gb.OnNewItemArrive += new NewItemArrive(NewItem_OnNewItemArrive);
                ActionList.Add(gb);
            }
            #endregion

            #region gettask
            if (appconfig.ShowTask)
            {
                ZuggerObservableCollection<TaskItem> taskCollection = new ZuggerObservableCollection<TaskItem>();
                GetTask gt = new GetTask(taskCollection);
                lbTaskCount.DataContext = taskCollection;
                gt.OnNewItemArrive += new NewItemArrive(NewItem_OnNewItemArrive);
                ActionList.Add(gt);
            }
            #endregion

            #region 由我创建的bug
            if (appconfig.ShowOpendByMe)
            {
                ZuggerObservableCollection<BugItem> openedByMeCollection = new ZuggerObservableCollection<BugItem>();
                GetOpenedByMeBug gobm = new GetOpenedByMeBug(openedByMeCollection);
                lbOpenedByMeBugCount.DataContext = openedByMeCollection;
                ActionList.Add(gobm);
            }
            #endregion

            #region 需求
            if (appconfig.ShowStory)
            {
                ZuggerObservableCollection<StoryItem> storyCollection = new ZuggerObservableCollection<StoryItem>();
                GetStory gs = new GetStory(storyCollection);
                lbStoryCount.DataContext = storyCollection;
                gs.OnNewItemArrive += new NewItemArrive(NewItem_OnNewItemArrive);
                ActionList.Add(gs);
            }
            #endregion
        }

        /// <summary>
        /// 调用actoinlist中的action，向PMS取数据
        /// </summary>
        private void CheckPMS()
        {
            #region DEBUG
            logger.Info(string.Format("{0}前 {1}", "CheckPMS()调整刷新间隔前", Environment.WorkingSet));
            #endregion

            timer.Interval = new TimeSpan(0, appconfig.RequestInterval, 0);

            #region DEBUG
            logger.Info(string.Format("{0}前 {1}", "CheckPMS()调整刷新间隔后", Environment.WorkingSet));            
            #endregion

            foreach (ActionBase ab in ActionList)
            {
                #region DEBUG
                logger.Info(string.Format("{0}前 {1}", ab.GetType().Name, Environment.WorkingSet));
                #endregion

                ab.Action();

                #region DEBUG
                logger.Info(string.Format("{0}后 {1}", ab.GetType().Name, Environment.WorkingSet));
                #endregion
            }

            CheckZuggerUpdate();

            firstLoad = false;
        }

        private void CheckZuggerUpdate()
        {
            if (!checkedUpdate)
            {
                string latestVersion;
                bool isUpdate = Util.GetLatestVersionNumber(out latestVersion);

                if (isUpdate)
                {
                    string showText = string.Format("检测到新版本{0}", latestVersion);
                    NewArriveNotify notifyfrm = new NewArriveNotify(showText);
                    notifyfrm.Closed += new EventHandler(notifyfrm_Closed);
                    notifyfrm.HyperLinkClick +=new RoutedEventHandler(notifyfrm_ZuggerUpdateHyperLinkClick);
                    Interlocked.Increment(ref ShownNotifyFormCount);
                    notifyfrm.Show();
                }

                checkedUpdate = true;
            }
        }

        void NewItem_OnNewItemArrive(ItemType type, int newItemsCount)
        {
            //防止程序第一次启动就产生通知
            if (!firstLoad)
            {
                //提示音
                using (SoundPlayer player = new SoundPlayer())
                {
                    string soundfile = System.IO.Path.Combine(Environment.CurrentDirectory, "Resources\\Notify.wav");
                    if (File.Exists(soundfile))
                    {
                        player.SoundLocation = soundfile;
                        player.Play();
                    }
                }

                animation = CreateSingleAnimation();

                //闪烁
                if (type == ItemType.Bug)
                {
                    lbBugCount.BeginAnimation(Label.OpacityProperty, animation);
                }
                else if (type == ItemType.Task)
                {
                    lbTaskCount.BeginAnimation(Label.OpacityProperty, animation);
                }
                else if (type == ItemType.Story)
                {
                    lbStoryCount.BeginAnimation(Label.OpacityProperty, animation);
                }

                //任务栏通知
                if (appconfig.IsTaskNotify)
                {
                    NewArriveNotify notifyfrm = new NewArriveNotify(type, newItemsCount, TaskbarPtr);
                    notifyfrm.Closed += new EventHandler(notifyfrm_Closed);
                    notifyfrm.HyperLinkClick += new RoutedEventHandler(notifyfrm_HyperLinkClick);

                    notifyfrm.ShownNotifyFormCount = ShownNotifyFormCount;
                    Interlocked.Increment(ref ShownNotifyFormCount);
                    notifyfrm.Show();
                }
            }
        }

        void notifyfrm_ZuggerUpdateHyperLinkClick(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(ConstValue.ZuggerProjectAddress);
        }

        void notifyfrm_HyperLinkClick(object sender, RoutedEventArgs e)
        {
            if (e.Source != null)
            {
                ItemType type = (ItemType)e.Source;

                if (type == ItemType.Bug)
                {
                    if (bugList.Visibility != Visibility.Visible)
                    {
                        DoubleClickFloatFrame(lbBugCount, bugList);
                    }
                    
                    bugList.ScrollIntoView(bugList.Items[bugList.Items.Count - 1]);
                }
                else if (type == ItemType.Task)
                {
                    if (taskList.Visibility != Visibility.Visible)
                    {
                        DoubleClickFloatFrame(lbTaskCount, taskList);
                    }

                    taskList.ScrollIntoView(taskList.Items[taskList.Items.Count - 1]);
                }
                else if (type == ItemType.Story)
                {
                    if (storyList.Visibility != Visibility.Visible)
                    {
                        DoubleClickFloatFrame(lbStoryCount, storyList);
                    }

                    storyList.ScrollIntoView(storyList.Items[storyList.Items.Count - 1]);
                }
                
            }
        }

        void notifyfrm_Closed(object sender, EventArgs e)
        {
            Interlocked.Decrement(ref ShownNotifyFormCount);
        }

        void timer_Tick(object sender, EventArgs e)
        {
            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, checkAction);
        }

        #region 点击悬浮框
        /// <summary>
        /// 展开opened by me bug list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lbOpenedByMeBugCount_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DoubleClickFloatFrame(lbOpenedByMeBugCount, openedBymeList);
        }

        /// <summary>
        /// 展开bug list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lbBugCount_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DoubleClickFloatFrame(lbBugCount, bugList);
        }

        /// <summary>
        /// 展开task list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lbTaskCount_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DoubleClickFloatFrame(lbTaskCount, taskList);
        }

        /// <summary>
        /// 展开stroy list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lbStoryCount_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DoubleClickFloatFrame(lbStoryCount, storyList);
        }

        private void SetDataContent(FrameworkElement showUI)
        {
            foreach (FrameworkElement c in listview.Children)
            {
                if (c != showUI)
                {
                    c.DataContext = null;
                }
            }
        }

        /// <summary>
        /// 打开关闭listbox
        /// </summary>
        /// <param name="label"></param>
        /// <param name="listbox"></param>
        private void DoubleClickFloatFrame(FrameworkElement label, FrameworkElement listbox)
        {
            if (label.DataContext != listbox.DataContext)
            {
                listbox.DataContext = label.DataContext;

                SetDataContent(listbox);
            }
            else
            {
                listbox.DataContext = null;
            }
        }

        #endregion

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {   
            this.DragMove();
        }

        private void MenuItemOption_Click(object sender, RoutedEventArgs e)
        {
            Option o = new Option(this);
            o.Owner = this;
            o.ShowDialog();
        }

        private void MenuItemClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        //双击item，打开对应的网页
        private void Label_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            string url = string.Empty;

            Label label = sender as Label;

            if (label != null && label.DataContext != null)
            {
                ItemBase ib = label.DataContext as ItemBase;

                Util.OpenItem(ib, ItemOperation.View);
            }
        }

        /// <summary>
        /// 在listbox选择中的item上enter也打开网页
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.F2)//F2查看
            {
                ListBox lb = sender as ListBox;
                if (lb != null && lb.DataContext != null && lb.SelectedItem != null)
                {
                    ItemBase ib = lb.SelectedItem as ItemBase;

                    Util.OpenItem(ib, ItemOperation.View);
                }
            }
            else if (e.Key == Key.F3)//F3编辑
            {
                ListBox lb = sender as ListBox;
                if (lb != null && lb.DataContext != null && lb.SelectedItem != null)
                {
                    ItemBase ib = lb.SelectedItem as ItemBase;

                    Util.OpenItem(ib, ItemOperation.Edit);
                }
            }
            else if (e.Key == Key.F4)//F4解决bug
            {
                ListBox lb = sender as ListBox;
                if (lb != null && lb.DataContext != null && lb.SelectedItem != null)
                {
                    ItemBase ib = lb.SelectedItem as ItemBase;

                    Util.OpenItem(ib, ItemOperation.Resole);
                }
            }
            else if (e.Key == Key.C && (Keyboard.Modifiers & (ModifierKeys.Control)) == (ModifierKeys.Control))
            {
                ListBox lb = sender as ListBox;
                if (lb != null && lb.DataContext != null && lb.SelectedItem != null)
                {
                    ItemBase ib = lb.SelectedItem as ItemBase;

                    CopyBaseItem(ib);
                }
            }
        }

        private void CopyBaseItem(ItemBase ib)
        {
            if (ib != null)
            {
                Clipboard.SetDataObject(string.Format("{0}ID:{1}, Title:{2}", ib.Tip, ib.ID, ib.Title));
            }
        }

        private void Label_MouseUp(object sender, MouseButtonEventArgs e)
        {
            //鼠标中键点击，打开searh窗口
            if (e.ChangedButton == MouseButton.Middle && e.MiddleButton == MouseButtonState.Released)
            {
                ItemBase ib = null;
                Label lb = sender as Label;
                switch (lb.Name)
                {
                    case "lbBugCount":
                        ib = new BugItem();
                        break;
                    case "lbTaskCount":
                        ib = new TaskItem();
                        break;
                    case "lbStoryCount":
                        ib = new StoryItem();
                        break;
                    default:
                        break;
                }

                SearchItem si = new SearchItem(ib);
                si.Top = this.Top;
                si.Left = this.Left + this.ActualWidth;
                si.ShowDialog();
            }
        }

        private void ResolveBug_Click(object sender, RoutedEventArgs e)
        {
            if (bugList.DataContext != null && bugList.SelectedItem != null)
            {
                ItemBase ib = bugList.SelectedItem as ItemBase;

                Util.OpenItem(ib, ItemOperation.Resole);
            }
        }

        private void ItemEdit_Click(object sender, RoutedEventArgs e)
        {
            object item = null;

            if (openedBymeList.DataContext != null && openedBymeList.SelectedItem != null)
            {
                item = openedBymeList.SelectedItem;
            }
            else if (bugList.DataContext != null && bugList.SelectedItem != null)
            {
                item = bugList.SelectedItem;
            }
            else if (taskList.DataContext != null && taskList.SelectedItem != null)
            {
                item = taskList.SelectedItem;
            }
            else if (storyList.DataContext != null && storyList.SelectedItem != null)
            {
                item = storyList.SelectedItem;
            }

            ItemBase ib = item as ItemBase;
            
            Util.OpenItem(ib, ItemOperation.Edit);
        }       

        private void CopyItem_Click(object sender, RoutedEventArgs e)
        {
            object item = null;

            if (openedBymeList.SelectedItem != null)
            {
                item = openedBymeList.SelectedItem;
            }
            if (bugList.SelectedItem != null)
            {
                item = bugList.SelectedItem;
            }
            else if (taskList.SelectedItem != null)
            {
                item = taskList.SelectedItem;
            }
            else if (storyList.SelectedItem != null)
            {
                item = storyList.SelectedItem;
            }

            ItemBase ib = item as ItemBase;

            CopyBaseItem(ib);
        }

        private void MenuItemAbout_Click(object sender, RoutedEventArgs e)
        {
            About about = new About(this);
            about.Owner = this;
            about.ShowDialog();
        }
    }
}
