using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using com.migenius.util;
using UnityEngine.Networking;
using UnityEngine;

namespace com.migenius.rs4.core
{
    /**
     * The main RealityServer Client Library class that can process
     * RealityServer commands. This interface specifies the methods and 
     * properties that allows commands to be processed by RealityServer 
     * and introduce a couple of important concepts that will be briefly 
     * covered here.
     * 
     * <b>%Command Processing</b>
     * The most obvious goal of the RealityServer client library is to 
     * make it as easy as possible to process RealityServer webservice 
     * commands. The service accepts commands for processing from the 
     * application, for instance by calling RSService.AddCommand(). 
     * Commands will be processed one by one in the order they are added. 
     * If the responses of the commands are of interest it is possible to 
     * add response handlers that will be called when commands have been
     * processed. The service can optimize command processing by sending 
     * a bunch of commands to the server at once, but logically all 
     * commands are serialized and processed one by one. Commands also 
     * fail individually so command processing will always continue with 
     * the next command even if the previous command failed. %Command 
     * processing
     * is asynchronous and adding commands while the service is busy will 
     * cause the commands to queue up, meaning it might take some time 
     * before they are finally sent to the server and processed. The 
     * RealityServer client library will alwasy process all added commands
     * and even if processing of commands might be delayed there is no 
     * concept of a command queue or any possibility to remove reduntant
     * commands once they are added to the service. This means that it is 
     * vital that the application don't add any redundant commands. To be 
     * able to accomplish this the application will need some way to know 
     * when the service is ready to process commands. This is solved by 
     * the central concept called <i>Process Commands Callback</i>.
     * 
     * <b>Process Commands Callback</b>
     * 
     * This is the core mechanism that the RealityServer Client 
     * Library use to process commands and is used to 
     * know when the service is ready to process commands. When the 
     * application needs to process commands, for instance in response
     * to user input, it should generally not add the commands directly
     * by using RSService.AddCommand(). Instead it should add a Process 
     * Commands Callback by calling RSService.AddCallback(callbackFunc). 
     * What the application essentially say is this: Hello service! I
     * have some commands I need to process. Please call the supplied 
     * function when you have time to process them. The service may 
     * then call this function immediately, or after some time if 
     * currently busy, at which point the application adds the commands.
     * 
     * <p>When adding a callback using RSService.AddCallback() it will be 
     * placed in a callback queue. Each callback represents some part of 
     * the application that wish to process commands, for instance to 
     * update the scene database, render the scene, or maybe persist some 
     * data to a database. Each callback, when made, can then add zero or 
     * more commands that will then be processed by the service immediately.
     * It is important to note that while there is no concept of a command 
     * queue in the RealityServer Client Library, there is a process
     * commands callback queue instead. The same callback can only be 
     * added once at a time, but is single shot. When the applicaiton needs 
     * to process commands again it needs add the callback again. It is the 
     * responsibility of the application to keep track of user input, etc,
     * occuring in the time between adding the callback and when it is 
     * actually made, at which point the application must add an optimized
     * sequence of commands.</P>
     * 
     * <b><i>Exmple:</i></b> Scene Navigation
     * 
     * <p>An application wants to implement scene navigation by letting the
     * user drag the mouse on the rendered scnene image. To accomplish this 
     * the application would have to perform the following steps: </p>
     * <p><b>1.</b> When the user triggers a mouse drag event, register a 
    * process commands callback and indicate that a callback is pending.</p>
        * 
        * <p><b>2.</b> While the callback is pending, update a client local 
        * camera transform each time the user triggers new mouse drag events.</p>
        * 
        * <p><b>3.</b> When the callback is made, add a RealityServer command
        * that updates the camera transform to the current value.
        * </p>
        * 
        * <p><b>4.</b> Clear the indication that a callback is pending and 
        * repeat from step 1.</p>
        *
        * <p>As can be seen in this example the callback mechanism can be used 
        * to add a single command to update the camera transform at the time when
        * the service is ready to process the command. This can't be accomplished 
        * with the RSService.AddCommand() method since the service will always 
        * execute all commands added and there is no way to know when it is a good 
        * time to add commands. It is important to note that RSService.AddCommand() 
        * is just a convenience method so that a callback does not need to be 
        * registered when not needed. Internally a call to AddCommand will result 
        * in a callback being added to the callback queue, so commands added this 
        * way will not be executed before any callbacks already in the queue.  
        * This means that adding command A using AddCommand() is equivalent to 
        * adding a callback and adding A when the callback is made. An example 
        * when callbacks are not needed is for instance when initializing an 
        * application. During initialization it is common for a fixed sequence 
        * of commands, for instance to load a scene, create scopes, etc, to be 
        * executed. It is perfectly safe to use both AddCommand() and the 
        * callback mechanism at the same time.</p>
        * 
        * <p><b>State Data</b></p>
        * 
        * <p>RealityServer commands are executed in a specific state that is set 
        * up by optional state handers on the server. The state handler 
        * determines things like the scope in which to execute commands based on
        * parameters passed with the low level request, for 
        * instance an HTTP request. Since the user of the RealityServer Client 
        * library should not have to worry how commands are sent to the server
        * this state data is instead specified using an object implementing the 
        * StateData interface. This interface allow specification of a path 
        * and a set of key/value pairs that the server side state handler then
        * inspects to determine the state to execute the commands in. The 
        * StateData instance to use when executing a command is determined 
        * when calling the RSService.AddCallback method (or the 
                * RSService.AddCommand method). All commands added in the callback 
        * will be associated with this state data and the service will make sure
        * that the commands are processed on the server in such a way that the 
        * RealityServer state handler is invoked with the provided data. Note 
        * that this means that to add commands using different state data they 
        * have to be added in different callbacks. The StateData interface also
        * allow specification of state commands which can be used to for instance
        * call the set_scope command. Again the service will make sure that 
        * commands are processed on the server in such a way that state commands
        * affect all the commands associated with a specific StateData instance.
        * </p>
        * 
        * <p>RSService also allow setting a default StateData to use when no 
        * explicit state data is specified in calls to AddCommand or AddCallback.
        * If no state is specified at all then the commands will be executed in 
        * the default (global) scope.</p>
        * 
        * <p><b>Connectors</b></p>
        * 
        * <p>The RealityServer Client library delegates actual processing of 
        * commands to a connector. All client library implementations support
        * a HTTP connector which process commands using HTTP requests. The 
        * ActionScript libary also supports an RTMP connector in addition to 
        * the HTTP connector which can be enabled to get access to RTMP specific 
        * commands. The %RSService.connectorName can be used to determine which 
        * connector is currently in use. The RSService will also dispatch 
        * %RSService events to indication when the connector has switched so that 
        * the application can take advantage of any connector specific commands.
        * </p>
        */

    public class RSDownloadHandler : DownloadHandlerScript
    {
        private RSService service;
        private List<byte> bytes;
        private UnityWebRequest request;

        public RSDownloadHandler(RSService service, UnityWebRequest request)
        {
            this.service = service;
            this.bytes = new List<byte>();
            this.request = request;
        }

        protected override bool ReceiveData(byte[] data, int dataLength)
        {
            bytes.AddRange(data.Take(dataLength));
            return true;
        }

        protected override void CompleteContent()
        {
            string response = Encoding.UTF8.GetString(bytes.ToArray());
            service.ProcessResponses(response);

            foreach (var service_request in service.m_requests)
            {
                if (service_request.Request == request)
                {
                    service.m_requests.Remove(service_request);
                    break;
                }
            }
        }
    }

    public class RSDownloadHandlerRender : DownloadHandlerScript
    {
        private RSService service;
        private List<byte> bytes;
        private UnityWebRequest request;

        public RSDownloadHandlerRender(RSService service, UnityWebRequest request)
        {
            this.service = service;
            this.bytes = new List<byte>();
            this.request = request;
        }

        protected override bool ReceiveData(byte[] data, int dataLength)
        {
            bytes.AddRange(data.Take(dataLength));
            return true;
        }

        protected override void CompleteContent()
        {
            bool valid = true;
            bool found = false;
            foreach (var service_request in service.m_requests)
            {
                if (service_request.Request == request)
                {
                    valid = service_request.Valid;
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                Logger.Log("warn", "RSDownloadHandlerRender request not found");
                return;
            }

            service.ProcessRenderResponse(bytes.ToArray(), null, valid);

            foreach (var service_request in service.m_requests)
            {
                if (service_request.Request == request)
                {
                    service.m_requests.Remove(service_request);
                    break;
                }
            }
        }
    }

    public class RSSeriveRequest
    {
        public RSSeriveRequest(UnityWebRequest request)
        {
            Request = request;
        }

        public bool Valid = true;
        public UnityWebRequest Request;
    }


    public class RSService : IAddCommand
        {
            public List<RSSeriveRequest> m_requests;

            public void InvalidateRenderRequests()
            {
                foreach (var request in m_requests)
                {
                    request.Valid = false;
                }
            }

            protected class CallbackWrapper
            {
                public SequenceHandler Callback { get; protected set; }
                public RSStateData StateData { get; protected set; }


                public CallbackWrapper(SequenceHandler callback, RSStateData stateData)
                {
                    Callback = callback;
                    StateData = stateData;
                }
            };

            /**
             * The default state data for this RSService instance. If no state 
             * data is specified in the AddCommand and AddCallback methods, 
             * then this is the state data that will be used. 
             */
            public RSStateData DefaultStateData { get; set; }
            public string Host { get; protected set; }
            public int Port { get; protected set; }
            public int Timeout { get; protected set; }
            public int CommandId { get; protected set; }
            /**
             * Returns the base URL to the service. The base URL is the 
             * URL with no path or URL arguments added. 
             * <p>Example: <code>http://somehost:8080/</code></p>
             */
            public string BaseURL { get; protected set; }

            protected RSCommandSequence currentCmdSequence = null;
            /**
             * The service callback that keeps track of commands added by the AddCommand method.
             */
            protected RSCommandSequence currentServiceCallback = null;

            protected Queue<CallbackWrapper> callbackQueue = new Queue<CallbackWrapper>();
            protected bool isBusy = false;
            protected IList<RSOutgoingCommand> currentCommands = null;
            protected IDictionary<int, RSOutgoingCommand> currentCommandsResponseMap = null;

            /** 
             * The response error code for commands that could not be processed
             * because of an internal client side library error. Client side 
             * errors are in the range 5000 to 5999. 
             */
            public static int CLIENT_SIDE_ERROR_CODE_INTERNAL = -5000;
            /** 
             * The response error code for commands that could not be 
             * processed because of a connection error. Client side 
             * errors are in the range 5000 to 5999. 
             */
            public static int CLIENT_SIDE_ERROR_CODE_CONNECTION = -5100;

            public RSService()
            {
                Host = "127.0.0.1";
                Port = 8080;
                Timeout = 100;

                Init();
            }

            public RSService(string host, int port, int timeout = 100)
            {
                Host = host;
                Port =  port;
                Timeout = timeout;
                m_requests = new List<RSSeriveRequest>();

                Init();
            }

            protected void Init()
            {
                BaseURL = "http://" + Host + ":" + Port.ToString() + "/";
            }

            public int NextCommandId()
            {
                return CommandId++;
            }

            /**
             * Adds a command to be processed. The service guarantees that 
             * all added commands will be processed and any response handler will
             * always be called regardless of if previous commands experience 
             * errors. Furthermore added commands will always be executed in the 
             * order they were added.
             * <p>
             * Note that adding commands using this method is equivalent to 
             * registering a process commands callback and adding commands 
             * when the process commands callback is made. This means that any
             * callbacks already registered will be executed before the command
             * (or commands if the delayProcessing flag is used) added using this 
             * method.
             * <p>
             * Example: Adding commands A, B, and C with delayProcessing set to 
             * true for A and B, but false for C will be equivalent to register a 
             * callback and add A, B, and C when the callback is made.
             * 
             * @param command The command to add.
             * 
             * @param handler Optional. If specified, this is 
             * a callback that will be called when the command has been 
             * processed. The object passed in the callback can be used to check 
             * if the command succeeded and to access any returned data.
             *
             * @param stateData Optional. The state data to use. If null or omitted 
             * the default state data will be used as specified in the constructor. 
             * 
             * @param delayProcessing Optional. A hint that tells the service not to try to send the 
             * command immediately. This hint is useful when adding a sequence 
             * of commands in one go. Specifying this flag to true for all 
             * commands except the last one added will ensure that the Service 
             * don't start processing the events immediately, but holds 
             * processing until the last command in the sequence has been added.
             **/
            public void AddCommand(IRSCommand command, ResponseHandler handler, 
                    RSStateData stateData = null, bool delayProcessing = false)
            {
                if (stateData == null)
                {
                    stateData = DefaultStateData;
                }

                lock (callbackQueue)
                {
                    if (currentServiceCallback == null)
                    {
                        currentServiceCallback = new RSCommandSequence(this, stateData);
                    }
                    else if (currentServiceCallback.StateData != stateData)
                    {
                        AddCommandSequence(currentServiceCallback);
                        currentServiceCallback = new RSCommandSequence(this, stateData);
                    }

                    currentServiceCallback.AddCommand(command, handler);

                    if (!delayProcessing)
                    {
                        AddCommandSequence(currentServiceCallback);
                        currentServiceCallback = null;
                    }
                }
            }
            public void AddCommand(IRSCommand command)
            {
                AddCommand(command, null);
            }

            /**
             * Helper function for adding already made command sequences to the current queue.
             */
            public void AddCommandSequence(RSCommandSequence seq, bool delayProcessing = false)
            {
                AddCallback(seq.AddToSequence, seq.StateData, delayProcessing); 
            }

            /**
             * <p>Adds a callback to the end of the callback queue. The callback
             * will be made at the point in time when the service is ready to 
             * process commands generated by this callback. Callbacks will 
             * always be made in the order they were registered with the 
             * service, so if callback A is added before callback B, then A 
             * will be called before B and consequently any commands added by
             * A will be processed before any commands added by B.</p>
             * 
             * <p>Callbacks are one-shot, meaning that a callback needs to be 
             * registered every time the application needs to process commands.
             * The same callback can only be registered once at a time. The 
             * application is responsible for keeping track of any user input 
             * that occurs while waiting for the callback and convert that 
             * user input into an optimized sequence of NWS commands. The same 
             * callback function can be added again as soon as it has been 
             * called or cancelled.</p>
             * 
             * <p>NOTE: When the callback is made the supplied RSCommandSequence
             * instance must be used to add the commands, not RSService.AddCommand().</p>
             * 
             * @param callback The callback. The callback function will be called with a 
             * single argument which is the RSCommandSequence to which commands should be 
             * added using the AddCommand(command, responseHandler) method.
             *
             * @param stateData Optional. The state data to use. If null or omitted 
             * the default state data will be used as specified in the constructor. 
             * 
             * @param delayProcessing Optional. This flag instructs the 
             * service if it should delay processing of the added callback or not. 
             * Defaults to false which is recommended in most cases.
             */ 
            public void AddCallback(SequenceHandler handler, RSStateData stateData = null, bool delayProcessing = false)
            {
                if (handler == null)
                {
                    return;
                }

                if (stateData == null)
                {
                    stateData = DefaultStateData;
                }

                bool found = false;

                lock (callbackQueue)
                {
                    foreach (CallbackWrapper wrapper in callbackQueue)
                    {
                        if (wrapper.Callback == handler)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        callbackQueue.Enqueue(new CallbackWrapper(handler, stateData));
                    }
                }

                if (!delayProcessing)
                {
                    ProcessCallbacks();
                }
            }

            /**
             * Processes the callbacks in the callback queue.
             */
            protected void ProcessCallbacks()
            {
                // If we are busy then nothing to do at the moment.

                RSCommandSequence safeSeq = null;

                lock (callbackQueue)
                {
                    if (isBusy)
                    {
                        return;
                    }

                    // Check if we have left-over work to do.
                    if (currentCmdSequence != null)
                    {
                        // Indicate that we are busy. This prevents any process_callbacks calls
                        // generated by the callbacks to be processed immediately.
                        isBusy = true;

                        // We need to split up the stored command sequence into chunks 
                        // that are safe to send.
                        safeSeq = currentCmdSequence.GetSafeSubsequence();

                        // If no more commands left in m_current_cmd_sequence, set it to 
                        // null. Otherwise keep it and process it again next call.
                        if (currentCmdSequence.Commands.Count == 0)
                        {
                            currentCmdSequence = null;
                        }

                        ProcessCommands(safeSeq);
                        return;
                    }

                    // If no callbacks in the queue, then we are done for now.
                    if (callbackQueue.Count == 0)
                    {
                        return;
                    }

                    // Indicate that we are busy.
                    isBusy = true;

                    // The command sequence we need to fill with commands from callbacks.
                    RSCommandSequence seq = null;

                    // Go through all the current callbacks (and any callbacks 
                    // added by those callbacks) until we hit a binary command
                    // or a callback using a different stateData instance.
                    int callbackCount = 0;
                    while (callbackQueue.Count > 0)
                    {
                        CallbackWrapper frontCallback = callbackQueue.Peek();

                        // Create the command sequence if not done yet.
                        if (seq == null)
                        {
                            seq = new RSCommandSequence(this, frontCallback.StateData);
                        }

                        // Check if the next callback is using the same StateData. 
                        // If not we need to stop and process the current command 
                        // sequence first.
                        if (frontCallback.StateData != seq.StateData)
                        {
                            break;
                        }

                        // Next callback is using the same state data, so call it 
                        // and add the commands to seq.
                        frontCallback = callbackQueue.Dequeue();
                        try
                        {
                            frontCallback.Callback(seq);
                        }
                        catch (Exception e)
                        {
                            // Error
                            Logger.Log("error", "Error in callback: " + frontCallback + "\n- " + e.ToString());
                        }

                        callbackCount++;

                        // If we get a sequence containing binary commands we 
                        // need to stop, otherwise continue calling callbacks if 
                        // there are any left.
                        if (seq.ContainsRenderCommands)
                        {
                            break;
                        }
                    }

                    // We need to split up the command sequence into chunks that are safe to send.
                    safeSeq = seq.GetSafeSubsequence();

                    // If there are still commands in the sequence, then we need 
                    // to store it and continue processing it later.
                    if (seq.Commands.Count > 0)
                    {
                        currentCmdSequence = seq; 
                    }
                }

                ProcessCommands(safeSeq);
            }
            
            /**
             * Function that process all the commands in the given 
             * RSCommandSequence. An assumption is made that all the commands are safe
             * to send in a single request. If there is a render command, it must be 
             * the last command, and no other commands may have callbacks.
             */
            protected void ProcessCommands(RSCommandSequence seq)
            {
                lock (callbackQueue)
                {
                    // If no commands left to process, flag that we are no longer busy 
                    // and continue to process callbacks.
                    if (seq.Commands.Count == 0)
                    {
                        isBusy = false;
                        ProcessCallbacks();
                        return;
                    }

                    // Array containing the commands to send.
                    IList<RSOutgoingCommand> commands = null;
                    IList<IRSCommand> stateCommands = null;
                    if (seq.StateData != null && seq.StateData.StateCommands != null)
                    {
                        stateCommands = seq.StateData.StateCommands;
                    }

                    // Build the array of commands to send.
                    if (stateCommands == null || stateCommands.Count == 0)
                    {
                        // No state commands, use the seq.Commands directly.
                        commands = seq.Commands;
                    }
                    else
                    {
                        // Build a new array including the state commands with all the commands.
                        commands = new List<RSOutgoingCommand>();

                        // Add state commands.
                        if (stateCommands != null)
                        {
                            foreach (IRSCommand cmd in stateCommands)
                            {
                                commands.Add(new RSOutgoingCommand(this, cmd));
                            }
                        }

                        // Add normal commands.
                        foreach (RSOutgoingCommand cmd in seq.Commands)
                        {
                            commands.Add(cmd);
                        }
                    }

                    currentCommands = commands;
                    currentCommandsResponseMap = new Dictionary<int, RSOutgoingCommand>();

                    StringBuilder arrStr = new StringBuilder();
                    bool first = true;
                    arrStr.Append('[');
                    foreach (RSOutgoingCommand cmd in commands)
                    {
                        // Add the command to the response map if it has a callaback.
                        if (cmd.CommandId >= 0)
                        {
                            currentCommandsResponseMap[cmd.CommandId] = cmd;
                        }

                        if (!first)
                        {
                            arrStr.Append(',');
                        }
                        first = false;

                        try
                        {
                            arrStr.Append(cmd.ToJSON());
                        }
                        catch (Exception e)
                        {
                            // FIXME:
                            // Strictly speaking, this callback can't be made 
                            // immediately since that breaks the service contract that 
                            // response handlers are called in the same order they are 
                            // handed to the service.

                            // failed to json serialize the command. call response 
                            // handler with an error (if one is registered).
                            cmd.DoClientErrorCallback("Failed to JSON serialize the command. " + e.ToString(), 
                                    CLIENT_SIDE_ERROR_CODE_INTERNAL);
                        }
                    }
                    arrStr.Append(']');

                    string url = BaseURL;

                    if (seq.ContainsRenderCommands)
                    {
                        string uri = url + "?json_rpc_request=" + arrStr.ToString();
                        UnityWebRequest webRequest = new UnityWebRequest(uri, UnityWebRequest.kHttpVerbGET);
                        webRequest.SetRequestHeader("Content-Type", "application/json");
                        webRequest.downloadHandler = new RSDownloadHandlerRender(this, webRequest);
                        m_requests.Add(new RSSeriveRequest(webRequest));
                        webRequest.SendWebRequest();
                    }
                    else
                    {
                        UnityWebRequest webRequest = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
                        webRequest.SetRequestHeader("Content-Type", "application/json");
                        webRequest.downloadHandler = new RSDownloadHandler(this, webRequest);
                        webRequest.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(arrStr.ToString()));
                        m_requests.Add(new RSSeriveRequest(webRequest));
                        webRequest.SendWebRequest();
                    }
                }
            }

            protected void ErrorAllCallbacks(string message)
            {
                foreach (RSOutgoingCommand command in currentCommands)
                {
                    command.DoClientErrorCallback(message, CLIENT_SIDE_ERROR_CODE_CONNECTION);
                }
            }

            /**
             * Process the given data and error for a render response.
             */
            public void ProcessRenderResponse(byte[] data, string error, bool valid = true)
            {
                RSOutgoingCommand lastCommand = currentCommands[currentCommands.Count - 1];
                RSRenderCommand renderCommand = lastCommand.Command as RSRenderCommand;
                if (renderCommand == null)
                {
                    Logger.Log("error", "Error getting last render command.");
                    ResponseCompleted();
                    return;
                }

                try
                {
                    if (data != null && data.Length >= 2 && (char)data[0] == '[' && (char)data[1] == '{')
                    {
                        ArrayList errors = JSON.JsonDecode(System.Text.Encoding.UTF8.GetString(data)) as ArrayList;
                        Hashtable errorResp = errors[0] as Hashtable;
                        Hashtable errorObj = errorResp["error"] as Hashtable;
                        error = errorObj["message"].ToString();
                    }

                    if (error != null)
                    {
                        lastCommand.DoClientErrorCallback(error, -2);
                        renderCommand.Target.OnError(error);
                    }
                    else if (valid)
                    {
                        if (valid)
                        {
                            bool continueProcessing = renderCommand.Target.OnLoad(renderCommand, this, data);
                            if (!continueProcessing)
                            {
                                return;
                            }
                        }
                        lastCommand.DoResultCallback(new Hashtable() { { "result", new Hashtable()}, {"valid", valid } });
                    }
                }
                catch (Exception e)
                {
                    Logger.Log("error", "Error handling render callback: " + e.ToString());
                }

                ResponseCompleted();
            }

            /**
             * Indicates that the service can continue processing callbacks.
             */
            public void ResponseCompleted()
            {
                isBusy = false;
                ProcessCallbacks();
            }

            public void ProcessResponses(string str)
            {
                Logger.Log("debug", "Received: " + str);
                ArrayList responses = null;
                try
                {
                    responses = JSON.JsonDecode(str) as ArrayList;
                }
                catch (Exception exp)
                {
                    // Error
                    Logger.Log("error", "Error parsing responses: " + exp.ToString());
                    ResponseCompleted();
                    return;
                }

                if (responses == null)
                {
                    Logger.Log("error", "Error parsing responses: parsed responses was null.");
                    ResponseCompleted();
                    return;
                }

                for (int i = 0; i < responses.Count; i++)
                {
                    Hashtable resp = responses[i] as Hashtable;
                    if (resp == null)
                    {
                        // Error
                        Logger.Log("error", "Unable to get response as hashtable: " + responses[i]);
                        continue;
                    }

                    if (resp.Contains("id")) 
                    {
                        int id = Convert.ToInt32(resp["id"]);
                        RSOutgoingCommand cmd = currentCommandsResponseMap[id];
                        cmd.DoResultCallback(resp);
                    }
                    else
                    {
                        Logger.Log("error", "Response does not contain an id: " + resp);
                    }
                }

                ResponseCompleted();
            }
        }
}
