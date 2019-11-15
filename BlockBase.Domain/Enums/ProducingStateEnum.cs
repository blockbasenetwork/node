using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Enums
{
    public enum ProducingStateEnum
    {
        Unknown,
        Produce,
        Send,
        WaitForProduceTimeToEnd,
        WaitForSendTimeToEnd,
        AmINextToSend,
        AmINextToProduce,
        Testing
    }
}
