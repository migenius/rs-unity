using System;
using System.Collections;
using System.Collections.Generic;

namespace com.migenius.rs4.core
{
    public class RSResponse
    {
        public IRSCommand Command { get; protected set; }
        public object Result { get; protected set; }
        public Hashtable Error { get; protected set; }
        public string ErrorMessage
        {
            get
            {
                if (Error != null && Error.Contains("message"))
                {
                    return (string)Error["message"];
                }
                return "";
            }
        }
        public bool IsErrorResponse { get; protected set; }
        public bool IsBatchResponse { get; protected set; }
        public IList<RSResponse> SubResponses { get; protected set; }
        public bool HasSubErrorResponse { get; protected set; }
        public object ServerResponse { get; protected set; }

        public RSResponse(IRSCommand command, object serverResponse)
        {
            Command = command;
            ServerResponse = serverResponse;
            
            Hashtable tableResponse = serverResponse as Hashtable;
            if (tableResponse == null)
            {
                IsErrorResponse = true;
                return;
            }

            if (tableResponse.Contains("result"))
            {
                Result = tableResponse["result"];
            }

            if (tableResponse.Contains("error"))
            {
                Hashtable error = tableResponse["error"] as Hashtable;
                if (error != null && error.Contains("code") && Convert.ToInt32(error["code"]) != 0)
                {
                    IsErrorResponse = true;
                    Error = error;
                }
            }

            // Is this a response for a batch command?
            RSBatchCommand batch = command as RSBatchCommand;
            if (batch != null)
            {
                if (!tableResponse.Contains("responses") || !tableResponse.Contains("has_sub_error_response"))
                {
                    // Error
                    return;
                }
                ArrayList responses = tableResponse["responses"] as ArrayList;
                bool subErrors = (bool)tableResponse["has_sub_error_response"];

                IsBatchResponse = true;
                if (responses.Count != batch.Commands.Count)
                {
                    // Error
                    return;
                }

                SubResponses = new List<RSResponse>();
                for (int i = 0; i < batch.Commands.Count; i++)
                {
                    SubResponses.Add(new RSResponse(batch.Commands[i], responses[i]));
                }
                HasSubErrorResponse = subErrors;
            }
            else 
            {
                IsBatchResponse = false;
                SubResponses = null;
                HasSubErrorResponse = false;
            }
        }
    }
}
