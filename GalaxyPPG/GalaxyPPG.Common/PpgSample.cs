using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace GalaxyPPG.Common
{
    [DataContract]
    public class PpgSample
    {
        long timestampMs;
        double? ppgGreen;
        double? ppgRed;
        double? ppgIr;
        double? accX;
        double? accY;
        double? accZ;
        double? heartRate;
        double? ibi_ms;
        string participantId;
        int rowIndex;

        public PpgSample() : this(0, 0, 0, 0, 0, 0, 0, 0, 0, "", 0) { }

        public PpgSample(long timestampMs, double? ppgGreen, double? ppgRed, double? ppgIr,
            double? accX, double? accY, double? accZ, double? heartRate,
            double? ibi_ms, string participantId, int rowIndex)
        {
            this.timestampMs = timestampMs;
            this.ppgGreen = ppgGreen;
            this.ppgRed = ppgRed;
            this.ppgIr = ppgIr;
            this.accX = accX;
            this.accY = accY;
            this.accZ = accZ;
            this.heartRate = heartRate;
            this.ibi_ms = ibi_ms;
            this.participantId = participantId;
            this.rowIndex = rowIndex;
        }

        [DataMember]
        public long TimestampMs { get => timestampMs; set => timestampMs = value; }

        [DataMember]
        public double? PpgGreen { get => ppgGreen; set => ppgGreen = value; }

        [DataMember]
        public double? PpgRed { get => ppgRed; set => ppgRed = value; }

        [DataMember]
        public double? PpgIr { get => ppgIr; set => ppgIr = value; }

        [DataMember]
        public double? AccX { get => accX; set => accX = value; }

        [DataMember]
        public double? AccY { get => accY; set => accY = value; }

        [DataMember]
        public double? AccZ { get => accZ; set => accZ = value; }

        [DataMember]
        public double? HeartRate { get => heartRate; set => heartRate = value; }

        [DataMember]
        public double? IBI_ms { get => ibi_ms; set => ibi_ms = value; }

        [DataMember]
        public string ParticipantId { get => participantId; set => participantId = value; }

        [DataMember]
        public int RowIndex { get => rowIndex; set => rowIndex = value; }

        public override string ToString()
        {
            return $"Participant : {ParticipantId} Row : {RowIndex} Timestamp : {TimestampMs} " +
                $"PPG Green : {PpgGreen} PPG Red : {PpgRed} PPG IR : {PpgIr} " +
                $"ACC : ({AccX}, {AccY}, {AccZ}) HR : {HeartRate} IBI : {IBI_ms}";
        }
    }
}
