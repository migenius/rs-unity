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
        string Name(); 
        Hashtable Parameters();

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
