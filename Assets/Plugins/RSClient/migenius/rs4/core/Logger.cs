using System;

namespace com.migenius.rs4.core
{
    /**
     * Very simple logger class.
     * As different systems have different ways of logging information, this was used to
     * abstract those differences away. As in normal C# using Console.Write might be what
     * is required, or perhaps System.Diagnostics.Debug.Write, or in the case of Unity Debug.Log
     *
     * So instead of the logger actually logging anything it simply triggers events that another
     * part of the system can use to log the information.
     *
     * Also adds the feature of logging with a particular category in mind, so general
     * "debug" statements can probably be ignored most of the time if they are cluttering
     * log streams.
     */
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

