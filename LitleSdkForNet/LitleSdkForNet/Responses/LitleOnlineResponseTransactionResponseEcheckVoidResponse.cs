using System.Xml.Serialization;
using Litle.Sdk.Xml;

namespace Litle.Sdk.Responses
{
    [LitleXmlType("litleOnlineResponseTransactionResponseEcheckVoidResponse")]
    [LitleXmlRoot("echeckVoidResponse")]
    public class LitleOnlineResponseTransactionResponseEcheckVoidResponse : CommonTransactionTypeWithReportGroupAndPostDate
    {
        [XmlAttribute("duplicate")]
        public bool Duplicate { get; set; }

        [XmlIgnore]
        public bool DuplicateSpecified { get; set; }
    }
}