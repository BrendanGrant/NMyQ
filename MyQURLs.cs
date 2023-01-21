using System;
using System.Collections.Generic;
using System.Text;

namespace NMyQ
{
    public static class MyQURLs
    {
        public static string BaseUri = "https://partner-identity.myq-cloud.com";
        public static string AuthorizeUri = $"{BaseUri}/connect/authorize";
        public static string TokenUri = $"{BaseUri}/connect/token";
        public static string RedirectUri = "com.myqops://ios";
        public static string AccountsUri = "https://accounts.myq-cloud.com/api/v6.0/accounts";
        public static string GetDevicesUri(string accountId) => $"https://devices.myq-cloud.com/api/v5.2/Accounts/{accountId}/Devices";
        public static string GarageDoorCommandUri(string accountId, string serialNumber, string command) => $"https://account-devices-gdo.myq-cloud.com/api/v5.2/Accounts/{accountId}/door_openers/{serialNumber}/{command}";
    }
}
