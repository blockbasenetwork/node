using System;

namespace BlockBase.Node.Commands.Utils
{
    public class CommandExecutionResult
    {

        
        public bool Succeeded { get; set; }
        public OperationResponse Response { get; set; }
        public CommandExecutionException Exception { get; set; }

        
    }
}
