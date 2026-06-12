using System;

namespace GalaxyPPG.Server
{
    public class TransferStartedEventArgs : EventArgs
    {
        public TransferStartedEventArgs(string participantId, string deviceId, DateTime startedAt)
        {
            ParticipantId = participantId;
            DeviceId = deviceId;
            StartedAt = startedAt;
        }

        public string ParticipantId { get; private set; }
        public string DeviceId { get; private set; }
        public DateTime StartedAt { get; private set; }
    }

    public class SampleReceivedEventArgs : EventArgs
    {
        public SampleReceivedEventArgs(string participantId, long timestampMs, int rowIndex, int receivedCount)
        {
            ParticipantId = participantId;
            TimestampMs = timestampMs;
            RowIndex = rowIndex;
            ReceivedCount = receivedCount;
        }

        public string ParticipantId { get; private set; }
        public long TimestampMs { get; private set; }
        public int RowIndex { get; private set; }
        public int ReceivedCount { get; private set; }
    }

    public class TransferCompletedEventArgs : EventArgs
    {
        public TransferCompletedEventArgs(string participantId, string deviceId, int receivedCount, DateTime completedAt)
        {
            ParticipantId = participantId;
            DeviceId = deviceId;
            ReceivedCount = receivedCount;
            CompletedAt = completedAt;
        }

        public string ParticipantId { get; private set; }
        public string DeviceId { get; private set; }
        public int ReceivedCount { get; private set; }
        public DateTime CompletedAt { get; private set; }
    }

    public class WarningRaisedEventArgs : EventArgs
    {
        public WarningRaisedEventArgs(string warningType, string message, string participantId, long timestampMs, double value)
        {
            WarningType = warningType;
            Message = message;
            ParticipantId = participantId;
            TimestampMs = timestampMs;
            Value = value;
        }

        public string WarningType { get; private set; }
        public string Message { get; private set; }
        public string ParticipantId { get; private set; }
        public long TimestampMs { get; private set; }
        public double Value { get; private set; }
    }
}
