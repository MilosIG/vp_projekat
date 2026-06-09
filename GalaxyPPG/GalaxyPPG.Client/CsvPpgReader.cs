using GalaxyPPG.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace GalaxyPPG.Client
{
    class CsvPpgReader
    {
        private string rejectedLogPath;
        private Dictionary<int, string> originalLines = new Dictionary<int, string>();

        public int TotalDataRows { get; private set; }
        public int RejectedRows { get; private set; }
        public string RejectedLogPath { get { return rejectedLogPath; } }

        public List<PpgSample> ReadSamples(string filePath, string participantId)
        {
            List<PpgSample> samples = new List<PpgSample>();
            rejectedLogPath = Path.Combine(Path.GetDirectoryName(filePath), "rejected_client.csv");
            TotalDataRows = 0;
            RejectedRows = 0;
            originalLines.Clear();

            using (StreamReader reader = new StreamReader(filePath))
            using (RejectedCsvLogger rejectedLogger = new RejectedCsvLogger(rejectedLogPath, false))
            {
                string header = reader.ReadLine();
                Dictionary<string, int> columns = BuildColumnMap(header);

                string line;
                int rowIndex = 2;

                while ((line = reader.ReadLine()) != null)
                {
                    TotalDataRows++;
                    originalLines[rowIndex] = line;

                    try
                    {
                        PpgSample sample = ParseLine(line, participantId, rowIndex, columns);
                        samples.Add(sample);
                    }
                    catch (Exception e)
                    {
                        WriteRejectedRow(rejectedLogger, rowIndex, e.Message, line);
                    }

                    rowIndex++;
                }
            }

            return samples;
        }

        public void AppendRejectedRow(int rowIndex, string reason, string originalLine)
        {
            using (RejectedCsvLogger rejectedLogger = new RejectedCsvLogger(rejectedLogPath, true))
            {
                WriteRejectedRow(rejectedLogger, rowIndex, reason, originalLine);
            }
        }

        public string GetOriginalLine(int rowIndex)
        {
            string originalLine;
            return originalLines.TryGetValue(rowIndex, out originalLine) ? originalLine : "";
        }

        private PpgSample ParseLine(string line, string participantId, int rowIndex, Dictionary<string, int> columns)
        {
            string[] parts = SplitCsvLine(line).ToArray();

            PpgSample sample = new PpgSample();

            sample.TimestampMs = ParseTimestampMs(parts, columns);
            sample.PpgGreen = ParseOptionalDouble(parts, columns, "ppggreen", "green", "ppg", "bvp", "value");
            sample.PpgRed = ParseOptionalDouble(parts, columns, "ppgred", "red");
            sample.PpgIr = ParseOptionalDouble(parts, columns, "ppgir", "ir", "infrared");
            sample.AccX = ParseOptionalDouble(parts, columns, "accx", "accelerometerx", "x");
            sample.AccY = ParseOptionalDouble(parts, columns, "accy", "accelerometery", "y");
            sample.AccZ = ParseOptionalDouble(parts, columns, "accz", "accelerometerz", "z");
            sample.HeartRate = ParseOptionalDouble(parts, columns, "heartrate", "hr", "bpm");
            sample.IBI_ms = ParseOptionalDouble(parts, columns, "ibims", "ibi", "rrinterval");
            sample.ParticipantId = participantId;
            sample.RowIndex = rowIndex;

            return sample;
        }

        private Dictionary<string, int> BuildColumnMap(string header)
        {
            Dictionary<string, int> columns = new Dictionary<string, int>();

            if (string.IsNullOrWhiteSpace(header))
            {
                return columns;
            }

            string[] names = SplitCsvLine(header).ToArray();

            for (int i = 0; i < names.Length; i++)
            {
                string key = NormalizeColumnName(names[i]);
                if (!columns.ContainsKey(key))
                {
                    columns.Add(key, i);
                }
            }

            return columns;
        }

        private long ParseTimestampMs(string[] parts, Dictionary<string, int> columns)
        {
            string value = GetValue(parts, columns, "timestampms", "timestamp_ms", "timems", "time_ms");

            if (value != null)
            {
                return long.Parse(value, CultureInfo.InvariantCulture);
            }

            value = GetValue(parts, columns, "timestamp", "time");

            if (value == null)
            {
                throw new FormatException("TimestampMs ili timestamp kolona ne postoji.");
            }

            double timestamp = double.Parse(value, CultureInfo.InvariantCulture);

            if (Math.Abs(timestamp) < 100000000000)
            {
                timestamp *= 1000;
            }

            return Convert.ToInt64(timestamp);
        }

        private double? ParseOptionalDouble(string[] parts, Dictionary<string, int> columns, params string[] aliases)
        {
            string value = GetValue(parts, columns, aliases);

            if (string.IsNullOrWhiteSpace(value) ||
                string.Equals(value.Trim(), "NaN", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return double.Parse(value, CultureInfo.InvariantCulture);
        }

        private string GetValue(string[] parts, Dictionary<string, int> columns, params string[] aliases)
        {
            foreach (string alias in aliases)
            {
                int index;

                if (columns.TryGetValue(NormalizeColumnName(alias), out index))
                {
                    if (index >= parts.Length)
                    {
                        throw new IndexOutOfRangeException("Kolona " + alias + " ne postoji u redu.");
                    }

                    return parts[index].Trim();
                }
            }

            return null;
        }

        private string NormalizeColumnName(string name)
        {
            StringBuilder builder = new StringBuilder();

            foreach (char c in name.Trim().ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(c))
                {
                    builder.Append(c);
                }
            }

            return builder.ToString();
        }

        private IEnumerable<string> SplitCsvLine(string line)
        {
            List<string> values = new List<string>();
            StringBuilder current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    values.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            if (inQuotes)
            {
                throw new FormatException("Nezatvoren navodnik u CSV redu.");
            }

            values.Add(current.ToString());
            return values;
        }

        private void WriteRejectedRow(RejectedCsvLogger rejectedLogger, int rowIndex, string reason, string originalLine)
        {
            RejectedRows++;
            rejectedLogger.WriteRejectedRow(rowIndex, reason, originalLine);
        }
    }
}