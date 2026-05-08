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

            CsvPpgReader reader = new CsvPpgReader();

            //OVO TREBA PROMJENITI KASNIJE U 5. ZADATKA!!!
            List<PpgSample> samples = reader.ReadSamples(
                @"C:\Users\Lenovo\Desktop\test_ppg.csv",
                "P01"
            );

            Console.WriteLine("Loaded samples: " + samples.Count);

            SessionMeta meta = new SessionMeta
            {
                ParticipantId = "P01",
                DeviceId = "GalaxyWatch",
                SampleRateHz = 25,
                TimestampOffsetMs = 0
            };

            

            try
            {
                proxy.StartSession(meta);

                foreach (PpgSample sample in samples)
                {
                    proxy.PushSample(sample);
                }

                proxy.EndSession();

                Console.WriteLine("CSV samples su uspesno poslati.");
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
