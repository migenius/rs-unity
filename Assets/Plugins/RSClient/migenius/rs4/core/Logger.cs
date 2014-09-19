using System;

namespace com.migenius.rs4.core
{
    public class Logger
    {
        public delegate void LogHandler(string category, params object[] values);
        public static event LogHandler OnLog;

        public static void Log(string category, params object[] values)
        {
            if (OnLog != null)
            {
                OnLog(category, values);
            }
        }
    }
}

