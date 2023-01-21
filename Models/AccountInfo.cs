using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMyQ.Models
{
    public class Account
    {
        public string id { get; set; }
        public string name { get; set; }
        public string created_by { get; set; }
        public MaxUsers max_users { get; set; }
    }

    public class MaxUsers
    {
        public int guest { get; set; }
        public int co_owner { get; set; }
    }

    public class AccountInfoResponse
    {
        public List<Account> accounts { get; set; }
    }


}
