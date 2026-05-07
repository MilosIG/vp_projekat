using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace GalaxyPPG.Common
{
    [DataContract]
    public class DataFormatFault
    {
        string message;

        public DataFormatFault(string message)
        {
            this.Message = message;
        }

        [DataMember]
        public string Message { get => message; set => message = value; }
    }
}
