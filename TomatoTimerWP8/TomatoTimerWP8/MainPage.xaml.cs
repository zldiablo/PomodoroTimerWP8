using Microsoft.Devices;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Scheduler;
using System;
using System.Windows;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace TomatoTimerWP8
{
    public partial class MainPage : PhoneApplicationPage
    {
        #region const properties
#if DEBUG
        private static readonly int WorkTime = 1;
        //private static readonly int RestTime = 5;
        private static int TomatoCycle = 2;
#else
        private static readonly int WorkTime = 25;
        //private static readonly int RestTime = 5;
        private static int TomatoCycle = 4;
#endif
        private static readonly string ReminderName = "TomatoTimeReminder";
        private DispatcherTimer dpt;
        private DateTime lastStarted;
        private bool timerStarted;
        
        #endregion

        #region Message Strings
        public static readonly string NotificationWorkTitle = "番茄时间提醒";
        public static readonly string NotificationWorkMessage = "番茄时间到了, 点击这里返回番茄时间应用";
        public static readonly string InitialMessage = "番茄工作法（番茄时间）是一种时间管理方法。每25分钟被定义成一个“番茄时间”，在这段时间内要不受打扰的完成一项任务。完成之后进行短暂的休息，再开始下一个番茄时间。\n番茄工作法包含4个简单步骤\n1.选择一个任务\n2.打开计时器，并不可打扰的工作25分钟\n3.休息5分钟\n4.买进行4个“番茄时间”休息15分钟\n点击“关于”可以进一步查看详细信息。";
        public static readonly string WorkTimeMessage = "您已经进行了{0}个番茄时间，现在可以休息5分钟，然后点击“开始计时”进行下一个";
        public static readonly string FinalWorkTimeMessage = "您已经进行了{0}个番茄时间，要多休息一段时间了。之后可以点击“开始计时”重新开始";
        public static readonly string SetNotificationMessage = "将于番茄时间结束时弹出提醒。您可以现在退出本程序，也可以留在本程序查看剩余时间。";
        #endregion

        private int currentCount;

        // 构造函数
        public MainPage()
        {
            InitializeComponent();
            dpt = new DispatcherTimer();
            dpt.Interval = new TimeSpan(0, 0, 1);
            dpt.Tick += UpdateTime;
            timerStarted = false;
            // 用于本地化 ApplicationBar 的示例代码
            //BuildLocalizedApplicationBar();
        }

        //protected override void OnBackKeyPress(CancelEventArgs e)
        //{
        //    if (MessageBox.Show("您确定要退出程序？", "提醒", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
        //    {
        //        e.Cancel = true;//操作取消
        //    }
        //    base.OnBackKeyPress(e);
        //}

        /// <summary>
        /// Timmer Function
        /// </summary>
        /// <param name="s"></param>
        /// <param name="args"></param>
        private void UpdateTime(object s, EventArgs args)
        {
            DateTime target = lastStarted.AddMinutes(WorkTime);
            DateTime now = DateTime.Now;
            if (target > now)
            {
                TimerMessage.Text = (target - now).ToString("mm\\:ss");
            }
            else
            {
                dpt.Stop();
                TimerMessage.Text = string.Empty;
                currentCount++;
                timerStarted = false;
                InitializeMessage();
                VibrateController vc = VibrateController.Default;
                vc.Start(TimeSpan.FromMilliseconds(500));
            }
            RefreshMessage();
        }

        private void RefreshMessage()
        {
            if (timerStarted)
            {
                ReminderMessage.Visibility = System.Windows.Visibility.Collapsed;
                TimerMessage.Visibility = System.Windows.Visibility.Visible;
                this.PageTitle.Text = "工作中";
                ReminderButton.Content = "重新开始计时";
            }
            else
            {
                ReminderMessage.Visibility = System.Windows.Visibility.Visible;
                TimerMessage.Visibility = System.Windows.Visibility.Collapsed;
                this.PageTitle.Text = "准备开始";
                ReminderButton.Content = "开始计时";
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            currentCount = 0;
            if (e.NavigationMode == NavigationMode.New && NavigationContext != null && NavigationContext.QueryString.ContainsKey("index"))
            {
                int res;
                if (int.TryParse(NavigationContext.QueryString["index"], out res))
                {
                    currentCount = res;
                }
                currentCount++;
                timerStarted = false;
                if (dpt.IsEnabled)
                {
                    dpt.Stop();
                }
            }
            InitializeMessage();
            RefreshMessage();
        }

        private void InitializeMessage()
        {
            if (currentCount == 0)
            {
                ReminderMessage.Text = InitialMessage;
                TimerMessage.Visibility = System.Windows.Visibility.Collapsed;
            }
            else if (currentCount % TomatoCycle == 0)
            {
                ReminderMessage.Text = string.Format(FinalWorkTimeMessage, currentCount);
            }
            else
            {
                ReminderMessage.Text = string.Format(WorkTimeMessage, currentCount);
            }
        }

        private void ReminderButton_Click(object sender, RoutedEventArgs e)
        {
            Reminder reminder = new Reminder(ReminderName);
            reminder.Title = NotificationWorkTitle;
            reminder.Content = NotificationWorkMessage;
            DateTime now = DateTime.Now;
            this.lastStarted = now;
            reminder.BeginTime = now.AddMinutes(WorkTime);
            reminder.ExpirationTime = now.AddMinutes(WorkTime);
            reminder.RecurrenceType = RecurrenceInterval.None;
            reminder.NavigationUri = new Uri("/MainPage.xaml?index=" + currentCount % TomatoCycle, UriKind.Relative);

            // Register the reminder with the system.
            if (ScheduledActionService.Find(ReminderName) != null)
            {
                ScheduledActionService.Remove(ReminderName);
            }
            ScheduledActionService.Add(reminder);

            dpt.Start();
            this.timerStarted = true;
            MessageBox.Show(SetNotificationMessage, "提醒设置成功", MessageBoxButton.OK);
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/AboutPage.xaml", UriKind.Relative));
        }

        // 用于生成本地化 ApplicationBar 的示例代码
        //private void BuildLocalizedApplicationBar()
        //{
        //    // 将页面的 ApplicationBar 设置为 ApplicationBar 的新实例。
        //    ApplicationBar = new ApplicationBar();

        //    // 创建新按钮并将文本值设置为 AppResources 中的本地化字符串。
        //    ApplicationBarIconButton appBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.add.rest.png", UriKind.Relative));
        //    appBarButton.Text = AppResources.AppBarButtonText;
        //    ApplicationBar.Buttons.Add(appBarButton);

        //    // 使用 AppResources 中的本地化字符串创建新菜单项。
        //    ApplicationBarMenuItem appBarMenuItem = new ApplicationBarMenuItem(AppResources.AppBarMenuItemText);
        //    ApplicationBar.MenuItems.Add(appBarMenuItem);
        //}
    }
}