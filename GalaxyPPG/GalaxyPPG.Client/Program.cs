using GalaxyPPG.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace GalaxyPPG.Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ChannelFactory<IPpgService> factory = new ChannelFactory<IPpgService>("PpgService");

            IPpgService proxy = factory.CreateChannel();

            SessionMeta meta = new SessionMeta
            {
                ParticipantId = "P01",
                DeviceId = "GalaxyWatch",
                SampleRateHz = 25,
                TimestampOffsetMs = 0
            };

            PpgSample sample = new PpgSample
            {
                TimestampMs = 1000,
                PpgGreen = 1200,
                PpgRed = 1100,
                PpgIr = 1000,
                AccX = 0.1,
                AccY = 0.2,
                AccZ = 0.9,
                HeartRate = 75,
                IBI_ms = 800,
                ParticipantId = "P01",
                RowIndex = 1
            };

            try
            {
                proxy.StartSession(meta);
                proxy.PushSample(sample);
                proxy.EndSession();

                Console.WriteLine("Test sample je uspesno poslat.");
            }
            catch (FaultException<DataFormatFault> e)
            {
                Console.WriteLine($"ERROR : {e.Detail.Message}");
            }
            catch (FaultException<ValidationFault> e)
            {
                Console.WriteLine($"ERROR : {e.Detail.Message}");
            }

            Console.Read();
        }
    }
}
