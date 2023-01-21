using System;

namespace NMyQ.Models
{
    public class Item
    {
        public string href { get; set; }
        public string serial_number { get; set; }
        public string device_family { get; set; }
        public string device_platform { get; set; }
        public string device_type { get; set; }
        public string device_model { get; set; }
        public string name { get; set; }
        public string parent_device_id { get; set; }
        public DateTime created_date { get; set; }
        public string account_id { get; set; }
        public State state { get; set; }
    }
}
