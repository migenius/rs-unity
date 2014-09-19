using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;

using com.migenius.util;

namespace com.migenius.rs4.core
{
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
