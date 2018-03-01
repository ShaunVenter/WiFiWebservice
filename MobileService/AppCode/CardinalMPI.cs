using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace Hotspot.AppCode.tools
{
    public class CardinalMPI
    {
        [XmlRoot("CardinalMPI")]
        public class CMPILURequest : CardinalObjectXML<CMPILURequest>
        {
            public string MsgType { get; set; }
            public string Version { get; set; }
            public string ProcessorId { get; set; }
            public string MerchantId { get; set; }
            public string TransactionPwd { get; set; }
            public string TransactionType { get; set; }
            public string Amount { get; set; }
            public string CurrencyCode { get; set; }
            public string OrderNumber { get; set; }
            public string CardNumber { get; set; }
            public string CardExpMonth { get; set; }
            public string CardExpYear { get; set; }
        }

        [XmlRoot("CardinalMPI")]
        public class CMPILUResponse : CardinalObjectXML<CMPILUResponse>
        {
            public string ErrorDesc { get; set; }
            public string ErrorNo { get; set; }
            public string TransactionId { get; set; }
            public string Enrolled { get; set; }
            public string Payload { get; set; }
            public string ACSUrl { get; set; }
            public string EciFlag { get; set; }
        }

        [XmlRoot("CardinalMPI")]
        public class CMPIAuthRequest : CardinalObjectXML<CMPIAuthRequest>
        {
            public string Version { get; set; }
            public string MsgType { get; set; }
            public string ProcessorId { get; set; }
            public string MerchantId { get; set; }
            public string TransactionType { get; set; }
            public string TransactionPwd { get; set; }
            public string TransactionId { get; set; }
            public string PAResPayload { get; set; }
        }

        [XmlRoot("CardinalMPI")]
        public class CMPIAuthResponse : CardinalObjectXML<CMPIAuthResponse>
        {
            public string ErrorDesc { get; set; }
            public string ErrorNo { get; set; }
            public string Cavv { get; set; }
            public string Xid { get; set; }
            public string EciFlag { get; set; }
            public string PAResStatus { get; set; }
            public string SignatureVerification { get; set; }
        }

        public class CardinalObjectXML<T>
        {
            public static T getCardinalObjectFromXMLString(string XML)
            {
                XML = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" + XML.Replace("<CardinalMPI", "<CardinalMPI xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"");
                var ser = new XmlSerializer(typeof(T));
                using (var sr = new StringReader(XML))
                {
                    return (T)ser.Deserialize(sr);
                }
            }
        }
    }
}