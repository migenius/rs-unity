using System;
using System.Net;
using System.Collections;
using System.Collections.Generic;
using com.migenius.rs4.core;
using com.migenius.util;

namespace com.migenius.rs4.core
{
    public class RSCommand : IRSCommand
    {
        protected string method;
		protected Hashtable parameters;
		
        public RSCommand()
        {
			parameters = new Hashtable();
        }
		
		public RSCommand(string method)
		{
			parameters = new Hashtable();
			this.method = method;
		}
		
		public RSCommand(string method, params object[] parameters)
		{
			this.parameters = new Hashtable();
			this.method = method;
			// We can only accept an even number of objects.
			int length = parameters.Length &(~1);
			
			for(int i = 0; i < length; i += 2)
			{
				this.parameters[parameters[i]] = parameters[i + 1];
			}
		}

		virtual public string Name()
		{
			return method;
		}
        virtual public Hashtable Parameters()
        {
            return parameters;
        }

        public RSCommand(string p_method, Dictionary<string, object> p_params)
        {
            method = p_method;
			parameters = new Hashtable();
			if(p_params == null)
			{
				return;
			}
			
			foreach (KeyValuePair<string, object> item in p_params)
            {
				parameters[item.Key] = item.Value;
			}
        }

        public void AddParameter(string key, object value)
        {
			parameters[key] = value;
        }
        public void SetParameters(Hashtable parameters)
        {
            this.parameters = parameters;
        }

        public string ToJSON(int id)
        {	
			Hashtable jsonObject = ToJSONObject(id);
			jsonObject["jsonrpc"] = "2.0";
			return JSON.JsonEncode(jsonObject);
        }
		
		public Hashtable ToJSONObject(int id)
		{
			Hashtable jsonObject = new Hashtable();
			jsonObject["method"] = Name();
			jsonObject["params"] = parameters;
			
			if (id >= 0)
			{
				jsonObject["id"] = id;
			}
			return jsonObject;
		}

		public override string ToString ()
		{
			string returnString = this.method + " : {";
			foreach (string key in parameters.Keys)
            {
				returnString += key + ":" + parameters[key] + ",";
			}
			
			return returnString + "}";
		}
    }
}
