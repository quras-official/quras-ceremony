using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CeremonyServer
{
    class User
    {
        public int Id = 0;
        public string Name = "";
        public string Email = "";
        public string Address = "";
        public string Pubkey = "";
        public string Country = "";
        public int Order = 0;
        public UserStatus Status = UserStatus.USER_STATUS_WAITING;
        public int SpentTime = 0;
        public uint CreatedAt = 0;
        public uint UpdatedAt = 0;
    }
}
