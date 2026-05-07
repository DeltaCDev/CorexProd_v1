using HandyControl.Controls;
using HandyControl.Data;

namespace CorexProd.WPF.Helpers
{
    public static class NotificationService
    {
        public static void Success(string mensaje)
        {
            Growl.Success(new GrowlInfo
            {
                Message = mensaje,
                ShowDateTime = false,
                WaitTime = 2
            });
        }

        public static void Warning(string mensaje)
        {
            Growl.Warning(new GrowlInfo
            {
                Message = mensaje,
                ShowDateTime = false,
                WaitTime = 3
            });
        }

        public static void Error(string mensaje)
        {
            Growl.Error(new GrowlInfo
            {
                Message = mensaje,
                ShowDateTime = false,
                WaitTime = 4
            });
        }

        public static void Info(string mensaje)
        {
            Growl.Info(new GrowlInfo
            {
                Message = mensaje,
                ShowDateTime = false,
                WaitTime = 2
            });
        }
    }
}