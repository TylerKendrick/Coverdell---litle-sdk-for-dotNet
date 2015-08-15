using System.Security;
using Litle.Sdk.Responses;

namespace Litle.Sdk.Requests
{
    public class SubmerchantCredit : TransactionTypeWithReportGroup
    {
        public string FundingSubmerchantId { get; set; }

        public string SubmerchantName { get; set; }

        public string FundsTransferId { get; set; }

        public long? Amount { get; set; }

        public EcheckType AccountInfo { get; set; }

        public override string Serialize()
        {
            var xml = "\r\n<submerchantCredit ";

            if (ID != null)
                xml += "id=\"" + SecurityElement.Escape(ID) + "\" ";
            if (CustomerId != null)
                xml += "customerId=\"" + SecurityElement.Escape(CustomerId) + "\" ";
            xml += "reportGroup=\"" + SecurityElement.Escape(ReportGroup) + "\">";
            if (FundingSubmerchantId != null)
                xml += "\r\n<fundingSubmerchantId>" + SecurityElement.Escape(FundingSubmerchantId) +
                       "</fundingSubmerchantId>";
            if (SubmerchantName != null)
                xml += "\r\n<submerchantName>" + SecurityElement.Escape(SubmerchantName) + "</submerchantName>";
            if (FundsTransferId != null)
                xml += "\r\n<fundsTransferId>" + SecurityElement.Escape(FundsTransferId) + "</fundsTransferId>";
            if (Amount != null)
                xml += "\r\n<amount>" + Amount + "</amount>";

            if (AccountInfo != null)
            {
                xml += "\r\n<accountInfo>";
                xml += AccountInfo.Serialize();
                xml += "</accountInfo>";
            }

            xml += "\r\n</submerchantCredit>";

            return xml;
        }
    }
}