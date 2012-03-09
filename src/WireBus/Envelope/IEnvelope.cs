using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WireBus
{
    public interface IEnvelope
    {
        string TypeName { get; set; }
    }
}
