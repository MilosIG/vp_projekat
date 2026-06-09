using GalaxyPPG.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;

namespace GalaxyPPG.Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string datasetRoot = ReadDatasetRoot();
            string participantId = ReadParticipantId();
            string participantFolder = Path.Combine(datasetRoot, participantId);

            if (!Directory.Exists(participantFolder))
            {
                Console.WriteLine("Folder ucesnika ne postoji: " + participantFolder);
                Console.Read();
                return;
            }

            string ppgFilePath = FindPpgFile(participantFolder);

            if (ppgFilePath == null)
            {
                Console.WriteLine("Nije pronadjen PPG.csv ili BVP.csv u folderu ucesnika: " + participantFolder);
                Console.Read();
                return;
            }

            bool simulateBreak = ReadSimulationOption();

            ChannelFactory<IPpgService> factory = null;
            IPpgService proxy = null;
            IClientChannel channel = null;

            CsvPpgReader reader = new CsvPpgReader();
            int sentSamples = 0;
            bool transferCompleted = false;

            try
            {
                factory = new ChannelFactory<IPpgService>("PpgService");
                proxy = factory.CreateChannel();
                channel = proxy as IClientChannel;

                List<PpgSample> samples = reader.ReadSamples(ppgFilePath, participantId);

                SessionMeta meta = new SessionMeta
                {
                    ParticipantId = participantId,
                    DeviceId = "GalaxyWatch",
                    SampleRateHz = 25,
                    TimestampOffsetMs = 0
                };

                proxy.StartSession(meta);

                foreach (PpgSample sample in samples)
                {
                    if (simulateBreak && sentSamples == 5)
                    {
                        throw new Exception("Simulacija prekida prenosa nakon 5 poslatih uzoraka.");
                    }

                    try
                    {
                        proxy.PushSample(sample);
                        sentSamples++;
                    }
                    catch (FaultException<DataFormatFault> e)
                    {
                        reader.AppendRejectedRow(sample.RowIndex, e.Detail.Message, reader.GetOriginalLine(sample.RowIndex));
                        Console.WriteLine("Server odbio red " + sample.RowIndex + ": " + e.Detail.Message);
                        continue;
                    }
                    catch (FaultException<ValidationFault> e)
                    {
                        reader.AppendRejectedRow(sample.RowIndex, e.Detail.Message, reader.GetOriginalLine(sample.RowIndex));
                        Console.WriteLine("Server odbio red " + sample.RowIndex + ": " + e.Detail.Message);
                        continue;
                    }
                }

                proxy.EndSession();
                transferCompleted = true;

                CloseClientChannel(channel);
                CloseFactory(factory);

                PrintSummary(ppgFilePath, reader.RejectedLogPath, reader.TotalDataRows, sentSamples, reader.RejectedRows);
            }
            catch (FaultException<DataFormatFault> e)
            {
                Console.WriteLine("DATA FORMAT ERROR : " + e.Detail.Message);
            }
            catch (FaultException<ValidationFault> e)
            {
                Console.WriteLine("VALIDATION ERROR : " + e.Detail.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR : " + e.Message);
            }
            finally
            {
                if (!transferCompleted)
                {
                    Console.WriteLine("Transfer nije kompletno zavrsen. Pokrece se cleanup resursa.");
                }

                AbortClientChannel(channel);
                AbortFactory(factory);

                Console.WriteLine("WCF proxy/factory resursi su zatvoreni ili abortovani u finally bloku.");
            }

            Console.Read();
        }

        private static string ReadDatasetRoot()
        {
            Console.WriteLine("Unesite root putanju GalaxyPPG dataset-a:");
            return (Console.ReadLine() ?? "").Trim();
        }

        private static string ReadParticipantId()
        {
            while (true)
            {
                Console.WriteLine("Unesite ucesnika:");
                string participantId = (Console.ReadLine() ?? "").Trim().ToUpperInvariant();

                if (IsValidParticipantId(participantId))
                {
                    return participantId;
                }

                Console.WriteLine("Pogresan unos. Dozvoljeni ucesnici su P01 do P24.");
            }
        }

        private static bool ReadSimulationOption()
        {
            Console.WriteLine("Da li zelite simulaciju prekida prenosa? DA/NE");
            string answer = (Console.ReadLine() ?? "").Trim();

            return string.Equals(answer, "DA", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsValidParticipantId(string participantId)
        {
            if (participantId.Length != 3 || participantId[0] != 'P')
            {
                return false;
            }

            int number;
            return int.TryParse(participantId.Substring(1), out number) && number >= 1 && number <= 24;
        }

        private static string FindPpgFile(string participantFolder)
        {
            string[] csvFiles = Directory.GetFiles(participantFolder, "*.csv", SearchOption.AllDirectories);

            string galaxyWatchPpg = ChoosePreferredFile(csvFiles, "PPG.csv", true);
            if (galaxyWatchPpg != null)
            {
                return galaxyWatchPpg;
            }

            string galaxyWatchBvp = ChoosePreferredFile(csvFiles, "BVP.csv", true);
            if (galaxyWatchBvp != null)
            {
                return galaxyWatchBvp;
            }

            string ppgFile = ChoosePreferredFile(csvFiles, "PPG.csv", false);
            if (ppgFile != null)
            {
                return ppgFile;
            }

            return ChoosePreferredFile(csvFiles, "BVP.csv", false);
        }

        private static string ChoosePreferredFile(string[] csvFiles, string fileName, bool requireGalaxyWatch)
        {
            List<string> candidates = csvFiles
                .Where(path => string.Equals(Path.GetFileName(path), fileName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (requireGalaxyWatch)
            {
                candidates = candidates
                    .Where(path => path.IndexOf("GalaxyWatch", StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }

            return candidates.Count == 0 ? null : candidates[0];
        }

        private static void CloseClientChannel(IClientChannel channel)
        {
            if (channel == null)
            {
                return;
            }

            if (channel.State != CommunicationState.Faulted &&
                channel.State != CommunicationState.Closed)
            {
                channel.Close();
            }
        }

        private static void CloseFactory(ChannelFactory<IPpgService> factory)
        {
            if (factory == null)
            {
                return;
            }

            if (factory.State != CommunicationState.Faulted &&
                factory.State != CommunicationState.Closed)
            {
                factory.Close();
            }
        }

        private static void AbortClientChannel(IClientChannel channel)
        {
            if (channel == null)
            {
                return;
            }

            if (channel.State != CommunicationState.Closed)
            {
                channel.Abort();
            }
        }

        private static void AbortFactory(ChannelFactory<IPpgService> factory)
        {
            if (factory == null)
            {
                return;
            }

            if (factory.State != CommunicationState.Closed)
            {
                factory.Abort();
            }
        }

        private static void PrintSummary(string ppgFilePath, string rejectedLogPath, int readRows, int sentSamples, int rejectedRows)
        {
            Console.WriteLine("Transfer summary:");
            Console.WriteLine("PPG/BVP CSV fajl: " + ppgFilePath);
            Console.WriteLine("Rejected log: " + rejectedLogPath);
            Console.WriteLine("Procitani redovi: " + readRows);
            Console.WriteLine("Uspesno poslati uzorci: " + sentSamples);
            Console.WriteLine("Odbijeni/problematicni redovi: " + rejectedRows);
        }
    }
}