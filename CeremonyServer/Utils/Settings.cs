using Ceremony.IO.MySQL;
using System;
using System.Net;
using System.Net.Mail;

namespace CeremonyServer
{
    internal sealed partial class Settings
    {
        public static Settings instance;

        public static Settings Default
        {
            get
            {
                if (instance == null)
                    instance = new Settings();

                return instance;
            }
        }

        public Settings()
        {
        }

        public bool SendEmailToSelectedUser(int mailType = (int)MailType.MAIL_TYPE_START)
        {
            try
            {
                LogUtil.Default.Log("Sending email to selected user...");

                using (CeremonySQL ceremonySQL = new CeremonySQL())
                {
                    User user = ceremonySQL.GetSelectedUser();

                    if (user == null)
                    {
                        throw new Exception("No User Selected.");
                    }

                    SmtpClient smtpClient = new SmtpClient("quras.io");
                    smtpClient.Port = 587;
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.Credentials = new NetworkCredential("email", "password");
                    smtpClient.EnableSsl = true;

                    String body = "";
                    // Set email body

                    if (mailType == (int)MailType.MAIL_TYPE_START)
                    {
                        body = "Hi " + user.Name + "!\n\n" +
                        (ceremonySQL.reconnect().GetSelectedUserIndex() == 1 ? "" : "Now " + (ceremonySQL.reconnect().GetSelectedUserIndex() - 1) + " people have participated to the Quras Trusted Setup.\n") +
                        "It is your turn to participate to the Quras Trusted Setup.\n\n" +
                        "You can participate as followings.\n" +
                        "Step 1. Sign In.\n" +
                        "1) Enter your email address and password.\n" +
                        "2) Press \"Sign In\" button to sign in.\n" +
                        "Step 2.Generate Seed Key\n" +
                        "1) Wait for your turn. Actually, you can check your turn and ceremony status in the client screen.\n" +
                        "2) When it is your turn, enter Seed Text and press \"Create Sead\" button. (You can reset your seed text by pressing \"Reset\" button)\n" +
                        "3) Check the created hex seed and press \"Generate\" button. You can see the status updated.\n\n" +
                        "And if you don't participate to the Quras Trusted Setup in 90 mins, you will be timeout.\n\n" +
                        "Thanks.\n" +
                        "Best Regard.";
                    }
                    else if (mailType == (int)MailType.MAIL_TYPE_TIMEOUT)
                    {
                        body = "Hi " + user.Name + "!\n\n" +
                        "You have not participated to the ceremony in 90 mins, So you have been timeout.\n\n" +
                        "Thanks.\n" +
                        "Best Regard.";
                    }

                    smtpClient.Send("tech@quras.io", user.Email, "Quras Trusted Setup", body);
                }

                LogUtil.Default.Log("Email has been sent to selected user.");
                return true;
            }
            catch (Exception ex)
            {
                LogUtil.Default.Log("Sending email to selected user Failed.");
                LogUtil.Default.Log(ex.ToString());
                return false;
            }
        }

        public bool StartCeremony()
        {
            using (CeremonySQL ceremonySQL = new CeremonySQL())
            {
                if (ceremonySQL.GetCeremonyStatus() != CeremonyStatus.CEREMONY_STATUS_WAITING)
                {
                    return false;
                }

                if (!ceremonySQL.reconnect().StartCeremony())
                {
                    return false;
                }
            }

            return true;
        }
    }
}
