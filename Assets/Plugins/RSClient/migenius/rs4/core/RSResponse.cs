using System;
using System.Collections;
using System.Collections.Generic;

namespace com.migenius.rs4.core
{
    /**
     * Defines the interface of a command response object. This interface
     * is used by RSService in calls to command response handlers. it 
     * gives access to all the data available in a response to a 
     * RealityServer command.
     * 
     * Batch commands has complex responses containing the responses of 
     * all the batch sub-commands. To make parsing eaiser there are several
     * batch command specific methods added to this interface. The result 
     * object will contain all the information needed, but the  
     * subResponses array contains all the sub-responses as Response 
     * objects. Note that sub-responses can also be responses to nested 
     * batch commands.
     */
    public class RSResponse
    {
        /**
         * Returns the command this is the response to. 
         */
        public IRSCommand Command { get; protected set; }
        /**
         * The result data structure that was returned by the RealityServer
         * command. The result will be null if the command experienced an 
         * error. Commands not returning a value will have an empty object 
         * as result. 
         */
        public object Result { get; protected set; }
        /**
         * Contains information about the error, or null if no error occured. If
         * the error is defined, it will always have a string "message" property 
         * with a short description about the error, and a "code" integer property
         * that identifies the error.
         */
        public Hashtable Error { get; protected set; }
        /**
         * Helper function for returning the error message if there is one,
         * otherwise an empty string is returned.
         */
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
        /**
         * Convenience property that is true if this is an error response.
         * In this case Response.Result will be null and Response.Error 
         * be set to an object containing more information 
         * about the error. 
         */
        public bool IsErrorResponse { get; protected set; }
        /**
         * True if this is the response to a batch command. If true then 
         * the batch specific methods can be used to easier parse the 
         * sub-responses of the batch command.
         */
        public bool IsBatchResponse { get; protected set; }
        /**
         * if IsBatchResponse is true, then this array contains objects of 
         * type RSResponse for all the sub-commands. sub-responses are 
         * in the same order as the sub-commands were added to the original
         * batch request.
         */
        public IList<RSResponse> SubResponses { get; protected set; }
        /**
         * Returns true if any of the sub-responses is an error response. 
         * This function also takes sub-responses of nested batch commands
         * into account. Note that the Response.Error property only say
         * if the batch command itself succedded or not, it does not say 
         * anything about the individual sub-commands. Each sub-command needs
         * to be inspected, and this is a convenience method to determine if
         * error handling is needed or not for the sub-responses.
         */
        public bool HasSubErrorResponse { get; protected set; }
        /**
         * The raw response object returned by the server.
         */
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
