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
using System.Windows.Threading;
using System.Windows.Media.Animation;

namespace ZuggerWpf
{
    /// <summary>
    /// Interaction logic for NewArriveNotify.xaml
    /// </summary>
    public partial class NewArriveNotify : Window
    {
        private enum DisplayStates
        {
            Opening,
            Opened,
            Hiding,
            Hidden
        }

        private DispatcherTimer stayOpenTimer = null;
        private Storyboard storyboard;
        private DoubleAnimation animation;

        private double hiddenTop;
        private double openedTop;
        private EventHandler arrivedHidden;
        private EventHandler arrivedOpened;

        /// <summary>
        /// 点击了hyperlink，如果只有一个item，则直接用网页打开，如果有多个item，在zugger中打开
        /// </summary>
        internal event RoutedEventHandler HyperLinkClick;

        private int openingMilliseconds = 1000;

        int NewItemsCount;
        ItemType NewItemsType;

        string showText; 

        IntPtr TaskbarPtr = IntPtr.Zero;

        internal NewArriveNotify(ItemType type, int newItemsCount, IntPtr taskbarPtr)
        {           
            InitializeComponent();

            NewItemsCount = newItemsCount;
            NewItemsType = type;
            TaskbarPtr = taskbarPtr;
        }

        internal NewArriveNotify(string showText)
        {
            InitializeComponent();

            this.showText = showText;
        }

        private DisplayStates displayState;
        /// <summary>
        /// The current DisplayState
        /// </summary>
        private DisplayStates DisplayState
        {
            get
            {
                return this.displayState;
            }
            set
            {
                if (value != this.displayState)
                {
                    this.displayState = value;
                    OnDisplayStateChanged();
                }                
            }
        }

        /// <summary>
        /// 若出现多个通知，每个通知的高度起始位置不同
        /// </summary>
        public int ShownNotifyFormCount
        {
            get;
            set;
        }


        private void Close_Up(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Set initial settings based on the current screen working area.
            this.SetInitialLocations(false);

            // Start the window in the Hidden state.
            this.DisplayState = DisplayStates.Hidden;

            // Prepare the timer for how long the window should stay open.
            this.stayOpenTimer = new DispatcherTimer();
            this.stayOpenTimer.Interval = TimeSpan.FromMilliseconds(2000);
            this.stayOpenTimer.Tick += new EventHandler(this.stayOpenTimer_Elapsed);

            // Prepare the animation to change the Top property.
            this.animation = new DoubleAnimation();
            Storyboard.SetTargetProperty(this.animation, new PropertyPath(Window.TopProperty));
            this.storyboard = new Storyboard();
            this.storyboard.Children.Add(this.animation);
            this.storyboard.FillBehavior = FillBehavior.Stop;

            // Create the event handlers for when the animation finishes.
            this.arrivedHidden = new EventHandler(this.Storyboard_ArrivedHidden);
            this.arrivedOpened = new EventHandler(this.Storyboard_ArrivedOpened);
            
            if (NewItemsCount != 0)
            {
                int count = NewItemsCount > 0 ? 1 : -NewItemsCount;

                showText = string.Format("收到{0}个{1}", count, NewItemsType);
            }

            txtTip.Text = showText;

            //set this form behind the taskbar layer
            Util.SetFormPos(this, TaskbarPtr);

            Notify();
        }

        private void SetInitialLocations(bool showOpened)
        {
            // Initialize the window location to the bottom right corner.
            this.Left = SystemParameters.WorkArea.Right - this.ActualWidth - 5;
            
            // Set the opened and hidden locations.
            this.hiddenTop = SystemParameters.WorkArea.Bottom - ShownNotifyFormCount * this.ActualHeight;
            this.openedTop = hiddenTop - this.ActualHeight;//workingArea.Bottom - this.ActualHeight - ShownNotifyFormCount * this.ActualHeight;

            // Set Top based on whether opened or hidden is desired
            if (showOpened)
            {
                this.Top = openedTop;
            }
            else
            {
                this.Top = hiddenTop;
            }
        }

        protected virtual void Storyboard_ArrivedHidden(object sender, EventArgs e)
        {
            // Setting the display state will result in any needed actions.
            this.DisplayState = DisplayStates.Hidden;

            this.Close();
        }

        protected virtual void Storyboard_ArrivedOpened(object sender, EventArgs e)
        {
            // Setting the display state will result in any needed actions.
            this.DisplayState = DisplayStates.Opened;
        }

        private void stayOpenTimer_Elapsed(Object sender, EventArgs args)
        {
            // Stop the timer because this should not be an ongoing event.
            this.stayOpenTimer.Stop();

            if (!this.IsMouseOver)
            {
                // Only start closing the window if the mouse is not over it.
                this.DisplayState = DisplayStates.Hiding;
            }
        }

        protected override void OnMouseEnter(System.Windows.Input.MouseEventArgs e)
        {
            if (this.DisplayState == DisplayStates.Opened)
            {
                // When the user mouses over and the window is already open, it should stay open.
                // Stop the timer that would have otherwise hidden it.
                this.stayOpenTimer.Stop();
            }
            else if ((this.DisplayState == DisplayStates.Hidden) ||
                     (this.DisplayState == DisplayStates.Hiding))
            {
                // When the user mouses over and the window is hidden or hiding, it should open. 
                this.DisplayState = DisplayStates.Opening;
            }

            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(System.Windows.Input.MouseEventArgs e)
        {
            if (this.DisplayState == DisplayStates.Opened)
            {
                // When the user moves the mouse out of the window, the timer to hide the window
                // should be started.
                this.stayOpenTimer.Stop();
                this.stayOpenTimer.Start();
            }

            base.OnMouseLeave(e);
        }

        public void Notify()
        {
            if (this.DisplayState == DisplayStates.Opened)
            {
                // The window is already open, and should now remain open for another count.
                this.stayOpenTimer.Stop();
                this.stayOpenTimer.Start();
            }
            else
            {
                this.DisplayState = DisplayStates.Opening;
            }
        }

        private void OnDisplayStateChanged()
        {
            //Util.SetFormPos(this, TaskbarPtr);
            // The display state has changed.

            // Unless the stortboard as already been created, nothing can be done yet.
            if (this.storyboard == null)
                return;

            // Stop the current animation.
            this.storyboard.Stop(this);

            // Since the storyboard is reused for opening and closing, both possible
            // completed event handlers need to be removed.  It is not a problem if
            // either of them was not previously set.
            this.storyboard.Completed -= arrivedHidden;
            this.storyboard.Completed -= arrivedOpened;

            if (this.displayState == DisplayStates.Opened)
            {
                // The window has just arrived at the opened state.

                // Because the inital settings of this TaskNotifier depend on the screen's working area,
                // it is best to reset these occasionally in case the screen size has been adjusted.
                this.SetInitialLocations(true);

                if (!this.IsMouseOver)
                {
                    // The mouse is not within the window, so start the countdown to hide it.
                    this.stayOpenTimer.Stop();
                    this.stayOpenTimer.Start();
                }
            }
            else if (this.displayState == DisplayStates.Opening)
            {
                // The window should start opening.

                // Make the window visible.
                this.Visibility = Visibility.Visible;
                //this.BringToTop();

                // Because the window may already be partially open, the rate at which
                // it opens may be a fraction of the normal rate.
                // This must be calculated.
                int milliseconds = this.CalculateMillseconds(this.openingMilliseconds, this.openedTop);

                if (milliseconds < 1)
                {
                    // This window must already be open.
                    this.DisplayState = DisplayStates.Opened;
                    return;
                }

                // Reconfigure the animation.
                this.animation.To = this.openedTop;
                this.animation.Duration = new Duration(new TimeSpan(0, 0, 0, 0, milliseconds));

                // Set the specific completed event handler.
                this.storyboard.Completed += arrivedOpened;

                // Start the animation.
                this.storyboard.Begin(this, true);
            }
            else if (this.displayState == DisplayStates.Hiding)
            {
                // The window should start hiding.

                // Because the window may already be partially hidden, the rate at which
                // it hides may be a fraction of the normal rate.
                // This must be calculated.
                int milliseconds = this.CalculateMillseconds(1000, this.hiddenTop);

                if (milliseconds < 1)
                {
                    // This window must already be hidden.
                    this.DisplayState = DisplayStates.Hidden;
                    return;
                }

                // Reconfigure the animation.
                this.animation.To = this.hiddenTop;
                this.animation.Duration = new Duration(new TimeSpan(0, 0, 0, 0, milliseconds));

                // Set the specific completed event handler.
                this.storyboard.Completed += arrivedHidden;

                // Start the animation.
                this.storyboard.Begin(this, true);
            }
            else if (this.displayState == DisplayStates.Hidden)
            {
                // Ensure the window is in the hidden position.
                SetInitialLocations(false);

                // Hide the window.
                this.Visibility = Visibility.Hidden;
            }
        }

        private int CalculateMillseconds(int totalMillsecondsNormally, double destination)
        {
            if (this.Top == destination)
            {
                // The window is already at its destination.  Nothing to do.
                return 0;
            }

            double distanceRemaining = Math.Abs(this.Top - destination);
            double percentDone = distanceRemaining / this.ActualHeight;

            // Determine the percentage of normal milliseconds that are actually required.
            return (int)(totalMillsecondsNormally * percentDone);
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            if (NewItemsCount > 0)
            {
                Util.OpenItem(NewItemsCount, NewItemsType, ItemOperation.View);
            }
            else
            {
                if (HyperLinkClick != null)
                {
                    e.Source = NewItemsType;
                    HyperLinkClick(this, e);
                }
            }
        }
    }
}
