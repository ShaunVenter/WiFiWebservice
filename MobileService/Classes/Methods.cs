using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Hotspot.AppCode.tools;
using Hotspot.RadiusClient;
using Hotspot.tools;
using RadiusdbContext;
using SecureradiusdbContext;
using System.Net.Mail;
using System.Net;
using AlwaysOnMobileService.HotservTools;
using System.Globalization;

namespace AlwaysOnMobileService
{
    public class Methods
    {
        public static UsageLeft getPackageDetails(string username, string accounttype)
        {
            UsageLeft ul = new UsageLeft();

            var tbl = getPackageDetailsGroupAccount(username);

            double total = 0;


            if (tbl != null)
            {
                total = tbl.dblBalance + tbl.dblTotalValueUsed;
                double i = (tbl.dblBalance / total) * 100;
                ul.percentage = i;

                if (tbl.dblTotalValueUsed == 0.00)
                {
                    ul.percentage = 100;
                }
                if (tbl.dblTotalValue == 0.00)
                {
                    tbl = getPackageDetailsNotGroupAccount(username, false);
                    ul.usageleft = tbl.lblAccountBalance;

                    if (tbl.dblTotalValue == 0.0)
                    {
                        ul.percentage = 100;
                    }
                    else
                    {
                        if (accounttype.ToLower().Contains("data"))
                        {
                            total = tbl.dblBalance + tbl.dblTotalValueUsed;
                            ul.percentage = (tbl.dblBalance / total) * 100;
                        }
                        else
                        {
                            double tot = tbl.dblTotalValue / 60;
                            ul.percentage = (tbl.dblBalance / tot) * 100;
                            ul.usageleft = tbl.lblAccountBalance;
                        }
                    }
                }
                else
                {
                    ul.usageleft = tbl.lblAccountBalance;
                }
            }

            return ul;
        }

        public static CompletePackageDetail getPackageDetailsGroupAccount(string Username)
        {

            double dblTotalValue = 0.0;
            string lblAccountType = "";
            string lblAccountAllowance = "";
            bool boolData = false;
            string strAdditionalText = "";
            double dblTotalValueUsed = 0.0;
            double dblBalance = 0.0;
            string lblAccountTotalUsage = "";
            string lblAccountBalance = "";

            CompletePackageDetail completepackage = new CompletePackageDetail();

            PackageGroup pg = getPackageGroup(Username);
            PackageValue strSQL = null;

            if (pg != null)
            {
                dblTotalValue = Convert.ToDouble(pg.Value);

                using (var db = new RadiusdbDB())
                {
                    strSQL = (from c in db.Usage_Detail_Group_Always_News
                              where c.Username == Username
                              group c by c.Username into g
                              select new PackageValue
                              {
                                  Total = (decimal)g.Sum(x => x.Timeused),
                                  DataTotal = ((decimal)g.Sum(x => x.Inputkb) + (decimal)g.Sum(x => x.Outputkb))
                              }).FirstOrDefault();
                }

                switch (pg.Attribute)
                {
                    case "Max-All-Session":
                        lblAccountType = "Time Limited Account";
                        lblAccountAllowance = String.Format("{0:F2}", (dblTotalValue / 60)) + " Minutes";

                        using (var db = new RadiusdbDB())
                        {
                            strSQL = (from c in db.Usage_Detail_Group_Always_News
                                      where c.Username == Username
                                      group c by c.Username into g
                                      select new PackageValue { Total = (decimal)g.Sum(x => x.Timeused) }).FirstOrDefault();
                        }

                        boolData = false;
                        break;
                    case "Max-Daily-Session":
                        lblAccountType = "Daily Time Limited Account";
                        lblAccountAllowance = String.Format("{0:F2}", (dblTotalValue / 60)) + " Minutes per day";

                        using (var db = new RadiusdbDB())
                        {

                            var FullstrSQL = (from c in db.Usage_Detail_Group_Always_News
                                              where c.Username == Username
                                              select new { username = c.Username, startime = c.Startime, timeused = c.Timeused, inputkb = c.Inputkb, outputkb = c.Outputkb }).ToList()
                                              .Where(a => ConvertFromDateTimeOffset((DateTimeOffset)a.startime).ToString("yyyy/MM/dd") == DateTime.Now.ToString("yyyy/MM/dd")).ToList();

                            strSQL = (from c in FullstrSQL
                                      group c by c.username into g
                                      select new PackageValue { Total = (decimal)g.Sum(x => x.timeused) }).FirstOrDefault();

                        }
                        strAdditionalText = " today";
                        boolData = false;
                        break;
                    case "Max-Recv-Limit":
                        lblAccountType = "Data Limited Account";
                        if (dblTotalValue < 1000000000.0)
                            lblAccountAllowance = String.Format("{0:F2}", (dblTotalValue / 1000000)) + " MB";
                        else
                            lblAccountAllowance = String.Format("{0:F2}", (dblTotalValue / 1000000000)) + " GB";

                        using (var db = new RadiusdbDB())
                        {
                            strSQL = (from c in db.Usage_Detail_Group_Always_News
                                      where c.Username == Username
                                      group c by c.Username into g
                                      select new PackageValue { Total = ((decimal)g.Sum(x => x.Inputkb) + (decimal)g.Sum(x => x.Outputkb)) }).FirstOrDefault();
                        }
                        boolData = true;
                        break;

                    case "Max-Daily-Recv-Limit":
                        lblAccountType = "Daily Data Limited Account";
                        if (dblTotalValue < 1000000000.0)
                            lblAccountAllowance = String.Format("{0:F2}", (dblTotalValue / 1000000)) + " MB per day";
                        else
                            lblAccountAllowance = String.Format("{0:F2}", (dblTotalValue / 1000000000)) + " GB per day";

                        using (var db = new RadiusdbDB())
                        {

                            var FullstrSQL = (from c in db.Usage_Detail_Group_Always_News
                                              where c.Username == Username
                                              select new { username = c.Username, startime = c.Startime, timeused = c.Timeused, inputkb = c.Inputkb, outputkb = c.Outputkb }).ToList()
                                              .Where(a => ConvertFromDateTimeOffset((DateTimeOffset)a.startime).ToString("yyyy/MM/dd") == DateTime.Now.ToString("yyyy/MM/dd")).ToList();

                            strSQL = (from c in FullstrSQL
                                      group c by c.username into g
                                      select new PackageValue { Total = ((decimal)g.Sum(x => x.inputkb) + (decimal)g.Sum(x => x.outputkb)) }).FirstOrDefault();
                        }

                        strAdditionalText = " today";
                        boolData = true;
                        break;
                    case "Max-Monthly-Recv-Limit":
                        lblAccountType = "Monthly Data Limited Account";
                        if (dblTotalValue < 1000000000.0)
                            lblAccountAllowance = String.Format("{0:F2}", (dblTotalValue / 1000000)) + " MB per month";
                        else
                            lblAccountAllowance = String.Format("{0:F2}", (dblTotalValue / 1000000000)) + " GB per month";

                        strAdditionalText = " this month";

                        using (var db = new RadiusdbDB())
                        {
                            var FullstrSQL = (from c in db.Usage_Detail_Group_Always_News
                                              where c.Username == Username
                                              select new { username = c.Username, startime = c.Startime, timeused = c.Timeused, inputkb = c.Inputkb, outputkb = c.Outputkb }).ToList()
                                              .Where(a => ConvertFromDateTimeOffset((DateTimeOffset)a.startime).ToString("yyyy/MM") == DateTime.Now.ToString("yyyy/MM")).ToList();

                            strSQL = (from c in FullstrSQL
                                      group c by c.username into g
                                      select new PackageValue { Total = ((decimal)g.Sum(x => x.inputkb) + (decimal)g.Sum(x => x.outputkb)) }).FirstOrDefault();
                        }



                        boolData = true;
                        break;
                }

                if (strSQL == null)
                {
                    strSQL = new PackageValue()
                    {
                        Total = 0
                    };
                }

                // Account is in a group
                if (strSQL != null)
                {
                    if (strSQL.Total.ToString() != "")
                        dblTotalValueUsed = System.Convert.ToDouble(strSQL.Total.ToString());
                    else
                        dblTotalValueUsed = 0.0;

                    if (!boolData)
                    {
                        dblBalance = (dblTotalValue / 60) - dblTotalValueUsed;
                        lblAccountTotalUsage = String.Format("{0:F2}", dblTotalValueUsed) + " Minutes" + strAdditionalText;
                        lblAccountBalance = String.Format("{0:F2}", dblBalance) + " Minutes" + strAdditionalText;
                    }
                    else
                    {
                        dblBalance = (dblTotalValue / 1000) - dblTotalValueUsed;
                        if (dblTotalValueUsed < 1000000.0)
                        {
                            if (dblTotalValueUsed < 1000.0)
                                lblAccountTotalUsage = String.Format("{0:F2}", dblTotalValueUsed) + " KB" + strAdditionalText;
                            else
                                lblAccountTotalUsage = String.Format("{0:F2}", dblTotalValueUsed / 1000) + " MB" + strAdditionalText;
                        }
                        else
                            lblAccountTotalUsage = String.Format("{0:F2}", dblTotalValueUsed / 1000000) + " GB" + strAdditionalText;

                        if (dblBalance < 1000000.0)
                        {
                            if (dblBalance < 1000.0)
                                lblAccountBalance = String.Format("{0:F2}", dblBalance) + " KB" + strAdditionalText;
                            else
                                lblAccountBalance = String.Format("{0:F2}", dblBalance / 1000) + " MB" + strAdditionalText;
                        }
                        else
                            lblAccountBalance = String.Format("{0:F2}", dblBalance / 1000000) + " GB" + strAdditionalText;
                    }
                }
                else
                    lblAccountTotalUsage = "Not Used" + strAdditionalText;
            }

            completepackage.dblTotalValue = dblTotalValue;
            completepackage.lblAccountType = lblAccountType;
            completepackage.lblAccountAllowance = lblAccountAllowance;
            completepackage.boolData = boolData;
            completepackage.strAdditionalText = strAdditionalText;
            completepackage.dblTotalValueUsed = dblTotalValueUsed;
            completepackage.dblBalance = dblBalance;
            completepackage.lblAccountTotalUsage = lblAccountTotalUsage;
            completepackage.lblAccountBalance = lblAccountBalance;

            return completepackage;
        }

        public static CompletePackageDetail getPackageDetailsNotGroupAccount(string Username, bool UsageRequired)
        {
            double dblTotalValue = 0.0;
            string lblAccountType = "";
            string lblAccountAllowance = "";
            bool boolData = false;
            string strAdditionalText = "";
            double dblTotalValueUsed = 0.0;
            double dblBalance = 0.0;
            string lblAccountTotalUsage = "";
            string lblAccountBalance = "";
            string lblAccountExpiration = "";

            List<PackageGroup> pg = null;
            PackageValue strSQL = null;
            CompletePackageDetail completepackage = new CompletePackageDetail();
            using (var db = new RadiusdbDB())
            {

                pg = (from d in db.Radchecks
                      where d.Username == Username
                      select new PackageGroup { Attribute = d.Attribute, Value = d.Value }).ToList();

            }

            lblAccountExpiration = "Never";
            lblAccountType = "Uncapped";
            lblAccountAllowance = "Unlimited";
            lblAccountBalance = "Unlimited";

            if (UsageRequired)
            {
                using (var db = new RadiusdbDB())
                {

                    strSQL = (from c in db.Checklimit_Views
                              where c.Username == Username
                              group c by c.Username into g
                              select new PackageValue { Total = ((decimal)g.Sum(x => x.Totdata) / 1000) }).FirstOrDefault();
                }
            }
            strAdditionalText = "";
            boolData = true;


            if (pg.Count > 0)
            {
                foreach (PackageGroup p in pg)
                {
                    switch (p.Attribute.ToString())
                    {
                        case "Expiration":
                            lblAccountExpiration = p.Value.ToString();
                            break;
                        case "Max-All-Session":
                            dblTotalValue = System.Convert.ToDouble(p.Value.ToString());
                            lblAccountType = "Time Limited Account";
                            lblAccountAllowance = String.Format("{0:F2}", (dblTotalValue / 60)) + " Minutes";

                            using (var db = new RadiusdbDB())
                            {
                                strSQL = (from c in db.Usage_Detail_Group_Always_News
                                          where c.Username == Username
                                          group c by c.Username into g
                                          select new PackageValue { Total = (decimal)g.Sum(x => x.Timeused) }).FirstOrDefault();
                            }
                            boolData = false;
                            break;
                        case "Max-Daily-Session":
                            dblTotalValue = System.Convert.ToDouble(p.Value.ToString());
                            lblAccountType = "Daily Time Limited Account";
                            lblAccountAllowance = System.Convert.ToString(dblTotalValue / 60) + " Minutes per day";
                            using (var db = new RadiusdbDB())
                            {
                                var FullstrSQL = (from c in db.Usage_Detail_Group_Always_News
                                                  where c.Username == Username
                                                  select new
                                                  {
                                                      username = c.Username,
                                                      startime = c.Startime,
                                                      timeused = c.Timeused,
                                                      inputkb = c.Inputkb,
                                                      outputkb = c.Outputkb
                                                  })
                                                  .ToList()
                                                  .Where(a => ConvertFromDateTimeOffset((DateTimeOffset)a.startime).ToString("yyyy/MM/dd") == DateTime.Now.ToString("yyyy/MM/dd"))
                                                  .ToList();

                                strSQL = (from c in FullstrSQL
                                          group c by c.username into g
                                          select new PackageValue { Total = (decimal)g.Sum(x => x.timeused) }).FirstOrDefault();
                            }
                            strAdditionalText = " today";
                            boolData = false;
                            break;
                        case "Max-Recv-Limit":
                            dblTotalValue = System.Convert.ToDouble(p.Value.ToString());
                            lblAccountType = "Data Limited Account";
                            if (dblTotalValue < 1000000000.0)
                                lblAccountAllowance = String.Format("{0:F2}", (dblTotalValue / 1000000)) + " MB";
                            else
                                lblAccountAllowance = String.Format("{0:F2}", (dblTotalValue / 1000000000)) + " GB";


                            using (var db = new RadiusdbDB())
                            {

                                strSQL = (from c in db.Checklimit_Views
                                          where c.Username == Username
                                          group c by c.Username into g
                                          select new PackageValue { Total = ((decimal)g.Sum(x => x.Totdata) / 1000) }).FirstOrDefault();


                            }
                            boolData = true;
                            break;

                        case "Max-Daily-Recv-Limit":
                            dblTotalValue = System.Convert.ToDouble(p.Value.ToString());
                            lblAccountType = "Daily Data Limited Account";
                            if (dblTotalValue < 1000000000.0)
                                lblAccountAllowance = String.Format("{0:F2}", (dblTotalValue / 1000000)) + " MB per day";
                            else
                                lblAccountAllowance = String.Format("{0:F2}", (dblTotalValue / 1000000000)) + " GB per day";

                            using (var db = new RadiusdbDB())
                            {

                                var FullstrSQL = (from c in db.Usage_Detail_Group_Always_News
                                                  where c.Username == Username
                                                  select new { username = c.Username, startime = c.Startime, timeused = c.Timeused, inputkb = c.Inputkb, outputkb = c.Outputkb }).ToList()
                                                  .Where(a => ConvertFromDateTimeOffset((DateTimeOffset)a.startime).ToString("yyyy/MM/dd") == DateTime.Now.ToString("yyyy/MM/dd")).ToList();

                                strSQL = (from c in FullstrSQL
                                          group c by c.username into g
                                          select new PackageValue { Total = ((decimal)g.Sum(x => x.inputkb) + (decimal)g.Sum(x => x.outputkb)) }).FirstOrDefault();

                            }
                            strAdditionalText = " today";
                            boolData = true;
                            break;
                        case "Max-Monthly-Recv-Limit":
                            dblTotalValue = System.Convert.ToDouble(p.Value.ToString());
                            lblAccountType = "Monthly Data Limited Account";
                            if (dblTotalValue < 1000000000.0)
                                lblAccountAllowance = String.Format("{0:F2}", (dblTotalValue / 1000000)) + " MB per month";
                            else
                                lblAccountAllowance = String.Format("{0:F2}", (dblTotalValue / 1000000000)) + " GB per month";


                            using (var db = new RadiusdbDB())
                            {
                                var FullstrSQL = (from c in db.Usage_Detail_Group_Always_News
                                                  where c.Username == Username
                                                  select new { username = c.Username, startime = c.Startime, timeused = c.Timeused, inputkb = c.Inputkb, outputkb = c.Outputkb }).ToList()
                                                  .Where(a => ConvertFromDateTimeOffset((DateTimeOffset)a.startime).ToString("yyyy/MM") == DateTime.Now.ToString("yyyy/MM")).ToList();

                                strSQL = (from c in FullstrSQL
                                          group c by c.username into g
                                          select new PackageValue { Total = ((decimal)g.Sum(x => x.inputkb) + (decimal)g.Sum(x => x.outputkb)) }).FirstOrDefault();
                            }
                            strAdditionalText = " this month";
                            boolData = true;
                            break;
                    }

                }

                if (strSQL == null)
                {
                    strSQL = new PackageValue()
                    {
                        Total = 0
                    };
                }

                if (strSQL != null)
                {
                    if (strSQL.Total.ToString() != "")
                        dblTotalValueUsed = System.Convert.ToDouble(strSQL.Total.ToString());
                    else
                        dblTotalValueUsed = 0.0;

                    if (!boolData)
                    {
                        dblBalance = (dblTotalValue / 60) - dblTotalValueUsed;
                        lblAccountTotalUsage = String.Format("{0:F2}", dblTotalValueUsed) + " Minutes" + strAdditionalText;
                        lblAccountBalance = String.Format("{0:F2}", dblBalance) + " Minutes" + strAdditionalText;
                    }
                    else
                    {
                        dblBalance = (dblTotalValue / 1000) - dblTotalValueUsed;
                        if (dblTotalValueUsed < 1000000.0)
                        {
                            if (dblTotalValueUsed < 1000.0)
                                lblAccountTotalUsage = String.Format("{0:F2}", dblTotalValueUsed) + " KB" + strAdditionalText;
                            else
                                lblAccountTotalUsage = String.Format("{0:F2}", dblTotalValueUsed / 1000) + " MB" + strAdditionalText;
                        }
                        else
                            lblAccountTotalUsage = String.Format("{0:F2}", dblTotalValueUsed / 1000000) + " GB" + strAdditionalText;

                        if (dblBalance > 0)
                        {
                            if (dblBalance < 1000000.0)
                            {
                                if (dblBalance < 1000.0)
                                    lblAccountBalance = String.Format("{0:F2}", dblBalance) + " KB" + strAdditionalText;
                                else
                                    lblAccountBalance = String.Format("{0:F2}", dblBalance / 1000) + " MB" + strAdditionalText;
                            }
                            else
                                lblAccountBalance = String.Format("{0:F2}", dblBalance / 1000000) + " GB" + strAdditionalText;

                        }

                    }

                }
                else
                    lblAccountTotalUsage = "Not Applicable";
            }
            /*else
            {
                //Service Provider Pacakge
                using (var db = new SecureradiusdbContext.SecureRadiusdb())
                {
                    pg = (from c in db.radchecks
                          where c.username == Username && c.attribute == "Max-Monthly-Data-Limit"
                          select new PackageGroup { Value = c.value }).ToList();
                }

                if (pg.Count > 0)
                {
                    int allowance = Convert.ToInt32(pg[0].Value.ToString());


                    if (allowance > 0)
                    {
                        if (allowance < 1000000.0)
                        {
                            if (allowance < 1000.0)
                                lblAccountAllowance = String.Format("{0:F2}", allowance) + " KB";
                            else
                                lblAccountAllowance = String.Format("{0:F2}", allowance / 1000) + " MB";
                        }
                        else
                        {
                            lblAccountAllowance = String.Format("{0:F2}", allowance / 1000000000) + " GB";
                        }

                        lblAccountType = "Unknown";
                        lblAccountTotalUsage = "Not Applicable";
                    }
                    else
                    {
                        // change to unknown
                        lblAccountAllowance = "Unknown";
                        lblAccountType = "Unknown";
                        lblAccountTotalUsage = "Not Applicable";
                    }
                }
                else
                {
                    //change to unknown
                    lblAccountAllowance = "Unknown";
                    lblAccountType = "Unknown";
                    lblAccountTotalUsage = "Not Applicable";
                }
            }*/

            completepackage.dblTotalValue = dblTotalValue;
            completepackage.lblAccountType = lblAccountType;
            completepackage.lblAccountAllowance = lblAccountAllowance;
            completepackage.boolData = boolData;
            completepackage.strAdditionalText = strAdditionalText;
            completepackage.dblTotalValueUsed = dblTotalValueUsed;
            completepackage.dblBalance = dblBalance;
            completepackage.lblAccountTotalUsage = lblAccountTotalUsage;
            completepackage.lblAccountBalance = lblAccountBalance;
            completepackage.lblAccountExpiration = lblAccountExpiration;


            return completepackage;
        }

        public static DateTime ConvertFromDateTimeOffset(DateTimeOffset dateTime)
        {
            if (dateTime.Offset.Equals(TimeSpan.Zero))
                return dateTime.UtcDateTime;
            else if (dateTime.Offset.Equals(TimeZoneInfo.Local.GetUtcOffset(dateTime.DateTime)))
                return DateTime.SpecifyKind(dateTime.DateTime, DateTimeKind.Local);
            else
                return dateTime.DateTime;
        }

        public static PackageGroup getPackageGroup(string Username)
        {
            List<string> InvalidAttributes = new List<string>
            {
                "Client-IP-Address",
                "Called-Station-Id",
                "NAS-IP-Address"
            };
            using (var db = new RadiusdbDB())
            {
                PackageGroup pg = db.ExecuteQuery<PackageGroup>(@"SELECT	D.attribute,
		                                                            D.value
                                                          FROM usergroup AS C
                                                          JOIN radgroupcheck AS D ON C.groupname = D.groupname
                                                          WHERE C.username = '" + Username + @"'
                                                          AND D.attribute NOT IN ('Client-IP-Address',
                                                                                  'Called-Station-Id',
                                                                                  'NAS-IP-Address')").FirstOrDefault();

                return pg;
            }
        }

        public static Packageitems getPackageDetail(string packageid)
        {
            Packageitems packages = null;

            using (var db = new RadiusdbDB())
            {
                packages = (from ap in db.Access_Packages
                            where ap.Id.ToString() == packageid
                            select new Packageitems
                            {
                                listOrder = ap.List_Order.ToString(),
                                id = ap.Id.ToString(),
                                packageDesc = ap.Package_Desc,
                                defaultPackage = ap.Default_Package,
                                displayPrice = ap.Display_Price,
                                optionDesc = ap.Option_Desc,
                                iveriPrice = ap.Iveri_Price,
                                packageName = ap.Package_Name,
                                packageType = ap.Package_Type,
                                expirationHours = ap.Expiration_Hours.ToString(),
                                expirationDays = ap.Expiration_Days.ToString(),
                                currency = ap.Currency,
                                usageDescription = ap.Usage_Description,
                                isfromfirstlogin = ap.Exp_From_First_Login,
                                radgroupcheckid = ap.Radgroupcheck_Id.ToString(),
                                expiryDays = ap.Expiration_Days.ToString()
                            }).FirstOrDefault();
                return packages;
            }
        }

        public static bool CheckACSUrl(string ACSUrl)
        {
            var domain = ACSUrl.ToLower().Trim();
            var socket = domain.Substring(0, domain.IndexOf(":"));
            domain = domain.Replace(socket + "://", "");
            domain = domain.Substring(0, domain.IndexOf("/")).Replace("/", "");

            using (var db = new RadiusdbDB())
            {
                var ACS = db.Acs_Domains.Where(n => n.Domain == domain && n.Socket == socket).ToList();
                if (ACS.Count == 0)
                {
                    var cmd = string.Format("insert into acs_domain (domain,socket,live) values ('{0}', '{1}', 'N')", domain, socket);
                    db.ExecuteCommand(cmd);
                    db.SubmitChanges();

                    return false;
                }
                else
                {
                    return ACS.Exists(n => n.Live == "Y");
                }
            }
        }

        public static bool existsBeforeLink(string username, string password)
        {
            try
            {
                int id = 0;
                using (var db = new RadiusdbDB())
                {
                    id = (from ao in db.Radchecks
                          where ao.Username == username && ao.Attribute == "Password" && ao.Value == password
                          select ao.Id).FirstOrDefault();

                    if (id == 0)
                    {
                        id = (from ao in db.Wifi_Credentials
                              where ao.Login_Username == username && ao.Login_Password == password
                              select ao.Id).FirstOrDefault();

                        if (id == 0)
                            return false;
                        else
                            return true;
                    }
                    else
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Methods.cs", "existsBeforeLink", ex.Message, (ex.StackTrace ?? "") + "\nInnerException: " + Newtonsoft.Json.JsonConvert.SerializeObject((ex.InnerException ?? new Exception(""))), Newtonsoft.Json.JsonConvert.SerializeObject(new { username = username, password = password }));

                return false;
            }
        }

        public static string isSamsungAccount(string username)
        {
            try
            {
                using (var db = new RadiusdbDB())
                {
                    string result = (from c in db.Samsungregistrations
                                     where c.Username == username
                                     select c.Mac_Address).FirstOrDefault();

                    return result;
                }
            }
            catch (Exception e)
            {
                Logger.Log("Methods.cs", "isSamsungAccount", e.Message, (e.StackTrace ?? "") + "\nInnerException: " + Newtonsoft.Json.JsonConvert.SerializeObject((e.InnerException ?? new Exception(""))), Newtonsoft.Json.JsonConvert.SerializeObject(new { username = username }));

                return null;
            }
        }

        public static string linkToAccount(string username, string password, string id, string accounttype, string providerid, string macaddress, int packageid)
        {
            int wf_id;
            string result = "0";
            try
            {
                using (var db = new RadiusdbDB())
                {
                    wf_id = (from ao in db.Wifi_Credentials
                             where ao.Login_Username == username && ao.Login_Password == password
                             select ao.Id).FirstOrDefault();

                    if (wf_id == 0)
                    {
                        result = db.Addwificredential(username, password, Convert.ToInt32(id), Convert.ToInt32(accounttype), Convert.ToInt32(providerid), macaddress, packageid).ToString();
                    }
                    else
                    {
                        result = "-1"; //already linked
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Methods.cs", "linkToAccount", ex.Message, (ex.StackTrace ?? "") + "\nInnerException: " + Newtonsoft.Json.JsonConvert.SerializeObject((ex.InnerException ?? new Exception(""))), Newtonsoft.Json.JsonConvert.SerializeObject(new { username = username, password = password, id = id, accounttype = accounttype, providerid = providerid, macaddress = macaddress, packageid = packageid }));
            }

            return result;
        }

        public static string linkToAccountSP(string username, string password, string id, string accounttype, string providerid, string macaddress)
        {
            int wf_id;
            string result = "0";
            try
            {
                using (var db = new RadiusdbDB())
                {
                    wf_id = (from ao in db.Wifi_Credentials
                             where ao.Login_Username == username && ao.Login_Password == password
                             select ao.Id).FirstOrDefault();

                    if (wf_id == 0)
                    {
                        result = db.Addlogintoaccountsp(username, password, Convert.ToInt32(id), Convert.ToInt32(accounttype), Convert.ToInt32(providerid), macaddress).ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Methods.cs", "linkToAccountSP", ex.Message, (ex.StackTrace ?? "") + "\nInnerException: " + Newtonsoft.Json.JsonConvert.SerializeObject((ex.InnerException ?? new Exception(""))), Newtonsoft.Json.JsonConvert.SerializeObject(new { username = username, password = password, id = id, accounttype = accounttype, providerid = providerid, macaddress = macaddress }));
            }

            return result;
        }

        public static int radSessionId()
        {
            using (var db = new SecureRadiusdb())
            {
                return (int)db.radsessionid();
            }
        }

        public static List<Voucher> getVoucherUserName(string username)
        {
            using (var db = new RadiusdbDB())
            {
                List<Voucher> strSQL = (from c in db.Vouchers
                                        where c.Username == username
                                        select c).ToList();

                return strSQL;
            }
        }

        public static List<ServiceProvider> getServiceProviders()
        {
            try
            {
                using (var db = new RadiusdbDB())
                {
                    var strSQL = (from r in db.Roaming_Partners
                                  where r.List_Landingpage.ToString() == "Y"
                                  orderby r.List_Landingpage
                                  select new ServiceProvider
                                  {
                                      Id = r.Id,
                                      Code = r.Code,
                                      Description = r.Description,
                                      List = r.List,
                                      Unit_Cost = (int)r.Unit_Cost,
                                      Currency = r.Currency,
                                      Hotspot_List = r.Hotspot_List,
                                      Summary_List = r.Summary_List,
                                      Unit_Type = r.Unit_Type,
                                      Is_Debtor_Code = r.Is_Debtor_Code,
                                      Vat_Number = r.Vat_Number,
                                      Postal_Addr1 = r.Postal_Addr1,
                                      Postal_Addr2 = r.Postal_Addr2,
                                      Postal_City = r.Postal_City,
                                      Postal_Code = r.Postal_Code,
                                      Contact_Name = r.Contact_Name,
                                      Contact_Telno = r.Contact_Telno,
                                      Contact_Fax = r.Contact_Fax,
                                      Contact_Email = r.Contact_Email,
                                      List_Landingpage = r.List_Landingpage,
                                      Landingpage_Desc = r.Landingpage_Desc,
                                      Routing_Prefix = r.Routing_Prefix,
                                      Hosted_Uam_Url = r.Hosted_Uam_Url,
                                      Hosted_Uam = r.Hosted_Uam,
                                      List_Order = (int)r.List_Order,
                                      Per_User_Charge = r.Per_User_Charge,
                                      DropDownValue = r.Hosted_Uam_Url != "" ? r.Id + "," + r.Hosted_Uam_Url : r.Id.ToString()
                                  }).ToList();

                    return strSQL;
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Methods.cs", "getServiceProviders", ex.Message, (ex.StackTrace ?? "") + "\nInnerException: " + Newtonsoft.Json.JsonConvert.SerializeObject((ex.InnerException ?? new Exception(""))), Newtonsoft.Json.JsonConvert.SerializeObject(new { }));

                return new List<ServiceProvider>();
            }
        }

        public static SecureRadCheck getSecureRadCheckVariable(string username, string password)
        {
            try
            {
                using (var db = new SecureRadiusdb())
                {
                    SecureRadCheck result = (from c in db.radchecks
                                             where c.username == username
                                             && c.attribute == "Password"
                                             && c.value == password
                                             select new SecureRadCheck
                                             {
                                                 Username = c.username,
                                                 Value = c.value,
                                                 Id = c.id,
                                                 Op = c.op,
                                                 Attribute = c.attribute
                                             }).FirstOrDefault();

                    return result;
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Methods.cs", "getSecureRadCheckVariable", ex.Message, (ex.StackTrace ?? "") + "\nInnerException: " + Newtonsoft.Json.JsonConvert.SerializeObject((ex.InnerException ?? new Exception(""))), Newtonsoft.Json.JsonConvert.SerializeObject(new { username = username, password = password }));
                return null;
            }
        }

        public static void logPurchaseAction(string hotspot_id, string macaddress, string ip_address, string strName, string strEmail, string strCardNum, string strAmount, string strPackageId, string strError, string strDescription, string strURL, string strEnrolled, string strCompleted, string strTransId, string strPares, string strMerchantRef)
        {
            using (var db = new RadiusdbDB())
            {
                db.Insertccpurchaselog(hotspot_id, macaddress, ip_address, strName, strEmail, strCardNum, strAmount, strPackageId, strURL, strEnrolled, strTransId, "", strCompleted, strError, strDescription, strMerchantRef);
            }
        }

        public static void updatePurchaseAction(string hotspot_id, string macaddress, string ip_address, string strError, string strDescription, string strCompleted, string strTransId, string strMerchantRef)
        {
            using (var db = new RadiusdbDB())
            {
                db.Updateccpurchaselog(hotspot_id, macaddress, ip_address, strTransId, strCompleted, strError, strDescription, strMerchantRef, ccPurchaseIDInsertUpdate(strMerchantRef));
            }
        }

        public static string ccPurchaseIDInsertUpdate(string merchant_ref)
        {
            using (var db = new RadiusdbDB())
            {
                try
                {
                    var ccpl = db.Creditcard_Purchase_Logs.Where(n => n.Merchant_Ref == merchant_ref).FirstOrDefault();

                    return ccpl != null ? "update" : "insert";
                }
                catch (Exception ex)
                {
                    var ExceptionMessage = ex.Message;

                    return "insert";
                }
            }
        }

        public static TokenReturn getTokenDetail(string user_id)
        {
            try
            {
                using (var db = new RadiusdbDB())
                {
                    var token = db.ExecuteQuery<Token>(@"SELECT CC.username AS ccname,
                                                                CC.email,
                                                                CC.package AS last_package,
                                                                CC.display_price AS last_price,
                                                                CC.trx_timestamp AS cctimestamp,
                                                                CC.transaction_index,
                                                                CC.cc_number,
                                                                CC.expiry_date AS expiration
												         FROM ccacct_log AS CC
												         JOIN hotspot AS H on CC.hotspotid = H.id
												         JOIN radcheck AS R on CC.radiusid = R.id
												         JOIN wifi_credentials AS WC on R.username = WC.login_username
												         JOIN ao_account AS AO on WC.ao_account_id = AO.id
												         WHERE AO.id = '" + user_id + @"'
                                                         AND CC.trx_timestamp > (now() - interval '6 months')
                                                         AND CC.transaction_type = 'Debit'
                                                         ORDER BY trx_timestamp DESC
                                                         LIMIT 1").FirstOrDefault();

                    if (token != null)
                    {
                        return new TokenReturn()
                        {
                            TokenSuccess = true,
                            ErrorMessage = "",
                            ccname = token.ccname,
                            email = token.email,
                            last_package = token.last_package,
                            last_price = token.last_price,
                            cctimestamp = token.cctimestamp,
                            transaction_index = token.transaction_index,
                            cc_number = token.cc_number,
                            expiration = token.expiration
                        };
                    }
                    else
                    {
                        return new TokenReturn() { TokenSuccess = false, ErrorMessage = "No valid/usable token" };
                    }

                }
            }
            catch (Exception e)
            {
                Logger.Log("Methods.cs", "getTokenDetail", e.Message, (e.StackTrace ?? "") + "\nInnerException: " + Newtonsoft.Json.JsonConvert.SerializeObject((e.InnerException ?? new Exception(""))), Newtonsoft.Json.JsonConvert.SerializeObject(new { user_id = user_id }));

                return new TokenReturn() { TokenSuccess = false, ErrorMessage = "Error: " + e.Message };
            }
        }

        public static PaymentReturn ProceedTransaction(string hotspot_id, string macaddress, string ip_address, Packageitems package, string CardName, string CardEmail, string CardNumber, string CardExpMonth, string CardExpYear, string CVV, string CellNumber, string CellCountryCode, string cavv, string xid, string MerchantReference, bool ThreeDSecure)
        {
            try
            {
                iVeriInterface.DebitResponse dr;

                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => { return true; };

                //Use iveri
                using (var ivi = new iVeriInterface.iVeriInterface())
                {
                    dr = ivi.ProccessTransaction(System.Configuration.ConfigurationManager.AppSettings["Authorization_Code"], //strAuthCode
                                                 Boolean.Parse(System.Configuration.ConfigurationManager.AppSettings["test_mode"]), //blTestMode
                                                 CardExpMonth, //strExpiryMonth
                                                 CardExpYear, //strExpiryYear
                                                 CardNumber.Trim(), //strCardNum
                                                 CVV, //strCVV
                                                 CardName, //strCCName
                                                 staticFunctions.getCellNumber(CellNumber, CellCountryCode), //strCellNo
                                                 CellCountryCode, //strCountryCode
                                                 CardEmail, //strCCEmail
                                                 hotspot_id, //strHotspotId
                                                 "60", //strHotspotGroupId
                                                 package.id, //strPackageId
                                                 "0", //strUserId
                                                 "14784", //strAreaId
                                                 ip_address, //strIPAddress
                                                 macaddress, //strMAC
                                                 "ALWS", //strTerminal
                                                 false, //blRecharge
                                                 "", //strUsername
                                                 "", //strPassword
                                                 MerchantReference, //strMerchantReference
                                                 false, //blFromMobileDevice
                                                 cavv, //cavv
                                                 xid,
                                                 ThreeDSecure); //xid
                }

                // if successful, generate username and password, redirect to details page
                // else display error
                if (dr.blSuccessful)
                {
                    string packagetype = "";

                    paymentSelects details = getPackages(package.id);

                    if (details.groupname != null || details.groupname != "")
                    {
                        addUserGroup(dr.strUsername, details.groupname);

                        switch (details.package_type.ToString().Trim())
                        {
                            case "T": { packagetype = "4"; break; }
                            case "D":
                            case "DT":
                            case "DTS": { packagetype = "5"; break; }
                        }
                    }

                    updatePurchaseAction(hotspot_id, macaddress, ip_address, "", "Transaction was successful.", "Y", dr.strTransactionIndex, MerchantReference);

                    var expiration = "";

                    if (package.expirationDays != null && package.expirationDays != "0")
                    {
                        expiration = package.expirationDays + " Days";
                    }
                    if (package.expirationHours != null && package.expirationHours != "0" && expiration == "")
                    {
                        expiration = package.expirationHours + " Hours";
                    }

                    return new PaymentReturn()
                    {
                        PaymentSuccess = true,
                        ErrorMessage = "",
                        Username = dr.strUsername,
                        Password = dr.strPassword,
                        Package_Type = packagetype,
                        Package_Bought = package.packageDesc.Remove(package.packageDesc.IndexOf(" - ")),
                        Package_Bought_Expiration = expiration
                    };
                }
                else
                {
                    //lblCreditError.Visible = true;
                    if (dr.error != null)
                    {
                        updatePurchaseAction(hotspot_id, macaddress, ip_address, dr.error.strErrorDescription, "Transaction Failed during proccessing.", "F", dr.strTransactionIndex, MerchantReference);

                        return new PaymentReturn()
                        {
                            PaymentSuccess = false,
                            ErrorMessage = dr.error.strErrorDescription
                        };
                    }
                    else
                    {
                        updatePurchaseAction(hotspot_id, macaddress, ip_address, "Unknown error. No error was returned.", "Transaction Failed during proccessing.", "F", dr.strTransactionIndex, MerchantReference);

                        return new PaymentReturn()
                        {
                            PaymentSuccess = false,
                            ErrorMessage = "Due to unexpected circumstances we were unable to process your request. For further assistance, please contact the HELPDESK on 0861 HOTSPOT (0861 468 7768)."
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Methods.cs", "ProceedTransaction", ex.Message, (ex.StackTrace ?? "") + "\nInnerException: " + Newtonsoft.Json.JsonConvert.SerializeObject((ex.InnerException ?? new Exception(""))), Newtonsoft.Json.JsonConvert.SerializeObject(new { hotspot_id = hotspot_id, macaddress = macaddress, ip_address = ip_address, package = package, CardName = CardName, CardEmail = CardEmail, CardNumber = CardNumber, CardExpMonth = CardExpMonth, CardExpYear = CardExpYear, CVV = CVV, CellNumber = CellNumber, CellCountryCode = CellCountryCode, cavv = cavv, xid = xid, MerchantReference = MerchantReference, ThreeDSecure = ThreeDSecure }));

                return new PaymentReturn()
                {
                    PaymentSuccess = false,
                    ErrorMessage = "Due to unexpected circumstances we were unable to process your request. For further assistance, please contact the HELPDESK on 0861 HOTSPOT (0861 468 7768)."
                };
            }
        }

        private static paymentSelects getPackages(string packageid)
        {
            using (var db = new RadiusdbDB())
            {
                paymentSelects details = (from ap in db.Access_Packages
                                          join rgc in db.Radgroupchecks
                                          on ap.Radgroupcheck_Id equals rgc.Id
                                          where ap.Id.ToString() == packageid
                                          select new paymentSelects { groupname = rgc.Groupname, package_type = ap.Package_Type }).FirstOrDefault();


                return details;
            }
        }

        private static void addUserGroup(string username, string usergroup)
        {
            using (var db = new RadiusdbDB())
            {
                db.Addusergroup(username, usergroup);
            }
        }

        public static UsageSummary getPackageUsageSummary(string username)
        {
            try
            {
                var summary = new UsageSummary()
                {
                    dblTotalValue = 0.0,
                    dblTotalValueUsed = 0.0,
                    dblBalance = 0.0,
                    AdditionalText = "",
                    Allowance = "Unlimited",
                    Type = "Unlimited",
                    Balance = "Not Applicable",
                    Expiration = "",
                    TotalUsage = "",
                    Username = username
                };

                var cp = (getPackageGroup(username) != null) ? getPackageDetailsGroupAccount(username) : getPackageDetailsNotGroupAccount(username, true);
                if (cp != null)
                {
                    summary = new UsageSummary()
                    {
                        dblTotalValue = cp.dblTotalValue,
                        dblTotalValueUsed = cp.dblTotalValueUsed,
                        dblBalance = cp.dblBalance,
                        AdditionalText = cp.strAdditionalText,
                        Allowance = cp.lblAccountAllowance,
                        Type = cp.lblAccountType,
                        Balance = cp.lblAccountBalance,
                        Expiration = cp.lblAccountExpiration,
                        TotalUsage = cp.lblAccountTotalUsage,
                        Username = username
                    };
                }

                summary.Expiration = getRadcheckValue(username) ?? "Never";

                return summary;
            }
            catch (Exception ex)
            {
                Logger.Log("Methods.cs", "getPackageUsageSummary", ex.Message, (ex.StackTrace ?? "") + "\nInnerException: " + Newtonsoft.Json.JsonConvert.SerializeObject((ex.InnerException ?? new Exception(""))), Newtonsoft.Json.JsonConvert.SerializeObject(new { username = username }));

                return new UsageSummary();
            }
        }

        public static List<UsageDetail> getPackageUsageDetails(string username, string fromDate, string toDate)
        {
            var FromDateTime = DateTime.Now.AddMonths(-1);
            var ToDateTime = DateTime.Now;
            try
            {
                FromDateTime = fromDate != null && fromDate.Length > 0 ? Convert.ToDateTime(fromDate) : FromDateTime;
                ToDateTime = toDate != null && toDate.Length > 0 ? Convert.ToDateTime(toDate) : ToDateTime;
            }
            catch (Exception ex)
            {
                Logger.Log("Methods.cs", "getPackageUsageDetails", "Converting Dates ex: " + ex.Message, (ex.StackTrace ?? "") + "\nInnerException: " + Newtonsoft.Json.JsonConvert.SerializeObject((ex.InnerException ?? new Exception(""))), Newtonsoft.Json.JsonConvert.SerializeObject(new { username = username, fromDate = fromDate, toDate = toDate }));
            }

            using (var db = new RadiusdbDB())
            {
                return db.Usage_Detail_Group_Always_News
                         .Where(n => n.Username == username
                                  && n.Startime == FromDateTime
                                  && n.Endtime <= ToDateTime.AddDays(1))
                         .ToList()
                         .Select(n => new UsageDetail
                         {
                             username = n.Username,
                             timeused = Convert.ToDecimal(n.Timeused),
                             startime = n.Startime ?? Convert.ToDateTime("1900-01-01 00:00:00.000"),
                             endtime = n.Endtime ?? Convert.ToDateTime("1900-01-01 00:00:00.000"),
                             inputkb = n.Inputkb.ToString(),
                             outputkb = n.Outputkb.ToString(),
                             totalkbsec = Convert.ToDecimal(n.Totalkbsec),
                             areadescription = n.Areadescription,
                             ipaddr = n.Ipaddr,
                             mac = n.Mac
                         })
                         .OrderBy(n => n.startime)
                         .ToList();
            }
        }

        public static string getRadcheckValue(string username)
        {
            using (var db = new RadiusdbDB())
            {
                return db.Radchecks.Where(n => n.Username == username && n.Attribute == "Expiration").Select(n => n.Value).FirstOrDefault();
            }
        }

        public static string ForgotPassword(string email, string mobile)
        {
            var returnString = "";
            using (var db = new RadiusdbDB())
            {
                var account = db.Ao_Accounts
                                .Where(n => n.Email_Enc.ToLower() == email.Trim().ToLower()
                                            || n.Mobile_Number.ToLower() == mobile.Trim().ToLower())
                                .Select(n => new
                                {
                                    n.Id,
                                    n.Login_Credential,
                                    n.Email_Enc,
                                    n.Mobile_Number,
                                    n.Password_Enc
                                })
                                .FirstOrDefault();

                var pass = EncryptDecrypt.strDecrypt(account.Password_Enc, "shaunencliff4eva", "shaunencliff4eva");

                try
                {
                    Methods.AddMobileUserAction(account.Id.ToString(), "9");

                    var body = string.Format(@"You have forgotten your password<br />
                                               <br />
                                               Login details:<br />
                                               Username: <b>{0}</b><br />
                                               Password: <b>{1}</b><br />
                                               <br />
                                               Should you require any assistance you can call us on our 24 hour helpdesk by calling 0861 HOTSPOT (0861 468 7768)<br />
                                               <br />", account.Login_Credential, pass);

                    returnString += SendEmail(account.Email_Enc, "AlwaysOn Forgot Password", body);
                }
                catch (Exception ex)
                {
                    Logger.Log("Methods.cs", "ForgotPassword", "Sending Email ex: " + ex.Message, (ex.StackTrace ?? "") + "\nInnerException: " + Newtonsoft.Json.JsonConvert.SerializeObject((ex.InnerException ?? new Exception(""))), Newtonsoft.Json.JsonConvert.SerializeObject(new { email = email, mobile = mobile }));

                    returnString += "There was an error sending the email. ";
                }

                try
                {
                    if (!string.IsNullOrEmpty(account.Mobile_Number))
                    {
                        var body = "AlwaysOn Login:\r\n" +
                                    "Username: " + account.Login_Credential + "\r\n" +
                                    "Password: " + pass + "\r\n" +
                                    "For any assistance call us on 0861 HOTSPOT (0861 468 7768)";

                        returnString += SendSMS(account.Mobile_Number, body);

                        returnString += "SMS sent Successfully. ";
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log("Methods.cs", "ForgotPassword", "Sending SMS ex: " + ex.Message, (ex.StackTrace ?? "") + "\nInnerException: " + Newtonsoft.Json.JsonConvert.SerializeObject((ex.InnerException ?? new Exception(""))), Newtonsoft.Json.JsonConvert.SerializeObject(new { email = email, mobile = mobile }));

                    returnString += "There was an error sending the sms. ";
                }
            }

            return returnString;
        }

        public static string SendEmail(string email, string subject, string body)
        {
            try
            {
                new SmtpClient(System.Configuration.ConfigurationManager.AppSettings["MailServer"])
                    .Send(new MailMessage()
                    {
                        From = new MailAddress("hotspots@alwayson.co.za"),
                        Subject = subject,
                        To = { email },
                        BodyEncoding = System.Text.Encoding.UTF8,
                        IsBodyHtml = true,
                        Body = body
                    });

                return "Email sent Successfully. ";
            }
            catch (Exception ex)
            {
                Logger.Log("Methods.cs", "SendEmail", ex.Message, (ex.StackTrace ?? "") + "\nInnerException: " + Newtonsoft.Json.JsonConvert.SerializeObject((ex.InnerException ?? new Exception(""))), Newtonsoft.Json.JsonConvert.SerializeObject(new { email = email, subject = subject, body = body }));

                return "There was an error sending the email. ";
            }
        }

        public static string SendSMS(string mobile, string body)
        {
            try
            {
                new SmtpClient(System.Configuration.ConfigurationManager.AppSettings["MailServer"])
                    .Send(new System.Net.Mail.MailMessage(new MailAddress("monitor@alwayson.co.za"), new MailAddress(mobile + "@sms.vine.co.za"))
                    {
                        BodyEncoding = System.Text.Encoding.UTF8,
                        Subject = "R646NJKGA",
                        IsBodyHtml = false,
                        Body = body
                    });

                return "SMS sent Successfully. ";
            }
            catch (Exception ex)
            {
                Logger.Log("Methods.cs", "SendSMS", ex.Message, (ex.StackTrace ?? "") + "\nInnerException: " + Newtonsoft.Json.JsonConvert.SerializeObject((ex.InnerException ?? new Exception(""))), Newtonsoft.Json.JsonConvert.SerializeObject(new { mobile = mobile, body = body }));

                return "There was an error sending the SMS. ";
            }
        }

        public static bool is_superwifi(string IP_address)
        {
            using (var db = new RadiusdbDB())
            {
                string strAreaIP = IP_address;
                strAreaIP = strAreaIP.Substring(0, strAreaIP.LastIndexOf("."));

                var superwifi = (from ha in db.Hotspot_Areas
                                 join h in db.Hotspots on ha.Hotspot_Id equals h.Id
                                 where ha.Areacode == strAreaIP
                                 && h.Live == "Y" && new[] { 11, 12, 13, 14, 18 }.Contains(h.Backhaul_Type_Id)
                                 select new { ha.Hotspot_Id, h.Live }).FirstOrDefault();

                if (superwifi != null)
                { return true; }
                else
                { return true; }
            }
        }

        public static string getpackageData(string id)
        {
            string result = "";
            using (var db = new RadiusdbDB())
            {
                try
                {
                    result = (from p in db.Radgroupchecks
                              where p.Id.ToString() == id
                              && p.Op == ":="
                              select p.Value).FirstOrDefault().ToString();
                }
                catch (Exception ex)
                {
                    var ExceptionMessage = ex.Message;

                    result = "";
                }

                return result;
            }
        }

        public static string FirstCharToUpper(string input)
        {
            if (String.IsNullOrEmpty(input)) input = "";
            return input.First().ToString().ToUpper() + input.Substring(1);
        }

        /// <summary>
        /// This will add an action to the app_user table and save historic data per user
        /// <para/>1	RegisterUser
        /// <para/>2	getUserProfile
        /// <para/>3	updateUserProfile
        /// <para/>4	getUserPackages
        /// <para/>5	linkDevice
        /// <para/>6	unlinkDevice
        /// <para/>7	link_existing_credentials
        /// <para/>8	unlink_existing_credentials
        /// <para/>9	forgotPassword
        /// <para/>10	SecureDirectPayment
        /// <para/>11	updatePackageRanking
        /// </summary>
        /// <param name="ao_account_id">ao_account_id</param>
        /// <param name="app_action_id">app_action_id</param>
        /// <example>
        /// "select * from app_action" to see all valid action ids
        /// </example>
        /// <returns>
        /// Success or Failed bool
        /// </returns>
        public static void AddMobileUserAction(string strAoAccountId, string strAppActionId)
        {
            //try
            //{
            //    System.Threading.Tasks.Task.Factory.StartNew(() =>
            //    {
            try
            {
                int ao_account_id = Convert.ToInt32(strAoAccountId);
                int app_action_id = Convert.ToInt32(strAppActionId);

                if (ao_account_id > 0 && app_action_id > 0)
                {
                    using (var db = new RadiusdbDB())
                    {
                        db.Add_App_Useraction(ao_account_id, app_action_id);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Methods.cs", "AddMobileUserAction", ex.Message, (ex.StackTrace ?? "") + "\nInnerException: " + Newtonsoft.Json.JsonConvert.SerializeObject((ex.InnerException ?? new Exception(""))), Newtonsoft.Json.JsonConvert.SerializeObject(new { strAoAccountId = strAoAccountId, strAppActionId = strAppActionId }));
            }
            //    }, System.Threading.Tasks.TaskCreationOptions.LongRunning);
            //}
            //catch { }
        }

        public static string ValueByteFormat(double Value)
        {
            if (Value < 1000.0)
                return String.Format("{0:F2}", Value) + " B";
            else if (Value < 1000000.0)
                return String.Format("{0:F2}", Value / 1000) + " KB";
            else if (Value < 1000000000.0)
                return String.Format("{0:F2}", Value / 1000000) + " MB";
            else if (Value < 1000000000000.0)
                return String.Format("{0:F2}", Value / 1000000000) + " GB";
            else if (Value < 1000000000000000.0)
                return String.Format("{0:F2}", Value / 1000000000000) + " TB";
            else if (Value < 1000000000000000000.0)
                return String.Format("{0:F2}", Value / 1000000000000000) + " PB";
            else
                return String.Format("{0:F2}", Value / 1000000000000000000) + " EB";
        }

        public static List<RadiusdbContext.Radcheck> GetUsernameRadcheck(string Username)
        {
            using (var db = new RadiusdbDB())
            {
                return db.Radchecks.Where(n => n.Username == Username).Select(n => n).ToList();
            }
        }

        public static PackageGroup GetPackageGroup(string Username)
        {
            var InvalidAttributes = new string[] { "Client-IP-Address", "Called-Station-Id", "NAS-IP-Address" }.ToList();

            using (var db = new RadiusdbDB())
            {
                return db.ExecuteQuery<PackageGroup>(@"SELECT D.attribute,
		                                                      D.value
                                                       FROM usergroup AS C
                                                       JOIN radgroupcheck AS D ON C.groupname = D.groupname
                                                       WHERE C.username = '" + Username + @"'
                                                       AND D.attribute NOT IN ('" + string.Join("','", InvalidAttributes) + "')").FirstOrDefault();
            }
        }

        public static oPackageDetail getPackageUsageDetails(string Username, bool blAllowToZero)
        {
            using (var cpd = new oPackageDetail())
            {
                cpd.dblTotalValue = 0.0;
                cpd.lblAccountType = "";
                cpd.lblAccountAllowance = "";
                cpd.boolData = false;
                cpd.strAdditionalText = "";
                cpd.dblTotalValueUsed = 0.0;
                cpd.dblPercentageLeft = 100;
                cpd.dblBalance = 0.0;
                cpd.lblAccountTotalUsage = "";
                cpd.lblAccountBalance = "";
                cpd.PackageTotal = 0;
                cpd.PackageDataTotal = 0;
                cpd.strUsedUp = "";
                cpd.strExpiration = "";

                var pg = GetPackageGroup(Username);
                if (pg != null)
                {
                    cpd.dblTotalValue = Convert.ToDouble(pg.Value);

                    var packageUsage = new List<Usage_Detail_Group_Always_New>();

                    using (var db = new RadiusdbDB())
                    {
                        packageUsage = db.Usage_Detail_Group_Always_News.Where(n => n.Username == Username).ToList();
                    }

                    switch (pg.Attribute)
                    {
                        default:
                            {
                                var pUse = (from c in packageUsage
                                            group c by c.Username into g
                                            select new
                                            {
                                                Total = (decimal)g.Sum(x => x.Timeused),
                                                DataTotal = ((decimal)g.Sum(x => x.Inputkb) + (decimal)g.Sum(x => x.Outputkb))
                                            }).FirstOrDefault();

                                if (pUse != null)
                                {
                                    cpd.PackageTotal = pUse.Total;
                                    cpd.PackageDataTotal = pUse.DataTotal;
                                }

                                break;
                            }
                        case "Max-All-Session":
                            {
                                cpd.lblAccountType = "Time Limited Account";
                                cpd.lblAccountAllowance = String.Format("{0:F2}", (cpd.dblTotalValue / 60)) + " Minutes";

                                var pUse = (from c in packageUsage
                                            group c by c.Username into g
                                            select new
                                            {
                                                Total = (decimal)g.Sum(x => x.Timeused)
                                            }).FirstOrDefault();

                                if (pUse != null)
                                {
                                    cpd.PackageTotal = pUse.Total;
                                }

                                cpd.boolData = false;
                                break;
                            }
                        case "Max-Daily-Session":
                            {
                                cpd.lblAccountType = "Daily Time Limited Account";
                                cpd.lblAccountAllowance = String.Format("{0:F2}", (cpd.dblTotalValue / 60)) + " Minutes per day";
                                cpd.strAdditionalText = " today";

                                var fpUse = packageUsage.Where(a => ConvertFromDateTimeOffset((DateTimeOffset)a.Startime).ToString("yyyy/MM/dd") == DateTime.Now.ToString("yyyy/MM/dd")).ToList();
                                var pUse = (from c in fpUse
                                            group c by c.Username into g
                                            select new
                                            {
                                                Total = (decimal)g.Sum(x => x.Timeused)
                                            }).FirstOrDefault();

                                if (pUse != null)
                                {
                                    cpd.PackageTotal = pUse.Total;
                                }

                                cpd.boolData = false;
                                break;
                            }
                        case "Max-Recv-Limit":
                            {
                                cpd.lblAccountType = "Data Limited Account";
                                cpd.lblAccountAllowance = ValueByteFormat(cpd.dblTotalValue);

                                var pUse = (from c in packageUsage
                                            group c by c.Username into g
                                            select new
                                            {
                                                Total = ((decimal)g.Sum(x => x.Inputkb) + (decimal)g.Sum(x => x.Outputkb))
                                            }).FirstOrDefault();

                                if (pUse != null)
                                {
                                    cpd.PackageTotal = pUse.Total;
                                }

                                cpd.boolData = true;
                                break;
                            }
                        case "Max-Daily-Recv-Limit":
                            {
                                cpd.lblAccountType = "Daily Data Limited Account";
                                cpd.lblAccountAllowance = ValueByteFormat(cpd.dblTotalValue) + " per day";
                                cpd.strAdditionalText = " today";

                                var fpUse = packageUsage.Where(n => ConvertFromDateTimeOffset((DateTimeOffset)n.Startime).ToString("yyyy/MM/dd") == DateTime.Now.ToString("yyyy/MM/dd")).ToList();
                                var pUse = (from c in fpUse
                                            group c by c.Username into g
                                            select new
                                            {
                                                Total = ((decimal)g.Sum(x => x.Inputkb) + (decimal)g.Sum(x => x.Outputkb))
                                            }).FirstOrDefault();

                                if (pUse != null)
                                {
                                    cpd.PackageTotal = pUse.Total;
                                }

                                cpd.boolData = true;
                                break;
                            }
                        case "Max-Monthly-Recv-Limit":
                            {
                                cpd.lblAccountType = "Monthly Data Limited Account";
                                cpd.lblAccountAllowance = ValueByteFormat(cpd.dblTotalValue) + " per month";
                                cpd.strAdditionalText = " this month";

                                var fpUse = packageUsage.Where(n => ConvertFromDateTimeOffset((DateTimeOffset)n.Startime).ToString("yyyy/MM") == DateTime.Now.ToString("yyyy/MM")).ToList();
                                var pUse = (from c in fpUse
                                            group c by c.Username into g
                                            select new
                                            {
                                                Total = ((decimal)g.Sum(x => x.Inputkb) + (decimal)g.Sum(x => x.Outputkb))
                                            }).FirstOrDefault();

                                if (pUse != null)
                                {
                                    cpd.PackageTotal = pUse.Total;
                                }

                                cpd.boolData = true;
                                break;
                            }
                    }

                    foreach (var rc in GetUsernameRadcheck(Username))
                    {
                        switch (rc.Attribute)
                        {
                            case "Expiration":
                                {
                                    DateTime? ExpiredDateTime = null;
                                    try
                                    {
                                        ExpiredDateTime = Convert.ToDateTime(rc.Value ?? DateTime.Now.AddDays(1).ToString());
                                    }
                                    catch { }

                                    var packageValid = ExpiredDateTime == null || ExpiredDateTime >= DateTime.Now;

                                    cpd.strExpiration = !packageValid ? "Your username expired on " + (rc.Value ?? "") : "";

                                    cpd.RadcheckUserExpiration = rc.Value;

                                    break;
                                }
                            case "Expire-After":
                                {
                                    cpd.iDaysExpireAfter = Convert.ToInt32(string.IsNullOrEmpty(rc.Value) ? "0" : rc.Value) / 60 / 60 / 24;

                                    break;
                                }
                        }
                    }

                    cpd.dblTotalValueUsed = Convert.ToDouble(cpd.PackageTotal);

                    if (string.IsNullOrEmpty(cpd.strExpiration))
                    {
                        if (!cpd.boolData)
                        {
                            cpd.dblBalance = (cpd.dblTotalValue / 60) - cpd.dblTotalValueUsed;
                            cpd.lblAccountTotalUsage = String.Format("{0:F2}", cpd.dblTotalValueUsed) + " Minutes" + cpd.strAdditionalText;
                            cpd.lblAccountBalance = String.Format("{0:F2}", cpd.dblBalance) + " Minutes" + cpd.strAdditionalText;

                            cpd.dblPercentageLeft = (cpd.dblBalance / (cpd.dblTotalValue / 60)) * 100;

                            cpd.strUsedUp = (blAllowToZero ? cpd.dblBalance <= 0 : cpd.dblBalance < 1) ? "The time allowance on this package is used up." : "";
                        }
                        else
                        {
                            cpd.dblBalance = (cpd.dblTotalValue / 1000) - cpd.dblTotalValueUsed;
                            cpd.lblAccountTotalUsage = ValueByteFormat(cpd.dblTotalValueUsed * 1000) + cpd.strAdditionalText;
                            cpd.lblAccountBalance = ValueByteFormat(cpd.dblBalance * 1000) + cpd.strAdditionalText;

                            cpd.dblPercentageLeft = (cpd.dblBalance / (cpd.dblBalance + cpd.dblTotalValueUsed)) * 100;

                            cpd.strUsedUp = (blAllowToZero ? cpd.dblBalance <= 0 : cpd.dblBalance < 1000.0) ? "The data allowance on this package is used up." : "";
                        }
                    }
                    else
                    {
                        cpd.dblPercentageLeft = 0;
                        cpd.strUsedUp = cpd.strExpiration;
                    }
                }
                else
                {
                    List<PackageGroup> lpg = null;

                    PackageValue strSQL = null;

                    CompletePackageDetail completepackage = new CompletePackageDetail();

                    using (var db = new RadiusdbDB())
                    {
                        var InvalidAttributes = new string[] { "Client-IP-Address", "Called-Station-Id", "NAS-IP-Address", "Password", "Simultaneous-Use", "Accuris" }.ToList();

                        lpg = db.Radchecks
                               .Where(d => d.Username == Username
                                        && !InvalidAttributes.Contains(d.Attribute))
                               .Select(n => new PackageGroup()
                               {
                                   Attribute = n.Attribute,
                                   Value = n.Value
                               })
                               .ToList();

                        strSQL = (from c in db.Checklimit_Views
                                  where c.Username == Username
                                  group c by c.Username into g
                                  select new PackageValue
                                  {
                                      Total = ((decimal)g.Sum(x => x.Totdata) / 1000)
                                  }).FirstOrDefault();
                    }

                    cpd.boolData = true;

                    if (lpg != null)
                    {
                        foreach (PackageGroup p in lpg)
                        {
                            cpd.dblTotalValue = Convert.ToDouble(p.Value);

                            switch (p.Attribute)
                            {
                                case "Expiration":
                                    {
                                        string strExpiredDateTime = null;
                                        DateTime? ExpiredDateTime = null;
                                        try
                                        {
                                            using (var db = new RadiusdbDB())
                                            {
                                                strExpiredDateTime = db.Radchecks.Where(n => n.Username == Username && n.Attribute == "Expiration").Select(n => n.Value).FirstOrDefault();
                                                ExpiredDateTime = Convert.ToDateTime(strExpiredDateTime ?? DateTime.Now.AddDays(1).ToString());
                                            }
                                        }
                                        catch { }

                                        var packageValid = ExpiredDateTime == null || ExpiredDateTime >= DateTime.Now;

                                        cpd.strExpiration = !packageValid ? "Your username expired on " + (strExpiredDateTime ?? "") : "";

                                        break;
                                    }
                                case "Max-All-Session":
                                    {
                                        cpd.lblAccountType = "Time Limited Account";
                                        cpd.lblAccountAllowance = String.Format("{0:F2}", (cpd.dblTotalValue / 60)) + " Minutes";

                                        using (var db = new RadiusdbDB())
                                        {
                                            strSQL = (from c in db.Usage_Histories
                                                      where c.Username == Username
                                                      group c by c.Username into g
                                                      select new PackageValue()
                                                      {
                                                          Total = (decimal)g.Sum(x => x.Timeused)
                                                      }).FirstOrDefault();

                                            if (strSQL != null)
                                            {
                                                cpd.PackageTotal = strSQL.Total;
                                            }
                                        }

                                        cpd.boolData = false;
                                        break;
                                    }
                                case "Max-Daily-Session":
                                    {
                                        cpd.lblAccountType = "Daily Time Limited Account";
                                        cpd.lblAccountAllowance = String.Format("{0:F2}", (cpd.dblTotalValue / 60)) + " Minutes per day";
                                        cpd.strAdditionalText = " today";

                                        using (var db = new RadiusdbDB())
                                        {
                                            var FullstrSQL = (from c in db.Usage_Histories
                                                              where c.Username == Username
                                                              && c.Usedate == DateTime.Now.ToString("yyyy/MM/dd").Replace("-", "/")
                                                              select new
                                                              {
                                                                  username = c.Username,
                                                                  usedate = c.Usedate,
                                                                  timeused = c.Timeused,
                                                                  inputkb = c.Inputkb,
                                                                  outputkb = c.Outputkb
                                                              })
                                                              .ToList();

                                            strSQL = (from c in FullstrSQL
                                                      group c by c.username into g
                                                      select new PackageValue()
                                                      {
                                                          Total = (decimal)g.Sum(x => x.timeused)
                                                      }).FirstOrDefault();

                                            if (strSQL != null)
                                            {
                                                cpd.PackageTotal = strSQL.Total;
                                            }
                                        }

                                        cpd.boolData = false;
                                        break;
                                    }
                                case "Max-Recv-Limit":
                                    {
                                        cpd.lblAccountType = "Data Limited Account";
                                        cpd.lblAccountAllowance = ValueByteFormat(cpd.dblTotalValue);

                                        using (var db = new RadiusdbDB())
                                        {
                                            strSQL = (from c in db.Checklimit_Views
                                                      where c.Username == Username
                                                      group c by c.Username into g
                                                      select new PackageValue()
                                                      {
                                                          Total = ((decimal)g.Sum(x => x.Totdata) / 1000)
                                                      }).FirstOrDefault();

                                            if (strSQL != null)
                                            {
                                                cpd.PackageTotal = strSQL.Total;
                                            }
                                        }

                                        cpd.boolData = true;
                                        break;
                                    }
                                case "Max-Daily-Recv-Limit":
                                    {
                                        cpd.lblAccountType = "Daily Data Limited Account";
                                        cpd.lblAccountAllowance = ValueByteFormat(cpd.dblTotalValue) + " per day";
                                        cpd.strAdditionalText = " today";

                                        using (var db = new RadiusdbDB())
                                        {
                                            var FullstrSQL = (from c in db.Usage_Histories
                                                              where c.Username == Username
                                                              && c.Usedate == DateTime.Now.ToString("yyyy/MM/dd").Replace("-", "/")
                                                              select new
                                                              {
                                                                  username = c.Username,
                                                                  usedate = c.Usedate,
                                                                  timeused = c.Timeused,
                                                                  inputkb = c.Inputkb,
                                                                  outputkb = c.Outputkb
                                                              })
                                                              .ToList();

                                            strSQL = (from c in FullstrSQL
                                                      group c by c.username into g
                                                      select new PackageValue()
                                                      {
                                                          Total = ((decimal)g.Sum(x => x.inputkb) + (decimal)g.Sum(x => x.outputkb))
                                                      }).FirstOrDefault();

                                            if (strSQL != null)
                                            {
                                                cpd.PackageTotal = strSQL.Total;
                                            }
                                        }

                                        cpd.boolData = true;
                                        break;
                                    }
                                case "Max-Monthly-Recv-Limit":
                                    {
                                        cpd.lblAccountType = "Monthly Data Limited Account";
                                        cpd.lblAccountAllowance = ValueByteFormat(cpd.dblTotalValue) + " per month";
                                        cpd.strAdditionalText = " this month";

                                        using (var db = new RadiusdbDB())
                                        {
                                            var FullstrSQL = (from c in db.Usage_Histories
                                                              where c.Username == Username
                                                              select new
                                                              {
                                                                  username = c.Username,
                                                                  usedate = c.Usedate,
                                                                  timeused = c.Timeused,
                                                                  inputkb = c.Inputkb,
                                                                  outputkb = c.Outputkb
                                                              })
                                                              .ToList()
                                                              .Where(a => DateTime.ParseExact(a.usedate, "yyyy/MM/dd", CultureInfo.InvariantCulture).ToString("yyyy/MM") == DateTime.Now.ToString("yyyy/MM"))
                                                              .ToList();

                                            strSQL = (from c in FullstrSQL
                                                      group c by c.username into g
                                                      select new PackageValue()
                                                      {
                                                          Total = ((decimal)g.Sum(x => x.inputkb) + (decimal)g.Sum(x => x.outputkb))
                                                      }).FirstOrDefault();

                                            if (strSQL != null)
                                            {
                                                cpd.PackageTotal = strSQL.Total;
                                            }
                                        }

                                        cpd.boolData = true;
                                        break;
                                    }
                            }

                            foreach (var rc in GetUsernameRadcheck(Username))
                            {
                                switch (rc.Attribute)
                                {
                                    case "Expiration":
                                        {
                                            DateTime? ExpiredDateTime = null;
                                            try
                                            {
                                                ExpiredDateTime = Convert.ToDateTime(rc.Value ?? DateTime.Now.AddDays(1).ToString());
                                            }
                                            catch { }

                                            var packageValid = ExpiredDateTime == null || ExpiredDateTime >= DateTime.Now;

                                            cpd.strExpiration = !packageValid ? "Your username expired on " + (rc.Value ?? "") : "";

                                            cpd.RadcheckUserExpiration = rc.Value;

                                            break;
                                        }
                                    case "Expire-After":
                                        {
                                            cpd.iDaysExpireAfter = Convert.ToInt32(string.IsNullOrEmpty(rc.Value) ? "0" : rc.Value) / 60 / 60 / 24;

                                            break;
                                        }
                                }
                            }

                            cpd.dblTotalValueUsed = Convert.ToDouble(cpd.PackageTotal);
                        }

                        if (string.IsNullOrEmpty(cpd.strExpiration))
                        {
                            if (!cpd.boolData)
                            {
                                cpd.dblBalance = (cpd.dblTotalValue / 60) - cpd.dblTotalValueUsed;
                                cpd.lblAccountTotalUsage = String.Format("{0:F2}", cpd.dblTotalValueUsed) + " Minutes" + cpd.strAdditionalText;
                                cpd.lblAccountBalance = String.Format("{0:F2}", cpd.dblBalance) + " Minutes" + cpd.strAdditionalText;

                                cpd.dblPercentageLeft = (cpd.dblBalance / (cpd.dblTotalValue / 60)) * 100;

                                cpd.strUsedUp = (blAllowToZero ? cpd.dblBalance <= 0 : cpd.dblBalance < 1) ? "The time allowance on this package is used up." : "";
                            }
                            else
                            {
                                cpd.dblBalance = (cpd.dblTotalValue / 1000) - cpd.dblTotalValueUsed;
                                cpd.lblAccountTotalUsage = ValueByteFormat(cpd.dblTotalValueUsed * 1000) + cpd.strAdditionalText;
                                cpd.lblAccountBalance = ValueByteFormat(cpd.dblBalance * 1000) + cpd.strAdditionalText;

                                cpd.dblPercentageLeft = (cpd.dblBalance / (cpd.dblBalance + cpd.dblTotalValueUsed)) * 100;

                                cpd.strUsedUp = (blAllowToZero ? cpd.dblBalance <= 0 : cpd.dblBalance < 1000.0) ? "The data allowance on this package is used up." : "";
                            }
                        }
                        else
                        {
                            cpd.dblPercentageLeft = 0;
                            cpd.strUsedUp = cpd.strExpiration;
                        }
                    }
                }

                return cpd;
            }
        }

        public static string EmailCensor(string email)
        {
            if (email == null) return null;
            if (string.IsNullOrWhiteSpace(email)) return "";
            if (!email.Contains('@')) return email;

            var firstPart = email.Substring(0, email.IndexOf('@'));
            if (firstPart.Length > 3) firstPart = firstPart.Substring(0, firstPart.Length - 3).PadRight(firstPart.Length, '*');

            var secondPart = email.Substring(email.IndexOf('@') + 1);
            if (secondPart.Contains('.'))
            {
                var thirdPart = secondPart.Substring(secondPart.IndexOf('.'));
                secondPart = secondPart.Replace(thirdPart, "");
                if (secondPart.Length > 3) secondPart = secondPart.Substring(3).PadLeft(secondPart.Length, '*');

                return firstPart + "@" + secondPart + thirdPart;
            }
            else
            {
                if (secondPart.Length > 3) secondPart = secondPart.Substring(3).PadLeft(secondPart.Length, '*');

                return firstPart + "@" + secondPart;
            }
        }
    }
}