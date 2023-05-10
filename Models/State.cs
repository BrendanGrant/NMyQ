using System;
using System.Collections.Generic;

namespace NMyQ.Models
{
    public class State
    {
        public bool gdo_lock_connected { get; set; }
        public bool attached_work_light_error_present { get; set; }
        public string learn_status { get; set; }
        public bool has_camera { get; set; }
        public string battery_backup_state { get; set; }
        public List<ActiveFaultCode> active_fault_codes { get; set; }
        public string attached_camera_serial_number { get; set; }
        public string door_state { get; set; }
        public DateTime last_update { get; set; }
        public bool is_unattended_open_allowed { get; set; }
        public bool is_unattended_close_allowed { get; set; }
        public int service_cycle_count { get; set; }
        public int absolute_cycle_count { get; set; }
        public bool online { get; set; }
        public DateTime last_status { get; set; }
        public bool? allow_bluetooth_lock { get; set; }
        public string firmware_version { get; set; }
        public bool? homekit_capable { get; set; }
        public bool? homekit_enabled { get; set; }
        public bool? learn_mode { get; set; }
        public DateTime? updated_date { get; set; }
        public List<string> physical_devices { get; set; }
        public bool? pending_bootload_abandoned { get; set; }
        public int? wifi_rssi_decibel_milliwatts { get; set; }
        public string wifi_signal_strength { get; set; }
        public string brand_name { get; set; }
        public bool? supports_dealer_diagnostics { get; set; }
        public bool? mandatory_update_required { get; set; }
        public string servers { get; set; }
        public DateTime? online_change_time { get; set; }
        public Links links { get; set; }
        public string paired_gdo_serial_number { get; set; }
        public string last_event { get; set; }
    }
}
