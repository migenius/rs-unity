using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;

using com.migenius.util;

namespace com.migenius.rs4.core
{
    /**
     * A render command is just like a normal command but it indicates that a render is expected
     * as the response, and as such should be treated as binary data and not a JSON response.
     * Also multiple render commands cannot be sent to the server at the same time, so they also
     * break up the command sequence into chunks of safe to send commands.
     */
    public class RSRenderCommand : RSCommand
    {
        public IRSRenderTarget Target { get; protected set; }

        public RSRenderCommand() : base() 
        {
        
        }
        
        public RSRenderCommand(string method) : base(method)
        {
        
        }
        
        public RSRenderCommand(IRSRenderTarget target, string method, params object[] parameters) : base(method, parameters) 
        {
            Target = target;
        }
    }
}
