using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using GalaxyPPG.Common;

namespace GalaxyPPG.Server
{
    public class PpgService : IPpgService
    {
        private static SessionMeta currentSession;
        private static int receivedSamples = 0;

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

            Console.WriteLine("Transfer started.");
            Console.WriteLine("ParticipantId: " + meta.ParticipantId);
            Console.WriteLine("DeviceId: " + meta.DeviceId);
        }

        public void PushSample(PpgSample sample)
        {
            if (sample == null)
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault("Sample nije prosledjen.")
                );
            }

            ValidateSample(sample);

            receivedSamples++;

            Console.WriteLine("Sample received: " + receivedSamples);
            Console.WriteLine(sample.ToString());
        }

        public void EndSession()
        {
            Console.WriteLine("Transfer completed.");
            Console.WriteLine("Received samples: " + receivedSamples);

            currentSession = null;
            receivedSamples = 0;
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