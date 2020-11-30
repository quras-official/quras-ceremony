using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CeremonyServer
{
    public enum UserStatus : int
    {
        USER_STATUS_WAITING = 0,
        USER_STATUS_OPERATING = 1,
        USER_STATUS_SUCCESSED = 2,
        USER_STATUS_TIMEOUT = 3,
        USER_STATUS_BLOCKED = 4
    }

    public enum CeremonyStatus : int
    {
        CEREMONY_STATUS_WAITING = 0,
        CEREMONY_STATUS_STARTED = 1,
        CEREMONY_STATUS_COMPLETED = 2
    }

    public enum MailType : int
    {
        MAIL_TYPE_START = 0,
        MAIL_TYPE_TIMEOUT = 1
    }
}
