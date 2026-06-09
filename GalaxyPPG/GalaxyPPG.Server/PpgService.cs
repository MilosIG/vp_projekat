using System;
using System.ServiceModel;
using System.IO;
using GalaxyPPG.Common;

namespace GalaxyPPG.Server
{
    public class PpgService : IPpgService
    {
        private static SessionMeta currentSession;
        private static int receivedSamples = 0;

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
            Console.WriteLine("ParticipantId: " + meta.ParticipantId);
            Console.WriteLine("DeviceId: " + meta.DeviceId);
            Console.WriteLine("Session file: " + Path.GetFullPath(sessionPath));
            Console.WriteLine("Rejects file: " + Path.GetFullPath(rejectsPath));
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

                receivedSamples++;

                WriteValidSample(sample);

                Console.WriteLine("Sample received: " + receivedSamples);
                Console.WriteLine(sample.ToString());
            }
            catch (FaultException<ValidationFault> ex)
            {
                WriteReject(ex.Detail.Message, sample.ToString());
                Console.WriteLine("Sample odbijen: " + ex.Detail.Message);
            }
        }

        public void EndSession()
        {
            Console.WriteLine("Transfer completed.");
            Console.WriteLine("Received samples: " + receivedSamples);

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
    }
}