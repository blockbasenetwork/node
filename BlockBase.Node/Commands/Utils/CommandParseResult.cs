using System;

namespace BlockBase.Node.Commands.Utils
{
    public class CommandParseResult
    {
        public CommandParseResult(){}

        public CommandParseResult(bool commandRecognized, bool succeeded)
        {
            CommandRecognized = commandRecognized;
            Succeeded = succeeded;
        }

        public CommandParseResult(bool commandRecognized, string parseErrorMessage)
        {
            CommandRecognized = commandRecognized;
            Succeeded = false;
            ParseErrorMessage = parseErrorMessage;
            
        }
        public bool Succeeded { get; set; }
        public bool CommandRecognized { get; set; }
        public string ParseErrorMessage { get; set; }
    }
}