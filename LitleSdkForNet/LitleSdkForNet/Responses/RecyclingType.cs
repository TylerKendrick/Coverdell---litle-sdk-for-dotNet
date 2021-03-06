using System.Xml.Serialization;
using Litle.Sdk.Xml;

namespace Litle.Sdk.Responses
{
    [LitleXmlType("recyclingType")]
    public class RecyclingType
    {
        [XmlElement("recycleAdvice")]
        public RecycleAdviceType RecycleAdvice { get; set; }

        [XmlElement("recycleEngineActive")]
        public bool RecycleEngineActive { get; set; }
    }
}