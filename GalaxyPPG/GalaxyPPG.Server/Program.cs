using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

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

                host.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR : " + e.Message);

                if (host != null)
                {
                    host.Abort();
                }
            }
        }
    }
}
