using System;
using System.Xml.Serialization;

namespace Litle.Sdk.Responses
{
    [XmlType("transactionRequest", Namespace = "http://www.litle.com/schema")]
    public class TransactionRequest
    {
        public virtual string Serialize()
        {
            throw new NotImplementedException();
        }
    }
}