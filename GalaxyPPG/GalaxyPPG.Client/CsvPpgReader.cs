using GalaxyPPG.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GalaxyPPG.Client
{
    class CsvPpgReader
    {
        public List<PpgSample> ReadSamples(string filePath, string participantId)
        {
            List<PpgSample> samples = new List<PpgSample>();

            using (StreamReader reader = new StreamReader(filePath))
            {
                string header = reader.ReadLine();

                string line;
                int rowIndex = 1;

                while ((line = reader.ReadLine()) != null)
                {
                    try
                    {
                        PpgSample sample = ParseLine(line, participantId, rowIndex);
                        samples.Add(sample);
                    }
                    catch (FormatException e)
                    {
                        DataFormatFault fault = new DataFormatFault("Format greska u redu " + rowIndex + ": " + e.Message);
                        Console.WriteLine("DATA FORMAT ERROR : " + fault.Message);
                    }
                    catch (IndexOutOfRangeException e)
                    {
                        DataFormatFault fault = new DataFormatFault("Nedovoljan broj kolona u redu " + rowIndex + ": " + e.Message);
                        Console.WriteLine("DATA FORMAT ERROR : " + fault.Message);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Rejected row " + rowIndex + ": " + e.Message);
                    }

                    rowIndex++;
                }
            }

            return samples;
        }

        private PpgSample ParseLine(string line, string participantId, int rowIndex)
        {
            string[] parts = line.Split(',');

            PpgSample sample = new PpgSample();

            sample.TimestampMs = long.Parse(parts[0], CultureInfo.InvariantCulture);
            sample.PpgGreen = double.Parse(parts[1], CultureInfo.InvariantCulture);
            sample.PpgRed = double.Parse(parts[2], CultureInfo.InvariantCulture);
            sample.PpgIr = double.Parse(parts[3], CultureInfo.InvariantCulture);
            sample.AccX = double.Parse(parts[4], CultureInfo.InvariantCulture);
            sample.AccY = double.Parse(parts[5], CultureInfo.InvariantCulture);
            sample.AccZ = double.Parse(parts[6], CultureInfo.InvariantCulture);
            sample.HeartRate = double.Parse(parts[7], CultureInfo.InvariantCulture);
            sample.IBI_ms = double.Parse(parts[8], CultureInfo.InvariantCulture);

            sample.ParticipantId = participantId;
            sample.RowIndex = rowIndex;

            return sample;
        }
    }
}
