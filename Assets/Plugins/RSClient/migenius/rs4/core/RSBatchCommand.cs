using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;

using com.migenius.util;

namespace com.migenius.rs4.core
{
    public class RSBatchCommand : RSCommand, IAddCommand
    {
        public ArrayList CommandValues { get; protected set; }
        public IList<IRSCommand> Commands { get; protected set; }

        public RSBatchCommand()
        {
            CommandValues = new ArrayList();
            parameters["commands"] = CommandValues;

            Commands = new List<IRSCommand>();
        }
		
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

        override public string Name()
        {
            return "batch";
        }

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
