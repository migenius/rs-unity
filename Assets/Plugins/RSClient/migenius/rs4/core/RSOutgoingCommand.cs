using System.Collections;

namespace com.migenius.rs4.core
{
    /**
     * An internal class for pairing a command with a response callback.
     */
    public class RSOutgoingCommand
    {
        public RSService Service { get; protected set; }
        public IRSCommand Command { get; protected set; }
        public ResponseHandler Callback { get; protected set; }
        public int CommandId { get; protected set; }

        public RSOutgoingCommand(RSService service, IRSCommand command, ResponseHandler callback = null, int commandId = -1)
        {
            Service = service;
            Command = command;
            Callback = callback;
            CommandId = commandId;
        }

        /**
         * If an error occured then we want to wrap up the error in a format
         * that's appropriate for the response callback.
         */
        public void DoClientErrorCallback(string message, int code)
        {
            if (Callback == null)
            {
                return;
            }

            Hashtable result = new Hashtable()
            {
                {"error", new Hashtable() {
                    {"message", message},
                    {"code", code}
                }}
            };

            Callback(new RSResponse(Command, result));
        }

        public void DoResultCallback(Hashtable response)
        {
            if (Callback == null)
            {
                return;
            }

            Callback(new RSResponse(Command, response));
        }

        public Hashtable ToJSONObject()
        {
            return Command.ToJSONObject(CommandId); 
        }
        public string ToJSON()
        {
            return Command.ToJSON(CommandId);
        }
    }
}
