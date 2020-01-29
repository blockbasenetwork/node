

using System;
using Antlr4.Runtime;

namespace BlockBase.Domain.Database.QueryParser
{
public class ThrowingErrorListener : BaseErrorListener 
{
    //todo: remove this 
   public static ThrowingErrorListener INSTANCE = new ThrowingErrorListener();

   public override void SyntaxError(IRecognizer recognizer,  IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
   {
         throw new FormatException("line " + line + ":" + charPositionInLine + " " + msg);
    }
}
}