namespace BlockBase.Domain.Enums
{
    //rpinto - state names should reflect state changes instead of only -> done
    public enum SidechainPoolStateEnum
    {
        RecoverInfo,
        CandidatureTime,
        ConfigTime,
        IPReceiveTime,
        IPSendTime,
        SecretTime,
        InitMining,
        WaitForNextState,
        Unknown
    }
}