using System.Security;
using Litle.Sdk.Responses;

namespace Litle.Sdk.Requests
{
    public class PayFacDebit : TransactionTypeWithReportGroup
    {
        public string FundingSubmerchantId { get; set; }

        public string FundsTransferId { get; set; }

        public long? Amount { get; set; }

        public override string Serialize()
        {
            string xml = "\r\n<payFacDebit ";

            if (ID != null)
                xml += "id=\"" + SecurityElement.Escape(ID) + "\" ";
            if (CustomerId != null)
                xml += "customerId=\"" + SecurityElement.Escape(CustomerId) + "\" ";
            xml += "reportGroup=\"" + SecurityElement.Escape(ReportGroup) + "\">";
            if (FundingSubmerchantId != null)
                xml += "\r\n<fundingSubmerchantId>" + SecurityElement.Escape(FundingSubmerchantId) +
                       "</fundingSubmerchantId>";
            if (FundsTransferId != null)
                xml += "\r\n<fundsTransferId>" + SecurityElement.Escape(FundsTransferId) + "</fundsTransferId>";
            if (Amount != null)
                xml += "\r\n<amount>" + Amount + "</amount>";

            xml += "\r\n</payFacDebit>";

            return xml;
        }
    }
}