namespace BlockBase.Domain.Enums
{
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