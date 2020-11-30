using CeremonyClient.Dialogs.NotifyMessage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CeremonyClient.Utils
{
    public static class StaticUtils
    {
        public static NotifyMessageManager NotifyMessageMgr;

        public static Brush ErrorBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0xFD, 0x4C, 59));
        public static Brush GreenBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x4f, 0xBF, 0x81));
        public static Brush BlueBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x43, 0x91, 0xDB));


        public static void ShowMessageBox(System.Windows.Media.Brush skin, string body)
        {
            NotifyMessage msg = null;

            msg = new NotifyMessage(skin, body,
                                    () => { });

            NotifyMessageMgr.EnqueueMessage(msg);
        }
    }
}
