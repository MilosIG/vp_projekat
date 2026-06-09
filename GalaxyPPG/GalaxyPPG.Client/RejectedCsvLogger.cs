using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GalaxyPPG.Client
{
    class RejectedCsvLogger : IDisposable
    {
        private readonly string path;
        private FileStream fileStream;
        private StreamWriter writer;
        private bool disposed;

        public RejectedCsvLogger(string path, bool append)
        {
            this.path = path;

            fileStream = new FileStream(
                path,
                append ? FileMode.Append : FileMode.Create,
                FileAccess.Write,
                FileShare.Read);

            writer = new StreamWriter(fileStream, Encoding.UTF8);

            if (!append)
            {
                writer.WriteLine("RowIndex,Reason,OriginalLine");
            }
        }

        ~RejectedCsvLogger()
        {
            Dispose(false);
        }

        public void WriteRejectedRow(int rowIndex, string reason, string originalLine)
        {
            if (disposed)
            {
                throw new ObjectDisposedException("RejectedCsvLogger");
            }

            writer.WriteLine(
                EscapeCsv(rowIndex.ToString(CultureInfo.InvariantCulture)) + "," +
                EscapeCsv(reason) + "," +
                EscapeCsv(originalLine));

            writer.Flush();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (writer != null)
                    {
                        writer.Dispose();
                        writer = null;
                    }

                    if (fileStream != null)
                    {
                        fileStream.Dispose();
                        fileStream = null;
                    }
                }

                disposed = true;
            }
        }

        private string EscapeCsv(string value)
        {
            if (value == null)
            {
                return "";
            }

            if (value.Contains(",") || value.Contains("\"") || value.Contains("\r") || value.Contains("\n"))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }

            return value;
        }
    }
}
