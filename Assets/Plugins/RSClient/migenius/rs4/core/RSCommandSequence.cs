using System.Collections.Generic;

namespace com.migenius.rs4.core
{
    /**
     * The CommandSequence class accepts a sequence of commands to be 
     * processed by the service. This is the type of the object passed 
     * in process command callbacks and it should typically not be 
     * instantiated directly.
     */
    public class RSCommandSequence : IAddCommand
    {
        /**
         * The service that created this CommandSequence.
         */
        public RSService Service { get; protected set; }
        /**
         * The state data used for commands added to this command sequence.
         */
        public RSStateData StateData { get; protected set; }

        /**
         * The array containing RSOutgoingCommand objects. 
         */
        protected List<RSOutgoingCommand> commands = new List<RSOutgoingCommand>();
        public IList<RSOutgoingCommand> Commands 
        {
            get
            {
                return commands.AsReadOnly();
            }
        }

        /**
         * True if the sequence contains one or more render commands. 
         */
        public bool ContainsRenderCommands { get; protected set; } 
        /**
         * True if the sequence contains one or more commands with response handlers.
         */
        public bool ContainsResponseHandlers { get; protected set; }

        public RSCommandSequence(RSService service, RSStateData stateData = null)
        {
            Service = service;
            StateData = stateData;

            ContainsRenderCommands = false;
            ContainsResponseHandlers = false;
        }

        /**
         * Copies the contents from the argument into this sequence. 
         * Note that anything in this sequence will be overwritten.
         */
        public void CopyFrom(RSCommandSequence seq)
        {
            Service = seq.Service;
            commands = seq.commands;
            ContainsResponseHandlers = seq.ContainsResponseHandlers;
            ContainsRenderCommands = seq.ContainsRenderCommands;
            StateData = seq.StateData;
        }

        /**
         * Adds a command to this command sequence.
         */
        public void AddCommand(IRSCommand command, ResponseHandler handler)
        {
            int id = -1;
            if (handler != null)
            {
                id = Service.NextCommandId();
                ContainsResponseHandlers = true;
            }
            if (command is RSRenderCommand)
            {
                ContainsRenderCommands = true;
            }

            commands.Add(new RSOutgoingCommand(Service, command, handler, id)); 
        }
        public void AddCommand(IRSCommand command)
        {
            AddCommand(command, null);
        }

        /**
         * Returns a sub sequence as an RSCommandSequence object that is safe to send in a single HTTP request.
         */
        public RSCommandSequence GetSafeSubsequence()
        {
            RSCommandSequence safeSeq = new RSCommandSequence(Service, StateData);

            if (commands.Count == 0)
            {
                return safeSeq;
            }

            // Does the subsequence contain callbacks.
            bool hasCallbacks = false;
            // Does the subsequence contain a render.
            bool hasRenderCommand = false;

            // Go through commands an decide which are safe in a single request
            int i = 0;
            for (; i < commands.Count; i++)
            {
                RSOutgoingCommand command = commands[i];
                if (command.Command is RSRenderCommand)
                {
                    // Two cases:
                    if (hasCallbacks)
                    {
                        // Case 1: There are callbacks, 
                        //         so not safe to include the render command. 
                        break;
                    }
                    
                    // Case 2: There are no callbacks, so we can add the render 
                    //         command to the safe sequence and then break.
                    hasRenderCommand = true;
                    if (command.Callback != null)
                    {
                        hasCallbacks = true;
                    }

                    // We must increase i otherwise the render command will not be included.
                    i++;
                    break;
                }

                if (command.Callback != null)
                {
                    hasCallbacks = true;
                }
            }

            // i now equals the number of commands that are safe to send. Splice the 
            // array into a safe part (return) and a non-safe part (keep).
            List<RSOutgoingCommand> safeCommands = commands;
            if (i < commands.Count)
            {
                safeCommands = commands.GetRange(0, i);
                commands.RemoveRange(0, i);
            }
            else
            {
                commands = new List<RSOutgoingCommand>();
            }

            safeSeq.commands = safeCommands;
            safeSeq.ContainsResponseHandlers = hasCallbacks;
            safeSeq.ContainsRenderCommands = hasRenderCommand;

            return safeSeq;
        }

        public void AddToSequence(RSCommandSequence seq)
        {
            foreach (RSOutgoingCommand cmd in Commands)
            {
                seq.AddCommand(cmd.Command, cmd.Callback);
            }
        }
    }
}
