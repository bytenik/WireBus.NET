using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace WireBus
{
    [Serializable, ProtoContract]
    class DefaultMessageEnvelope : IEnvelope
    {
        #region Implementation of IEnvelope

        [ProtoMember(1, IsRequired=true)]
        public string TypeName { get; set; }

        #endregion
    }
}
