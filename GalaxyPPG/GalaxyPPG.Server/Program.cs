using System;
using System.ServiceModel;

namespace GalaxyPPG.Server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ServiceHost host = null;

            try
            {
                host = new ServiceHost(typeof(PpgService));

                host.Open();

                Console.WriteLine("PpgService is started.");
                Console.WriteLine("Press <enter> to stop service.");

                Console.ReadLine();

                if (host.State != CommunicationState.Faulted)
                {
                    host.Close();
                }

                Console.WriteLine("ServiceHost uspjesno zatvoren.");
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR : " + e.Message);

                if (host != null)
                {
                    host.Abort();
                }
            }
            finally
            {
                if (host != null && host.State != CommunicationState.Closed)
                {
                    host.Abort();
                }

                Console.WriteLine("ServiceHost cleanup zavrsen.");
            }
        }
    }
}