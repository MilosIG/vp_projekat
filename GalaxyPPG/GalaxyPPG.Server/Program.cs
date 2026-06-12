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
                SubscribeToPpgServiceEvents();

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

        private static void SubscribeToPpgServiceEvents()
        {
            PpgService.OnTransferStarted += PpgService_OnTransferStarted;
            PpgService.OnSampleReceived += PpgService_OnSampleReceived;
            PpgService.OnTransferCompleted += PpgService_OnTransferCompleted;
            PpgService.OnWarningRaised += PpgService_OnWarningRaised;
        }

        private static void PpgService_OnTransferStarted(object sender, TransferStartedEventArgs e)
        {
            Console.WriteLine("[EVENT] Transfer started: " + e.ParticipantId + " / " + e.DeviceId);
        }

        private static void PpgService_OnSampleReceived(object sender, SampleReceivedEventArgs e)
        {
            Console.WriteLine("[EVENT] Sample received: " + e.ParticipantId +
                " row " + e.RowIndex +
                " count " + e.ReceivedCount);
        }

        private static void PpgService_OnTransferCompleted(object sender, TransferCompletedEventArgs e)
        {
            Console.WriteLine("[EVENT] Transfer completed: " + e.ParticipantId +
                " / " + e.DeviceId +
                ", received " + e.ReceivedCount + " samples");
        }

        private static void PpgService_OnWarningRaised(object sender, WarningRaisedEventArgs e)
        {
            Console.WriteLine("[WARNING] " + e.WarningType +
                ": " + e.ParticipantId +
                " at " + e.TimestampMs +
                " ms, value=" + e.Value);
        }
    }
}
