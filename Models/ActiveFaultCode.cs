using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMyQ.Models
{
    public class ActiveFaultCode
    {
        public string code { get; set; }
        public string description { get; set; }
        public DateTime activated_timestamp { get; set; } 
    }
}
