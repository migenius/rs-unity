using System.Collections.Generic;

namespace com.migenius.rs4.core
{
    /**
     * This interface encapsulates the data that is used by the 
     * RealityServer state handlers to decide which state commands are 
     * processed in. The state data can be specified when adding 
     * commands directly to the service, or when regstering a process
     * commands callback. All commands added in a specific callback
     * will operate in the same state. A default state data can also
     * be set on the service itself. This state will then be used for all
     * commands where an explicit state data has not been specified.
     * 
     * <p>States is an optional RealityServer mechanism that can be used
     * to control the scope in which RealityServer commands are executed 
     * and how URL paths are mapped to the content root among other 
     * things. The state handler is a customizable server side component 
     * that decides the state commands should be executed in based on 
     * the data specified by this interface. </p>
     * 
     * <p><b>Note: The StateData class 
     * is designed to be constant. It is not safe to change any members of
     * a StateData object after it has been created, instead a new StateData 
     * instance must be created and used if the state data needs to change.</b>
     * </p>
     */
    public class RSStateData
    {
        /**
         * The path of the state data or null if not defined. This 
         * corresponds to the path part of the URL used when processing requests.
         */
        public string Path { get; protected set; }

        /**
         * The parameters of the state data or null if not defined. The 
         * state data parameters will be added to the URL query string (the part of 
         * the URL that is after the question mark). 
         * <p>Example: 
         * If the paramerters contains the key "param" with the value "myval" the 
         * URL query string passed to the server will be "?param=myval".
         * </p>
         */
        public IDictionary<string, string> Parameters { get; protected set; }

        /**
         * An array of commands that are always executed before 
         * the commands added by callbacks within an execution context
         * on the server. Examples of a command that can be used is the
         * "use_scope". Adding this command will make sure that the 
         * current scope is changed and affects all commands that are 
         * executed in the context of this state data.
         */
        public IList<IRSCommand> StateCommands { get; protected set; }

        /**
         * Creates a StateData object.
         */
        public RSStateData(string path = null, IDictionary<string, string> parameters = null, IList<IRSCommand> stateCommands = null) 
        {
            Path = path;

            Parameters = parameters;
            if (Parameters == null)
            {
                Parameters = new Dictionary<string, string>();
            }

            StateCommands = stateCommands;
            if (StateCommands == null)
            {
                StateCommands = new List<IRSCommand>();
            }
        }
    }
}

