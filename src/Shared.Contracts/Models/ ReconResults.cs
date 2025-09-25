using System;
using System.Runtime.Serialization;

namespace Shared.Contracts
{
    [DataContract]
    public class ReconResult
    {
        [DataMember(Order = 1)] public bool Success { get; set; }
        [DataMember(Order = 2)] public DateTimeOffset At { get; set; }
        [DataMember(Order = 3)] public double AveragedValue { get; set; }
        [DataMember(Order = 4)] public string Message { get; set; } = "";

        public ReconResult() { }
        public ReconResult(bool ok, DateTimeOffset at, double avg, string msg)
        { Success = ok; At = at; AveragedValue = avg; Message = msg; }
    }
}
