using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;

using com.migenius.util;

namespace com.migenius.rs4.core
{
    /**
     * A batch command is a special
     * command that can have any number of sub-commands. The batch command 
     * is processed as a single command and gets only a single reply
     * from the server. The sub-commands can't have individual response 
     * handlers, but the Response class has helper methods that 
     * makes it easier to process the sub-command results of a batch 
     * command.
     * 
     * Batch commands can be nested, meaning that a batch command can
     * have sub-commands that are in turn batch commands and so on. 
     * Batch commands can not contain commands that return binary data 
     * such as the "render" command.
     */
    public class RSBatchCommand : RSCommand, IAddCommand
    {
        // Stores the values of each command.
        // This is added to the internal parameters hash table already.
        public ArrayList CommandValues { get; protected set; }
        public IList<IRSCommand> Commands { get; protected set; }

        /**
         * Create a batch command with no commands.
         */
        public RSBatchCommand()
        {
            CommandValues = new ArrayList();
            parameters["commands"] = CommandValues;

            Commands = new List<IRSCommand>();
        }
		
        /**
         * Create a batch command with a given list of commands to begin with.
         */
		public RSBatchCommand(params IRSCommand[] commands)
		{
            CommandValues = new ArrayList();
            parameters["commands"] = CommandValues;

            Commands = new List<IRSCommand>();
			for(int i = 0; i < commands.Length; i++)
			{
				AddCommand(commands[i]);
			}
		}

        /**
         * Always returns "batch" as batch commands are treated different by RealityServer.
         */
        override public string Name()
        {
            return "batch";
        }
        
        /**
         * Adds a command to the batch. Batch sub-commands will be processed
         * in the same order they are added and their responses can be 
         * accessed from the Response object passed to a response handler 
         * of the batch command itself.
         */
        public void AddCommand(IRSCommand command)
        {
            CommandValues.Add(new Hashtable()
            {
                {"name", command.Name()},
                {"params", command.Parameters()}
            });
            Commands.Add(command);
        }
    }
}
