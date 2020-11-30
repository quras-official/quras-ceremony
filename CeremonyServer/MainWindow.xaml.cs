using Ceremony.IO.MySQL;
using CeremonyServer.Network;
using System;
using System.Windows;
using System.Windows.Threading;

namespace CeremonyServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private RpcServer server;

        DispatcherTimer dispatcherTimer = new DispatcherTimer();

        private const int PARTICIPANT_INTERVAL = 90 * 60;   // 90 mins

        public int nTotalSegments = 2179311;

        public MainWindow()
        {
            LogUtil.Default.Log("Ceremony Server Started...");

            InitializeComponent();
            StartRPCService();

            //check ceremony status and if the ceremony is started, start timeinterval.
            using (CeremonySQL ceremonySQL = new CeremonySQL())
            {
                ceremonySQL.GetLastParticipantTime();
                int selectedUserIndex = ceremonySQL.reconnect().GetSelectedUserIndex();
                int userIndex = ceremonySQL.reconnect().GetUserIndex();
                var lastParticipantTime = ceremonySQL.reconnect().GetLastParticipantTime();

                if (ceremonySQL.reconnect().GetCeremonyStatus() == CeremonyStatus.CEREMONY_STATUS_STARTED)
                {
                    if (selectedUserIndex != userIndex)
                    {
                        DateTime now = DateTime.Now;
                        if (DateTimeUtil.Default.GetTimeStamp(DateTime.Now) - lastParticipantTime > PARTICIPANT_INTERVAL)
                        {
                            Settings.Default.SendEmailToSelectedUser((int)MailType.MAIL_TYPE_TIMEOUT);
                            ceremonySQL.reconnect().TimeOutUser();
                            StartTimeInterval();
                        }
                        else
                        {
                            StartTimeInterval(PARTICIPANT_INTERVAL - (int)(DateTimeUtil.Default.GetTimeStamp(DateTime.Now) - lastParticipantTime));
                        }
                    }
                }
            }
            
            RefreshView();
        }

        public void StartRPCService()
        {
            server = new RpcServer(this);
            string[] uriPrefix = new string[1];
            uriPrefix[0] = "http://localhost:10033";
            server.Start(uriPrefix, "", "");
        }

        

        public void RefreshView()
        {
            using (CeremonySQL ceremonySQL = new CeremonySQL())
            {
                TxtParticipantCnt.Text = "Total Participants : " + ceremonySQL.GetUserIndex();
                TxtSelectedUserIndex.Text = "Index : " + ceremonySQL.reconnect().GetSelectedUserIndex();

                User user = ceremonySQL.reconnect().GetSelectedUser();

                TxtSelectedUserName.Text = "Name : " + (user == null ? "" : user.Name);
                TxtSelectedUserEmail.Text = "Email : " + (user == null ? "" : user.Email);
                TxtSelectedUserAddress.Text = "Address : " + (user == null ? "" : user.Address);
                TxtSelectedUserCountry.Text = "Country : " + (user == null ? "" : user.Country);

                TxtSuccessedCnt.Text = "Successed Participants : " + ceremonySQL.reconnect().GetParticipantCntByStatus(UserStatus.USER_STATUS_SUCCESSED);
                TxtTimeoutCnt.Text = "Timeout Participants : " + ceremonySQL.reconnect().GetParticipantCntByStatus(UserStatus.USER_STATUS_TIMEOUT);

                string strStatus = "";
                switch (ceremonySQL.reconnect().GetCeremonyStatus())
                {
                    case CeremonyStatus.CEREMONY_STATUS_WAITING:
                        strStatus = "WAITING";
                        break;
                    case CeremonyStatus.CEREMONY_STATUS_STARTED:
                        strStatus = "STARTED";
                        break;
                    case CeremonyStatus.CEREMONY_STATUS_COMPLETED:
                        strStatus = "COMPLETED";
                        break;
                }

                TxtCeremonyStatus.Text = "Ceremony Status : " + strStatus;
                if (ceremonySQL.reconnect().GetCeremonyStatus() == CeremonyStatus.CEREMONY_STATUS_WAITING)
                {
                    BtnStart.IsEnabled = true;
                }
                else
                {
                    BtnStart.IsEnabled = false;
                }
            }
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (!Settings.Default.StartCeremony())
            {
                return;
            }
            
            RefreshView();

            StartTimeInterval();
        }

        public void StartTimeInterval(int interval = PARTICIPANT_INTERVAL)
        {
            LogUtil.Default.Log("Interval: " + interval);
            StopTimeInterval();

            Application.Current.Dispatcher.Invoke(delegate {
                RefreshView();
            });

            using (CeremonySQL ceremonySQL = new CeremonySQL())
            {
                if (ceremonySQL.GetCeremonyStatus() != CeremonyStatus.CEREMONY_STATUS_STARTED)
                {
                    return;
                }

                if (ceremonySQL.reconnect().GetUserIndex() == ceremonySQL.reconnect().GetSelectedUserIndex())
                {
                    return;
                }
            }

            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(interval / 3600, (interval % 3600) / 60, interval % 60);
            dispatcherTimer.Start();

            Settings.Default.SendEmailToSelectedUser();
        }

        private void dispatcherTimer_Tick(Object sender, EventArgs e)
        {
            Settings.Default.SendEmailToSelectedUser((int)MailType.MAIL_TYPE_TIMEOUT);
            using (CeremonySQL cermeonySQL = new CeremonySQL())
            {
                cermeonySQL.TimeOutUser();
            }
            StartTimeInterval();
        }

        public void StopTimeInterval()
        {
            if (dispatcherTimer != null)
            {
                dispatcherTimer.Stop();
            }
            dispatcherTimer = null;
        }
    }
}
