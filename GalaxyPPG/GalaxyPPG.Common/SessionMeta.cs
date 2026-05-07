using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.Serialization;

namespace GalaxyPPG.Common
{
    [DataContract]
    public class SessionMeta
    {
        string participantId;
        string deviceId;
        double sampleRateHz;
        long timestampOffsetMs;

        public SessionMeta() : this("", "", 0, 0)
        {
        }

        public SessionMeta(string participantId, string deviceId, double sampleRateHz, long timestampOffsetMs)
        {
            this.participantId = participantId;
            this.deviceId = deviceId;
            this.sampleRateHz = sampleRateHz;
            this.timestampOffsetMs = timestampOffsetMs;
        }

        [DataMember]
        public string ParticipantId { get => participantId; set => participantId = value; }

        [DataMember]
        public string DeviceId { get => deviceId; set => deviceId = value; }

        [DataMember]
        public double SampleRateHz { get => sampleRateHz; set => sampleRateHz = value; }

        [DataMember]
        public long TimestampOffsetMs { get => timestampOffsetMs; set => timestampOffsetMs = value; }
    }
}
