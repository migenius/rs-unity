using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;

using com.migenius.util;

namespace com.migenius.rs4.core
{
    public interface IRSCommand
    {
        // Return the name of the command.
        string Name(); 
        // Return a hash table containing the parameters to be sent to the server.
        Hashtable Parameters();

        // Return the command as a JSON string with the given id, an id of less than zero
        // will result in a command will not get a response from the server.
        string ToJSON(int id);

		// Returns a JSONObject representing the current IRSCommand. 
		// Does not contain the jsonrpc version, which is left up to the toJSON string
		// function to put in if required.
		//
		// type: Sets the type of this command, it is usually "method" or "name".
		// id: Sets the id of this command. If id is less than zero, then no id property is attached.
		Hashtable ToJSONObject(int id);
    }
}
