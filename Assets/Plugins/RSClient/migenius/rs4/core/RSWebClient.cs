using System;
using System.Net;

namespace com.migenius.rs4.core
{
    /**
     * Simple derived WebClient class which adds a configurable timeout
     * which is needed to enable large timeouts for requests which can
     * take considerable time such as scene loading requests. 
     */
    public class RSWebClient : WebClient
    {
        private int timeout;
        public int Timeout
        {
            get {
                return timeout;
            }
            set {
                timeout = value;
            }
        }

        public RSWebClient()
        {
            this.timeout = 100;
        }

        public RSWebClient(int timeout)
        {
            this.timeout = timeout;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var result = base.GetWebRequest(address);
            result.Timeout = this.timeout * 1000; // WebRequest uses milliseconds
            return result;
        }
    }
}
