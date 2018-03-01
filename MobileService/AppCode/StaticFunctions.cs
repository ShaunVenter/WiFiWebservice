using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using Hotspot;
using System.Collections;
using System.Text;
using System.Net.Mail;
using System.Globalization;
using Hotspot.tools;
using System.Net;
using RadiusdbContext;
using System.Linq;
using AlwaysOnMobileService;
using System.Net.Sockets;
using System.Collections.Specialized;

namespace Hotspot.tools
{

    public class staticFunctions
    {
        public static int validateUser(String strEmail, String strMac, String strCreditCard, String strExpMonth, String strExpYear, String strHotspotId, String strIpAddress, String strPackageDescription)
        {
            if (strCreditCard == null)
                strCreditCard = "";

            int intReturnCode = 0;
            /*ArrayList parms = null;*/
            String strBlacklistedOn = "";
            using (var db = new RadiusdbDB())
            {
                if ((strEmail != null) && (strEmail != ""))
                {
                    /* parms = new ArrayList();
                     parms.Add(strEmail);
                     strSQL = "SELECT reason_desc FROM email_blacklist WHERE email_address = ?";
                     dataBlacklist = dcData.getDataTableParamater(strSQL, parms, false);*/

                    var dataBlacklist = (from c in db.Email_Blacklists
                                         where c.Email_Address == strEmail
                                         select c).FirstOrDefault();

                    if (dataBlacklist != null)
                    {
                        intReturnCode = -1;
                        strBlacklistedOn += "Email, ";
                    }
                }
                if ((strMac != null) && (strMac != ""))
                {
                    /*parms = new ArrayList();
                    parms.Add(strMac);
                    strSQL = "SELECT reason_desc FROM mac_blacklist WHERE mac_address = ?";
                    dataBlacklist = dcData.getDataTableParamater(strSQL, parms, false);*/

                    var dataBlacklist = (from c in db.Mac_Blacklists
                                         where c.Mac_Address == strMac
                                         select c).FirstOrDefault();

                    if (dataBlacklist != null)
                    {
                        intReturnCode = -2;
                        strBlacklistedOn += "Mac address, ";
                    }
                }
                if ((strCreditCard != null) && (strCreditCard != ""))
                {
                    /* parms = new ArrayList();
                     parms.Add(hideCardNumber(strCreditCard));
                     parms.Add(strExpMonth + strExpYear);
                     strSQL = "SELECT reason_desc FROM cc_blacklist WHERE cc_number = ? AND expiry_date = ?";
                     dataBlacklist = dcData.getDataTableParamater(strSQL, parms, false);*/

                    var dataBlacklist = (from c in db.Cc_Blacklists
                                         where c.Cc_Number == hideCardNumber(strCreditCard)
                                         && c.Expiry_Date == strExpMonth + strExpYear
                                         select c).FirstOrDefault();

                    if (dataBlacklist != null)
                        intReturnCode = -3;

                    strBlacklistedOn += "Card Number, ";
                }
            }

            if (intReturnCode < 0)
            {
                if (strPackageDescription != null && strPackageDescription != "")
                    sendBlacklistEmail(strMac, hideCardNumber(strCreditCard), strExpMonth + "/" + strExpYear, strEmail, strHotspotId, strIpAddress, strPackageDescription, strBlacklistedOn);
            }

            return intReturnCode;
        }

        public static string hideCardNumber(string strCardNum)
        {
            if (strCardNum.Length > 8)
            {
                int intHideLength;
                StringBuilder sb = new StringBuilder();
                intHideLength = strCardNum.Length - 8;
                sb.Append(strCardNum.Substring(0, 4));
                for (int i = 0; i < intHideLength; i++)
                {
                    sb.Append(".");
                }
                sb.Append(strCardNum.Substring(strCardNum.Length - 4, 4));

                return sb.ToString();
            }
            else return strCardNum;
        }

        public static void sendBlacklistEmail(String strBMac, String strBCCard, String strBExpiryDate, String strBEmail, String strHotspotId, String strIpAddress, String strPackageDescription, String strBlacklistedOn)
        {
            /*String strSQL;
            ArrayList parms;
            dcData.Open();*/

            String strLocationName = "Undefined";
            using (var db = new RadiusdbDB())
            {

                if ((strHotspotId == null) || (strHotspotId == ""))
                {
                    strHotspotId = "0";
                }
                else
                {
                    /* strSQL = "SELECT locationname FROM hotspot WHERE id = ?";
                     parms = new ArrayList();
                     parms.Add(strHotspotId);
                     dataHotspot = dcData.getDataTableParamater(strSQL, parms, false);*/

                    string dataHotspot = (from c in db.Hotspots
                                          where c.Id == Convert.ToInt32(strHotspotId)
                                          select c.Locationname).FirstOrDefault();

                    if (dataHotspot != null && dataHotspot != "")
                    {
                        strLocationName = dataHotspot;
                    }
                }

                try
                {
                    MailAddress from = new MailAddress("hotspots@alwayson.co.za");
                    MailAddress to = new MailAddress("hotspot@alwayson.co.za");

                    System.Net.Mail.MailMessage email = new System.Net.Mail.MailMessage(from, to);

                    email.BodyEncoding = System.Text.Encoding.UTF8;
                    email.IsBodyHtml = true;
                    email.Subject = "Blacklisted user attempted use of services";
                    email.Body = "A blacklisted hotspot user attempted to use our services:<br />" +
                        strHotspotId + " - " + strLocationName + "<br /><br />" +
                        "Blacklisted on:" + strBlacklistedOn + "<br /><br />" +
                        "Description:" + strPackageDescription + "<br />" +
                        "MAC:" + strBMac + "<br />" +
                        "IP:" + strIpAddress + "<br />" +
                        "EMail:" + strBEmail + "<br />" +
                        "CCard:" + hideCardNumber(strBCCard) + " exp:" + strBExpiryDate + "<br />"
                        ;

                    SmtpClient client = new SmtpClient(System.Configuration.ConfigurationManager.AppSettings["MailServer"]);
                    client.Send(email);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        public static String uppercaseFirstLetterOfWord(String strSentence)
        {
            //TextInfo UsaTextInfo = new CultureInfo("es-US", false).TextInfo;
            //string capitalized = UsaTextInfo.ToTitleCase(strSentence);
            //return capitalized;

            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(strSentence);
        }

        public static string getCellNumber(string strCellNo, string strCountryCode)
        {
            if (strCellNo == null ||
                strCellNo == "")
            {
                return "";
            }
            strCellNo = strCellNo.Replace(" ", "");
            strCellNo = strCellNo.Replace("-", "");
            strCellNo = strCellNo.Replace("(", "").Replace(")", "");

            if (strCellNo.StartsWith("+"))
            {
                strCellNo = strCellNo.Substring(1);
            }

            if (strCellNo.StartsWith("00"))
            {
                strCellNo = strCellNo.Substring(2);
            }
            else if (strCellNo.StartsWith("0"))
            {
                strCellNo = strCellNo.Substring(1);
            }

            if (strCellNo.StartsWith(strCountryCode) && strCellNo.Length > 10)
            {
                strCellNo = strCellNo.Substring(strCountryCode.Length);
            }

            return strCellNo;
        }

        public static int getCardType(string strCardNumber)
        {
            //*CARD TYPES            *PREFIX           *WIDTH
            //'American Express       34, 37            15
            //'Diners Club            300 to 305, 36    14
            //'Carte Blanche          38                14
            //'Discover               6011              16
            //'EnRoute                2014, 2149        15
            //'JCB                    3                 16
            //'JCB                    2131, 1800        15
            //'Master Card            51 to 55          16
            //'Visa                   4                 13, 16

            /*
            Card types
            '1' for American Express
            '2' for Discover
            '3' for MasterCard
            '4' for Visa
            '5' for Diners Club
            '6' for Visa Electron (Debit Card)
            '7' for Maestro (Debit Card)
             */

            int intLeftOne = 0;
            int intLeftTwo = 0;
            int intLeftThree = 0;
            int intLeftFour = 0;
            int intCardType = 0;

            try
            {
                Int32.TryParse(strCardNumber.Substring(0, 1), out intLeftOne);
                Int32.TryParse(strCardNumber.Substring(0, 2), out intLeftTwo);
                Int32.TryParse(strCardNumber.Substring(0, 3), out intLeftThree);
                Int32.TryParse(strCardNumber.Substring(0, 4), out intLeftFour);

                if (intLeftTwo == 34 ||
                    intLeftTwo == 37)
                    intCardType = 1; //American Express

                //Don't currently support Discover cards
                //else if (intLeftFour == 6011)
                //    intCardType = 2; //Discover

                else if (intLeftTwo == 51 ||
                    intLeftTwo == 52 ||
                    intLeftTwo == 53 ||
                    intLeftTwo == 54 ||
                    intLeftTwo == 55)
                    intCardType = 3; //MasterCard

                else if (intLeftOne == 4)
                    intCardType = 4; //Visa

                else if (intLeftThree == 300 ||
                    intLeftThree == 300 ||
                    intLeftThree == 300 ||
                    intLeftThree == 300 ||
                    intLeftThree == 300 ||
                    intLeftThree == 300 ||
                    intLeftTwo == 36)
                    intCardType = 5; //Diners Club
            }
            catch (Exception) { }
            return intCardType;
        }

        public static string RestGet(string url, NameValueCollection parameters)
        {
            try
            {
                string urlparams = "";
                foreach (string key in parameters)
                {
                    if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(parameters[key]))
                    {
                        urlparams += (urlparams.Length == 0 ? "" : "&") + key + "=" + parameters[key];
                    }
                }

                url += (url.Contains("?") ? "&" : "?") + urlparams;

                using (var client = new AlwaysOnWebClient() { Timeout = 1000 })
                {
                    return client.DownloadString(url);
                }
            }
            catch (Exception ex)
            {
                Logger.Log("StaticFunction.cs", "RestGet", ex.Message, (ex.StackTrace ?? "") + "\nInnerException: " + Newtonsoft.Json.JsonConvert.SerializeObject((ex.InnerException ?? new Exception(""))), Newtonsoft.Json.JsonConvert.SerializeObject(new { url = url, parameters = parameters }));

                return "";
            }
        }

        public static string RestPost(string url, NameValueCollection parameters)
        {
            try
            {
                using (var client = new AlwaysOnWebClient() { Timeout = 1000 })
                {
                    return Encoding.UTF8.GetString(client.UploadValues(url, "POST", parameters));
                }
            }
            catch (Exception ex)
            {
                Logger.Log("StaticFunction.cs", "RestPost", ex.Message, (ex.StackTrace ?? "") + "\nInnerException: " + Newtonsoft.Json.JsonConvert.SerializeObject((ex.InnerException ?? new Exception(""))), Newtonsoft.Json.JsonConvert.SerializeObject(new { url = url, parameters = parameters }));

                return "";
            }
        }
    }

    public class AlwaysOnWebClient : WebClient
    {
        public int Timeout { get; set; } = 1000;  //Default 1s

        protected override WebRequest GetWebRequest(Uri uri)
        {
            WebRequest w = base.GetWebRequest(uri);
            w.Timeout = Timeout;
            return w;
        }
    }
}
