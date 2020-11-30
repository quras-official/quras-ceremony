using Ceremony;
using Ceremony.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ceremony.Core
{
    public class UserInfo
    {
        public string UserName;
        public string EmailAddr;
        public string Country;
        public int TotalParticipant;
        public int SelectedIndex;
        public int MyOrder;
        public Wallet MyWallet;
    }
}
