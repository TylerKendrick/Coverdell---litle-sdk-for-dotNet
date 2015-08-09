using System;
using System.Xml.Serialization;
using Litle.Sdk.Requests;

namespace Litle.Sdk.Responses
{
    [XmlInclude(typeof (TransactionTypeOptionReportGroup))]
    [XmlInclude(typeof (TransactionTypeWithReportGroupAndPartial))]
    [XmlInclude(typeof (TransactionTypeWithReportGroup))]
    [XmlInclude(typeof (RegisterTokenRequestType))]
    [Serializable]
    [XmlType(Namespace = "http://www.litle.com/schema")]
    public class TransactionType : TransactionRequest
    {
        private string _idField;
        private string _customerIdField;

        [XmlAttribute]
        public string ID
        {
            get { return _idField; }
            set { _idField = value; }
        }

        [XmlAttribute]
        public string CustomerId
        {
            get { return _customerIdField; }
            set { _customerIdField = value; }
        }
    }
}