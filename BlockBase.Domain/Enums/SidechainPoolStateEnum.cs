namespace BlockBase.Domain.Enums
{
    //rpinto - state names should reflect state changes instead of only -> done
    public enum SidechainPoolStateEnum
    {
        Starting,
        CandidatureTime,
        ConfigTime,
        IPReceiveTime,
        IPSendTime,
        SecretTime,
        InitProduction,
        WaitForNextState,
        Unknown
    }
}