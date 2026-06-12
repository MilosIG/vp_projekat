using System;
using System.ServiceModel;
using System.IO;
using GalaxyPPG.Common;
using System.Configuration;
using System.Globalization;

namespace GalaxyPPG.Server
{
    public class PpgService : IPpgService
    {
        public static event EventHandler<TransferStartedEventArgs> OnTransferStarted;
        public static event EventHandler<SampleReceivedEventArgs> OnSampleReceived;
        public static event EventHandler<TransferCompletedEventArgs> OnTransferCompleted;
        public static event EventHandler<WarningRaisedEventArgs> OnWarningRaised;

        private static SessionMeta currentSession;
        private static int receivedSamples = 0;
        private static double? previousIbiMs;
        private static int weakPpgCounter;

        private static StreamWriter sessionWriter;
        private static StreamWriter rejectsWriter;

        public void StartSession(SessionMeta meta)
        {
            if (meta == null)
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault("Session meta nije prosledjen.")
                );
            }

            currentSession = meta;
            receivedSamples = 0;
            previousIbiMs = null;
            weakPpgCounter = 0;

            string dateFolder = DateTime.Now.ToString("yyyy-MM-dd");

            string folderPath = Path.Combine(
                "Data",
                meta.ParticipantId,
                meta.DeviceId,
                dateFolder);

            Directory.CreateDirectory(folderPath);

            string sessionPath = Path.Combine(folderPath, "session.csv");
            string rejectsPath = Path.Combine(folderPath, "rejects.csv");

            FileStream sessionFileStream = new FileStream(
                sessionPath,
                FileMode.Append,
                FileAccess.Write,
                FileShare.Read);

            FileStream rejectsFileStream = new FileStream(
                rejectsPath,
                FileMode.Append,
                FileAccess.Write,
                FileShare.Read);

            sessionWriter = new StreamWriter(sessionFileStream);
            rejectsWriter = new StreamWriter(rejectsFileStream);

            if (new FileInfo(sessionPath).Length == 0)
            {
                sessionWriter.WriteLine("TimestampMs,PpgGreen,PpgRed,PpgIr,AccX,AccY,AccZ,HeartRate,IBI_ms,ParticipantId,RowIndex");
                sessionWriter.Flush();
            }

            if (new FileInfo(rejectsPath).Length == 0)
            {
                rejectsWriter.WriteLine("Reason,OriginalSample");
                rejectsWriter.Flush();
            }

            Console.WriteLine("Transfer started.");
            Console.WriteLine("Sequential sample transfer started.");
            Console.WriteLine("ParticipantId: " + meta.ParticipantId);
            Console.WriteLine("DeviceId: " + meta.DeviceId);
            Console.WriteLine("Session file: " + Path.GetFullPath(sessionPath));
            Console.WriteLine("Rejects file: " + Path.GetFullPath(rejectsPath));

            RaiseTransferStarted(meta);
        }

        public void PushSample(PpgSample sample)
        {
            if (sample == null)
            {
                WriteReject("Sample nije prosledjen.", "null");
                Console.WriteLine("Sample odbijen: sample je null.");
                return;
            }

            try
            {
                ValidateSample(sample);
                AnalyzeSample(sample);

                receivedSamples++;

                WriteValidSample(sample);

                RaiseSampleReceived(sample, receivedSamples);

                if (ShouldPrintTransferProgress(receivedSamples))
                {
                    Console.WriteLine("Transfer in progress. Sample received: " + receivedSamples);
                    Console.WriteLine(sample.ToString());
                }
            }
            catch (FaultException<ValidationFault> ex)
            {
                WriteReject(ex.Detail.Message, sample.ToString());
                Console.WriteLine("Sample odbijen: " + ex.Detail.Message);
            }
        }

        public void EndSession()
        {
            SessionMeta completedSession = currentSession;
            int completedSamples = receivedSamples;

            Console.WriteLine("Transfer completed.");
            Console.WriteLine("Received samples: " + receivedSamples);

            if (completedSession != null)
            {
                RaiseTransferCompleted(completedSession, completedSamples);
            }

            if (sessionWriter != null)
            {
                sessionWriter.Close();
                sessionWriter.Dispose();
                sessionWriter = null;
            }

            if (rejectsWriter != null)
            {
                rejectsWriter.Close();
                rejectsWriter.Dispose();
                rejectsWriter = null;
            }

            currentSession = null;
            receivedSamples = 0;
            previousIbiMs = null;
            weakPpgCounter = 0;
        }

        private void WriteValidSample(PpgSample sample)
        {
            if (sessionWriter == null)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault("Session fajl nije otvoren.")
                );
            }

            sessionWriter.WriteLine(
                sample.TimestampMs + "," +
                sample.PpgGreen + "," +
                sample.PpgRed + "," +
                sample.PpgIr + "," +
                sample.AccX + "," +
                sample.AccY + "," +
                sample.AccZ + "," +
                sample.HeartRate + "," +
                sample.IBI_ms + "," +
                sample.ParticipantId + "," +
                sample.RowIndex);

            sessionWriter.Flush();
        }

        private void WriteReject(string reason, string originalSample)
        {
            if (rejectsWriter == null)
            {
                return;
            }

            rejectsWriter.WriteLine(
                EscapeCsv(reason) + "," +
                EscapeCsv(originalSample));

            rejectsWriter.Flush();
        }

        private string EscapeCsv(string value)
        {
            if (value == null)
            {
                return "";
            }

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static bool ShouldPrintTransferProgress(int receivedCount)
        {
            return receivedCount <= 5 || receivedCount % 1000 == 0;
        }

        private void ValidateSample(PpgSample sample)
        {
            if (currentSession == null)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault("Sesija nije pokrenuta. Prvo pozvati StartSession.")
                );
            }

            if (sample.TimestampMs < 0)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault("TimestampMs mora biti veci ili jednak 0.")
                );
            }

            if (sample.HeartRate.HasValue && (sample.HeartRate.Value < 30 || sample.HeartRate.Value > 220))
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault("HeartRate nije u opsegu [30, 220].")
                );
            }

            if ((sample.PpgGreen.HasValue && sample.PpgGreen.Value < 0) ||
                (sample.PpgRed.HasValue && sample.PpgRed.Value < 0) ||
                (sample.PpgIr.HasValue && sample.PpgIr.Value < 0))
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault("PPG vrijednosti moraju biti vece ili jednake 0.")
                );
            }

            if (sample.IBI_ms.HasValue && (sample.IBI_ms.Value < 250 || sample.IBI_ms.Value > 2000))
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault("IBI_ms nije u opsegu [250, 2000].")
                );
            }
        }

        private void AnalyzeSample(PpgSample sample)
        {
            double hrMinBpm = ReadDoubleSetting("HrMinBpm", 50);
            double hrMaxBpm = ReadDoubleSetting("HrMaxBpm", 180);
            double accMotionThreshold = ReadDoubleSetting("AccMotionThreshold", 20);
            double ibiOutOfRangePct = ReadDoubleSetting("IbiOutOfRangePct", 0.2);
            double ppgMinSignalThreshold = ReadDoubleSetting("PpgMinSignalThreshold", 0.01);
            int weakPpgConsecutiveRows = ReadIntSetting("WeakPpgConsecutiveRows", 5);

            if (sample.HeartRate.HasValue && !double.IsNaN(sample.HeartRate.Value))
            {
                double heartRate = sample.HeartRate.Value;

                if (heartRate < hrMinBpm || heartRate > hrMaxBpm)
                {
                    RaiseWarning(
                        "HrOutOfRangeWarning",
                        "HeartRate is outside configured range.",
                        sample,
                        heartRate);
                }
            }

            if (sample.IBI_ms.HasValue && !double.IsNaN(sample.IBI_ms.Value))
            {
                double currentIbi = sample.IBI_ms.Value;

                if (previousIbiMs.HasValue &&
                    Math.Abs(currentIbi - previousIbiMs.Value) > ibiOutOfRangePct * previousIbiMs.Value)
                {
                    RaiseWarning(
                        "IbiSpikeWarning",
                        "IBI changed more than configured percentage.",
                        sample,
                        currentIbi);
                }

                previousIbiMs = currentIbi;
            }

            if (HasUsableValue(sample.AccX) && HasUsableValue(sample.AccY) && HasUsableValue(sample.AccZ))
            {
                double accX = sample.AccX.Value;
                double accY = sample.AccY.Value;
                double accZ = sample.AccZ.Value;
                double anorm = Math.Sqrt(accX * accX + accY * accY + accZ * accZ);

                if (anorm > accMotionThreshold)
                {
                    RaiseWarning(
                        "ExcessiveMotionWarning",
                        "Acceleration norm is above configured threshold.",
                        sample,
                        anorm);
                }
            }

            if (HasUsableValue(sample.PpgGreen) && HasUsableValue(sample.PpgRed) && HasUsableValue(sample.PpgIr))
            {
                if (sample.PpgGreen.Value < ppgMinSignalThreshold &&
                    sample.PpgRed.Value < ppgMinSignalThreshold &&
                    sample.PpgIr.Value < ppgMinSignalThreshold)
                {
                    weakPpgCounter++;
                }
                else
                {
                    weakPpgCounter = 0;
                }

                if (weakPpgCounter > weakPpgConsecutiveRows)
                {
                    RaiseWarning(
                        "WeakPpgWarning",
                        "PPG signal is weak for more than configured consecutive rows.",
                        sample,
                        weakPpgCounter);
                }
            }
        }

        private static bool HasUsableValue(double? value)
        {
            return value.HasValue && !double.IsNaN(value.Value);
        }

        private static double ReadDoubleSetting(string key, double defaultValue)
        {
            string value = ConfigurationManager.AppSettings[key];
            double parsedValue;

            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsedValue))
            {
                return parsedValue;
            }

            return defaultValue;
        }

        private static int ReadIntSetting(string key, int defaultValue)
        {
            string value = ConfigurationManager.AppSettings[key];
            int parsedValue;

            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedValue))
            {
                return parsedValue;
            }

            return defaultValue;
        }

        private void RaiseTransferStarted(SessionMeta meta)
        {
            EventHandler<TransferStartedEventArgs> handler = OnTransferStarted;

            if (handler != null)
            {
                handler(this, new TransferStartedEventArgs(meta.ParticipantId, meta.DeviceId, DateTime.Now));
            }
        }

        private void RaiseSampleReceived(PpgSample sample, int receivedCount)
        {
            EventHandler<SampleReceivedEventArgs> handler = OnSampleReceived;

            if (handler != null)
            {
                handler(this, new SampleReceivedEventArgs(
                    sample.ParticipantId,
                    sample.TimestampMs,
                    sample.RowIndex,
                    receivedCount));
            }
        }

        private void RaiseTransferCompleted(SessionMeta meta, int receivedCount)
        {
            EventHandler<TransferCompletedEventArgs> handler = OnTransferCompleted;

            if (handler != null)
            {
                handler(this, new TransferCompletedEventArgs(
                    meta.ParticipantId,
                    meta.DeviceId,
                    receivedCount,
                    DateTime.Now));
            }
        }

        private void RaiseWarning(string warningType, string message, PpgSample sample, double value)
        {
            EventHandler<WarningRaisedEventArgs> handler = OnWarningRaised;

            if (handler != null)
            {
                handler(this, new WarningRaisedEventArgs(
                    warningType,
                    message,
                    sample.ParticipantId,
                    sample.TimestampMs,
                    value));
            }
        }
    }
}
