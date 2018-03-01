using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Web;
using System.Web.Services;
using Hotspot.AppCode.tools;
using Hotspot.RadiusClient;
using Hotspot.tools;
using RadiusdbContext;
using SecureradiusdbContext;
using System.Text.RegularExpressions;
using System.Collections.Specialized;

namespace AlwaysOnMobileService
{

    public class AlwaysOnMobileService : IAlwaysOnMobileService
    {
        public const int HotspotIDAndroid = 2946;
        public const int HotspotIDiOS = 4053;
        public const string ReinstallApp = "Reinstall App from the store";
        public const string InvalidApiKey = "Please provide a valid api_key";

        //1 Register User
        public ReturnRegistration RegisterUser(string api_key, string name, string surname, string username, string password, string mobile)
        {
            int ihotspotid = CheckAPIKey(api_key);
            if (ihotspotid == -2)
            {
                return new ReturnRegistration() { Success = false, Message = ReinstallApp };
            }
            else if (ihotspotid == -1)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "RegisterUser", InvalidApiKey, "", Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key, name = name, surname = surname, username = username, password = password, mobile = mobile }));

                return new ReturnRegistration() { Success = false, Message = InvalidApiKey, UserProfile = null };
            }

            try
            {
                string id = string.Empty;
                string result = string.Empty;
                string registeredID = string.Empty;

                using (var db = new RadiusdbDB())
                {
                    if (db.Ao_Accounts.Where(n => n.Login_Credential == username.ToLower()).Select(n => n.Id).FirstOrDefault() > 0)
                    {
                        return new ReturnRegistration() { Success = false, Message = "Username already exists", UserProfile = null };
                    }

                    string PassEncrypt = EncryptDecrypt.strEncrypt(password.Trim(), "shaunencliff4eva", "shaunencliff4eva");

                    db.Addaoaccount(username, PassEncrypt, 1, 236, DateTime.Now, name, surname, 1, mobile, ihotspotid);

                    //details = db.Ao_Accounts.Where(n => n.Login_Credential == username.ToLower()).Select(n => n.Id).FirstOrDefault();

                    var UserProfile = (from ao in db.Ao_Accounts
                                       where ao.Login_Credential == username.ToLower()
                                       && ao.Password_Enc == PassEncrypt
                                       select new UserProfile
                                       {
                                           user_id = ao.Id.ToString(),
                                           name = ao.Name,
                                           surname = ao.Surname,
                                           title = ao.Title,
                                           mobile_number = ao.Mobile_Number,
                                           email_enc = ao.Email_Enc,
                                           country_id = ao.Country_Id.ToString(),
                                           date_created = ao.Date_Created.ToString(),
                                           login_credential = ao.Login_Credential,
                                           accountstatus_id = ao.Accountstatus_Id.ToString()
                                       }).FirstOrDefault();

                    if (UserProfile != null && Convert.ToInt32(UserProfile.user_id) > 0)
                    {
                        Methods.AddMobileUserAction(UserProfile.user_id, "1");
                        return new ReturnRegistration() { Success = true, Message = "Registration Successful", UserProfile = UserProfile };
                    }
                    else
                    {
                        return new ReturnRegistration() { Success = false, Message = "Registration Failure", UserProfile = null };
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "RegisterUser", e.Message, (e.StackTrace ?? "") + "\nInnerException: " + Newtonsoft.Json.JsonConvert.SerializeObject((e.InnerException ?? new Exception(""))), Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key, name = name, surname = surname, username = username, password = password, mobile = mobile }));

                return new ReturnRegistration() { Success = false, Message = "Error: " + e.Message, UserProfile = null };
            }
        }

        //2 Get User Profile
        public ReturnUserProfile getUserProfile(string api_key, string username, string password)
        {
            int ihotspotid = CheckAPIKey(api_key);
            if (ihotspotid == -2)
            {
                return new ReturnUserProfile() { Success = false, Message = ReinstallApp };
            }
            else if (ihotspotid == -1)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "getUserProfile", InvalidApiKey, "", Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key, username = username, password = password }));

                return new ReturnUserProfile() { Success = false, Message = InvalidApiKey, UserProfile = null };
            }

            try
            {
                string PassEncrypt = EncryptDecrypt.strEncrypt(password.Trim(), "shaunencliff4eva", "shaunencliff4eva");

                using (var db = new RadiusdbDB())
                {
                    var result = (from ao in db.Ao_Accounts
                                  where ao.Login_Credential == username.ToLower()
                                  && ao.Password_Enc == PassEncrypt
                                  select new UserProfile
                                  {
                                      user_id = ao.Id.ToString(),
                                      name = ao.Name,
                                      surname = ao.Surname,
                                      title = ao.Title,
                                      mobile_number = ao.Mobile_Number,
                                      email_enc = ao.Email_Enc,
                                      country_id = ao.Country_Id.ToString(),
                                      date_created = ao.Date_Created.ToString(),
                                      login_credential = ao.Login_Credential,
                                      accountstatus_id = ao.Accountstatus_Id.ToString()
                                  }).FirstOrDefault();

                    if (result != null)
                    {
                        Methods.AddMobileUserAction(result.user_id, "2");
                        return new ReturnUserProfile() { Success = true, Message = "", UserProfile = result };
                    }
                    else
                    {
                        var pres = (from rc in db.Radchecks
                                    where rc.Username == username
                                    && rc.Attribute == "Password"
                                    && rc.Value == password
                                    select rc).FirstOrDefault();

                        if (pres != null)
                        {
                            var linked = (from wc in db.Wifi_Credentials
                                          join ao in db.Ao_Accounts on wc.Ao_Account_Id equals ao.Id
                                          where wc.Login_Username == pres.Username
                                          select ao.Login_Credential).FirstOrDefault();

                            return new ReturnUserProfile() { Success = true, Message = "This is package credentials", UserProfile = null, IsPackageCredentials = true, IsPackageLinked = !string.IsNullOrWhiteSpace(linked), PackageLinkedAccount = Methods.EmailCensor(linked) };
                        }
                        else
                        {
                            return new ReturnUserProfile() { Success = false, Message = "Login failed", UserProfile = null };
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "getUserProfile", e.Message, (e.StackTrace ?? "") + "\nInnerException: " + Newtonsoft.Json.JsonConvert.SerializeObject((e.InnerException ?? new Exception(""))), Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key, username = username, password = password }));

                return new ReturnUserProfile() { Success = false, Message = "Error: " + e.Message, UserProfile = null };
            }
        }

        //3 Update User Profile
        public ReturnDefault updateUserProfile(string api_key, string user_id, string title, string name, string surname, string email, string mobile)
        {
            int ihotspotid = CheckAPIKey(api_key);
            if (ihotspotid == -2)
            {
                return new ReturnDefault() { Success = false, Message = ReinstallApp };
            }
            else if (ihotspotid == -1)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "updateUserProfile", InvalidApiKey, "", Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key, user_id = user_id, title = title, name = name, surname = surname, email = email, mobile = mobile }));

                return new ReturnDefault() { Success = false, Message = InvalidApiKey };
            }

            try
            {
                var success = false;
                using (var db = new RadiusdbDB())
                {
                    Methods.AddMobileUserAction(user_id, "3");
                    success = (int)db.Updateuserprofile(Convert.ToInt32(user_id), title, name, surname, email, mobile, 236) > 0;
                    success = success && (int)db.Updateusername(Convert.ToInt32(user_id), email) > 0;
                }

                return new ReturnDefault() { Success = success, Message = success ? "Update Successful" : "Update Failed" };
            }
            catch (Exception e)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "updateUserProfile", e.Message, (e.StackTrace ?? "") + "\nInnerException: " + Newtonsoft.Json.JsonConvert.SerializeObject((e.InnerException ?? new Exception(""))), Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key, user_id = user_id, title = title, name = name, surname = surname, email = email, mobile = mobile }));

                return new ReturnDefault() { Success = false, Message = "Error: " + e.Message };
            }
        }

        //4 Get User Packages
        public ReturnUserPackages getUserPackages(string api_key, string user_id, string macaddress)
        {
            int ihotspotid = CheckAPIKey(api_key);
            if (ihotspotid == -2)
            {
                return new ReturnUserPackages() { Success = false, Message = ReinstallApp };
            }
            else if (ihotspotid == -1)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "getUserPackages", InvalidApiKey, "", Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key, user_id = user_id, macaddress = macaddress }));

                return new ReturnUserPackages() { Success = false, Message = InvalidApiKey, UserPackages = null };
            }

            try
            {
                using (var db = new RadiusdbDB())
                {
                    List<UserPackage> dataDetails = db.ExecuteQuery<UserPackage>(@"SELECT WC.id AS id,
                                                                        WC.login_username AS login_username,
                                                                        WC.login_password AS login_password,
                                                                        WC.active AS active,
                                                                        WC.expiry_date AS expiry_date,
                                                                        COALESCE(WC.credential_desc, '') AS credential_desc,
                                                                        COALESCE(WAT.accounttype_desc, '') AS accounttype_desc,
                                                                        COALESCE(HG.group_desc, '') AS group_desc,
                                                                        COALESCE(UG.groupname, '') AS group_name,
                                                                        COALESCE(R.value, WC.mac_address, '') AS mac_address,
                                                                        WC.service_provider_id AS service_provider_id,
                                                                        WC.package_id AS package_id,
                                                                        WC.createdate AS create_date,
                                                                        '' as usageleft_percentage,
                                                                        '' as usageleft_value,
                                                                        WC.use_rank
                                                                 FROM ao_account AS AO
                                                                 JOIN wifi_credentials AS WC ON AO.id = WC.ao_account_id
                                                                 JOIN wifi_account_type AS WAT ON wc.accounttype_id = WAT.id
                                                                 LEFT JOIN hotspot AS H ON WC.hotspot_id = H.id
                                                                 LEFT JOIN hotspot_group AS HG ON WC.hotspotgroup_id = HG.id
                                                                 LEFT JOIN usergroup AS UG ON WC.login_username = UG.username
                                                                 LEFT JOIN radcheck AS R ON WC.login_username = R.username AND R.attribute = 'Calling-Station-Id'
                                                                 WHERE AO.id = '" + user_id + @"'
                                                                 AND (WC.mac_address = '" + macaddress + @"'
                                                                     OR WC.mac_address IS NULL)
                                                                 AND accounttype_desc != 'Complimentary'
                                                                 AND (Expiry_Date >= '" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + @"' OR Expiry_Date IS NULL)
                                                                 AND COALESCE((select R.value from radcheck AS R where WC.login_username = R.username AND R.attribute = 'Expiration')::timestamp(0), now() + '1 day' ) > now()")
                                        .Distinct()
                                        .OrderByDescending(x => x.create_date)
                                        .ToList();

                    foreach (UserPackage r in dataDetails)
                    {
                        UsageLeft usageleft = Methods.getPackageDetails(r.login_username.ToString(), r.accounttype_desc.ToString());

                        string perc1 = usageleft.percentage.ToString("0");

                        int res = Convert.ToInt32(perc1);
                        perc1 = perc1.Replace(",", ".");

                        if (perc1 == "0")
                        {
                            r.usageleft_percentage = "0";
                            r.usageleft_value = "Package Used Up";
                        }
                        else if (usageleft.usageleft == "Unlimited")
                        {
                            r.usageleft_percentage = "100";
                            r.usageleft_value = "Uncapped";
                        }
                        else if (r.service_provider_id != "1")
                        {
                            r.usageleft_percentage = "100";
                            r.usageleft_value = "Service Provider Rate Limited";
                        }
                        else
                        {
                            r.usageleft_percentage = usageleft.percentage.ToString();
                            r.usageleft_value = usageleft.usageleft;
                        }
                    }

                    if (dataDetails != null && dataDetails.Count > 0)
                    {
                        Methods.AddMobileUserAction(user_id, "4");
                        return new ReturnUserPackages() { Success = true, Message = "", UserPackages = dataDetails };
                    }
                    else
                    {
                        return new ReturnUserPackages() { Success = false, Message = "User account has no linked packages", UserPackages = null };
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "getUserPackages", e.Message, (e.StackTrace ?? "") + "\nInnerException: " + Newtonsoft.Json.JsonConvert.SerializeObject((e.InnerException ?? new Exception(""))), Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key, user_id = user_id, macaddress = macaddress }));

                return new ReturnUserPackages() { Success = false, Message = "Error: " + e.Message, UserPackages = null };
            }
        }

        //4.5 Get User Packages
        public ReturnUserPackages getUserPackagesV2(string api_key, string user_id, string macaddress, string sessionid)
        {
            int ihotspotid = CheckAPIKey(api_key);
            if (ihotspotid == -2)
            {
                return new ReturnUserPackages() { Success = false, Message = ReinstallApp };
            }
            else if (ihotspotid == -1)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "getUserPackagesV2", InvalidApiKey, "", Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key, user_id = user_id, macaddress = macaddress, sessionid = sessionid }));

                return new ReturnUserPackages() { Success = false, Message = InvalidApiKey, UserPackages = null };
            }

            try
            {
                using (var db = new RadiusdbDB())
                {
                    List<UserPackage> dataDetails = db.ExecuteQuery<UserPackage>(@"SELECT WC.id AS id,
                                                                        WC.login_username AS login_username,
                                                                        WC.login_password AS login_password,
                                                                        WC.active AS active,
                                                                        WC.expiry_date AS expiry_date,
                                                                        COALESCE(WC.credential_desc, '') AS credential_desc,
                                                                        COALESCE(WAT.accounttype_desc, '') AS accounttype_desc,
                                                                        COALESCE(HG.group_desc, '') AS group_desc,
                                                                        COALESCE(UG.groupname, '') AS group_name,
                                                                        COALESCE(R.value, WC.mac_address, '') AS mac_address,
                                                                        WC.service_provider_id AS service_provider_id,
                                                                        WC.package_id AS package_id,
                                                                        WC.createdate AS create_date,
                                                                        '' as usageleft_percentage,
                                                                        '' as usageleft_value,
                                                                        WC.use_rank
                                                                 FROM ao_account AS AO
                                                                 JOIN wifi_credentials AS WC ON AO.id = WC.ao_account_id
                                                                 JOIN wifi_account_type AS WAT ON wc.accounttype_id = WAT.id
                                                                 LEFT JOIN hotspot AS H ON WC.hotspot_id = H.id
                                                                 LEFT JOIN hotspot_group AS HG ON WC.hotspotgroup_id = HG.id
                                                                 LEFT JOIN usergroup AS UG ON WC.login_username = UG.username
                                                                 LEFT JOIN radcheck AS R ON WC.login_username = R.username AND R.attribute = 'Calling-Station-Id'
                                                                 WHERE AO.id = '" + user_id + @"'
                                                                 AND (WC.mac_address = '" + macaddress + @"'
                                                                     OR WC.mac_address IS NULL)
                                                                 AND accounttype_desc != 'Complimentary'
                                                                 AND (Expiry_Date >= '" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + @"' OR Expiry_Date IS NULL)
                                                                 AND COALESCE((select R.value from radcheck AS R where WC.login_username = R.username AND R.attribute = 'Expiration')::timestamp(0), now() + '1 day' ) > now()")
                                        .Distinct()
                                        .OrderByDescending(x => x.create_date)
                                        .ToList();

                    var ret = new List<UserPackage>();
                    dataDetails.ForEach(p =>
                    {
                        var pud = Methods.getPackageUsageDetails(p.login_username, false);
                        var package = new UserPackage()
                        {
                            id = p.id,
                            login_username = p.login_username,
                            login_password = p.login_password,
                            active = p.active,
                            expiry_date = p.expiry_date,
                            credential_desc = p.credential_desc,
                            accounttype_desc = p.accounttype_desc,
                            group_desc = p.group_desc,
                            group_name = p.group_name,
                            mac_address = p.mac_address,
                            service_provider_id = p.service_provider_id,
                            package_id = p.package_id,
                            create_date = p.create_date,
                            use_rank = p.use_rank,

                            usageleft_percentage = pud.dblPercentageLeft.ToString("0"),
                            usageleft_value = pud.lblAccountBalance
                        };

                        if (int.TryParse(package.service_provider_id, out int spid) && spid > 1)
                        {
                            package.usageleft_value = "Service Provider Rate Limited";
                            package.usageleft_percentage = "100";
                            package.accounttype_desc = "Uncapped";
                        }
                        else if (package.group_name == "Unlimited" || package.group_name == "Uncapped")
                        {
                            package.usageleft_value = "Uncapped";
                            package.usageleft_percentage = "100";
                            package.accounttype_desc = "Uncapped";
                        }

                        if (!string.IsNullOrEmpty(sessionid))
                        {
                            using (var hs = new HotservTools.HotspotTools())
                            {
                                try
                                {
                                    hs.IsAccuris(username: package.login_username,
                                             password: package.login_password,
                                             usergroup: package.group_name,
                                             packageexpiry: pud.RadcheckUserExpiration,
                                             sessionid: sessionid);
                                }
                                catch (Exception ex)
                                {
                                    Logger.Log("AlwaysOnMobileService.svc.cs", "getUserPackagesV2", "hotspottools ex: " + ex.Message, (ex.StackTrace ?? "") + "\nInnerException: " + Newtonsoft.Json.JsonConvert.SerializeObject((ex.InnerException ?? new Exception(""))), Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key, user_id = user_id, macaddress = macaddress, sessionid = sessionid }));

                                    var ExceptionMessage = ex.Message;
                                }
                            }
                        }

                        ret.Add(package);
                    });

                    if (ret.Count > 0)
                    {
                        Methods.AddMobileUserAction(user_id, "4");
                        return new ReturnUserPackages() { Success = true, Message = "", UserPackages = ret };
                    }
                    else
                    {
                        return new ReturnUserPackages() { Success = true, Message = "User account has no linked packages", UserPackages = new List<UserPackage>() };
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "getUserPackagesV2", e.Message, (e.StackTrace ?? "") + "\nInnerException: " + Newtonsoft.Json.JsonConvert.SerializeObject((e.InnerException ?? new Exception(""))), Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key, user_id = user_id, macaddress = macaddress, sessionid = sessionid }));

                return new ReturnUserPackages() { Success = false, Message = "Error: " + e.Message, UserPackages = null };
            }
        }

        //5 Get available Packages to Purchase
        public ReturnVoucherPackages getVoucherPackages(string api_key)
        {
            int ihotspotid = CheckAPIKey(api_key);
            if (ihotspotid == -2)
            {
                return new ReturnVoucherPackages() { Success = false, Message = ReinstallApp };
            }
            else if (ihotspotid == -1)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "getVoucherPackages", InvalidApiKey, "", Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key }));

                return new ReturnVoucherPackages() { Success = false, Message = InvalidApiKey, PackageItems = null };
            }

            try
            {
                List<Packageitems> packages = null;

                var parms = new string[] { "T", "DT", "D" }.ToList();

                using (var db = new RadiusdbDB())
                {
                    packages = (from p in db.Access_Packages
                                join hp in db.Hotspot_Ccpackages
                                on p.Id equals hp.Access_Package_Id
                                where parms.Contains(p.Package_Type)
                                && hp.Hotspot_Id == ihotspotid
                                select new Packageitems
                                {
                                    listOrder = p.List_Order.ToString(),
                                    id = p.Id.ToString(),
                                    defaultPackage = p.Default_Package,
                                    displayPrice = p.Display_Price,
                                    optionDesc = p.Option_Desc,
                                    iveriPrice = p.Iveri_Price,
                                    packageName = p.Package_Name,
                                    packageType = p.Package_Type,
                                    packageDesc = p.Package_Desc,
                                    expirationHours = p.Expiration_Hours.ToString(),
                                    expirationDays = p.Expiration_Days.ToString(),
                                    currency = p.Currency,
                                    usageDescription = p.Usage_Description.ToString(),
                                    isfromfirstlogin = p.Exp_From_First_Login,
                                    radgroupcheckid = p.Radgroupcheck_Id.ToString()
                                })
                               .ToList()
                               .OrderBy(x => x.listOrder)
                               .ToList();
                }

                if (packages != null && packages.Count > 0)
                {
                    packages.ForEach(n =>
                    {
                        n.additionalInfo = new AdditionalInfo();

                        string strDesc = n.packageDesc;
                        strDesc = strDesc.IndexOf(" - ") > 0 ? strDesc.Substring(strDesc.IndexOf(" - ") + 3).Trim() : strDesc;
                        strDesc = strDesc.IndexOf("(") > 0 ? strDesc.Substring(0, strDesc.IndexOf("(")).Trim() : strDesc;
                        strDesc = strDesc.Contains("Minutes") ? strDesc.Replace("Minutes", "MIN") : strDesc;
                        strDesc = strDesc.Contains("Hours") ? strDesc.Replace("Hours", "HRS") : strDesc;
                        strDesc = strDesc.Trim().Replace(" ", "");
                        int pos = Regex.Match(strDesc, "[A-Za-z]").Index;
                        n.additionalInfo.HeaderPre = strDesc.Substring(0, pos);
                        n.additionalInfo.HeaderPost = strDesc.Substring(pos).ToUpper();

                        n.additionalInfo.Value = n.additionalInfo.HeaderPre + " " + n.additionalInfo.HeaderPost;

                        if (n.additionalInfo.HeaderPre == "1" && n.additionalInfo.HeaderPost.ToLower() == "hour")
                        {
                            n.additionalInfo.HeaderPre = "60";
                            n.additionalInfo.HeaderPost = "MIN";
                        }

                        n.additionalInfo.Period = "Once-off cost";

                        if (n.additionalInfo.HeaderPost.Contains("MIN") || n.additionalInfo.HeaderPost.Contains("HRS") || n.additionalInfo.HeaderPost.Contains("DAY"))
                        {
                            var minutes = 0.0;
                            minutes = n.additionalInfo.HeaderPost.Contains("MIN") ? Convert.ToInt32(n.additionalInfo.HeaderPre) : minutes;
                            minutes = n.additionalInfo.HeaderPost.Contains("HRS") ? Convert.ToInt32(n.additionalInfo.HeaderPre) * 60 : minutes;
                            minutes = n.additionalInfo.HeaderPost.Contains("DAY") ? Convert.ToInt32(n.additionalInfo.HeaderPre) * 60 * 24 : minutes;

                            n.additionalInfo.Songs = Math.Round((minutes / 3) * 2, 0).ToString();
                            n.additionalInfo.Videos = Math.Round(minutes / 3, 0).ToString();
                            n.additionalInfo.Voice = minutes.ToString();
                        }
                        else if (n.additionalInfo.HeaderPost.Contains("MB") || n.additionalInfo.HeaderPost.Contains("GB"))
                        {
                            var megabytes = 0.0;
                            megabytes = n.additionalInfo.HeaderPost.Contains("MB") ? Convert.ToInt32(n.additionalInfo.HeaderPre) : megabytes;
                            megabytes = n.additionalInfo.HeaderPost.Contains("GB") ? Convert.ToInt32(n.additionalInfo.HeaderPre) * 1000 : megabytes;

                            n.additionalInfo.Songs = Math.Round((megabytes / 50) * 15, 0).ToString();
                            n.additionalInfo.Videos = Math.Round(megabytes / 10, 0).ToString();
                            n.additionalInfo.Voice = n.additionalInfo.Songs;
                        }

                        var expiration = (n.expirationDays != "0") ? "Expires in " + n.expirationDays + " days" + (n.isfromfirstlogin == "Y" ? ", from first login" : "") : (n.expirationHours != "0" && n.expirationHours != null) ? "Expires in " + n.expirationHours + " hours" + (n.isfromfirstlogin == "Y" ? ", from first login" : "") : "No Expiration";
                        n.additionalInfo.Description = Methods.FirstCharToUpper(n.usageDescription) + " ~ " + expiration;
                        n.additionalInfo.Price = n.displayPrice.ToString().Replace(" ", "").Replace(".00", "") + "*";
                        n.additionalInfo.ExpiryDays = n.expiryDays;
                        double scopeValue = 0;

                        string data = Methods.getpackageData(n.radgroupcheckid);

                        if (data != "" && (n.packageType.Trim() == "D" || n.packageType.Trim() == "DT"))
                        {
                            scopeValue = (Convert.ToDouble(n.iveriPrice) / Convert.ToDouble(data) * 10000);
                            scopeValue = Math.Round(scopeValue, 2) == 0.4 ? scopeValue / 10 : scopeValue;
                        }
                        scopeValue = data != "" && (n.packageType.Trim() == "T") ? (Convert.ToDouble(n.iveriPrice) / (Convert.ToDouble(data) / 60)) / 100 : scopeValue;
                        n.additionalInfo.PricePer = (n.packageType.Trim() == "T") ? "R" + scopeValue.ToString("0.00") + " /MIN" : n.additionalInfo.PricePer;
                        n.additionalInfo.PricePer = (n.packageType.Trim() == "D" || n.packageType.Trim() == "DT") ? "R" + Math.Round(scopeValue, 2) + " /MB" : n.additionalInfo.PricePer;
                    });

                    return new ReturnVoucherPackages() { Success = true, Message = "", PackageItems = packages };
                }
                else
                {
                    return new ReturnVoucherPackages() { Success = false, Message = "There are no available packages", PackageItems = null };
                }
            }
            catch (Exception e)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "getVoucherPackages", e.Message, (e.StackTrace ?? "") + "\nInnerException: " + Newtonsoft.Json.JsonConvert.SerializeObject((e.InnerException ?? new Exception(""))), Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key }));

                return new ReturnVoucherPackages() { Success = false, Message = "Error: " + e.Message, PackageItems = null };
            }
        }

        //6 Link Device TO account
        public ReturnDefault linkDevice(string api_key, string user_id, string macaddress)
        {
            int ihotspotid = CheckAPIKey(api_key);
            if (ihotspotid == -2)
            {
                return new ReturnDefault() { Success = false, Message = ReinstallApp };
            }
            else if (ihotspotid == -1)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "linkDevice", InvalidApiKey, "", Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key, user_id = user_id, macaddress = macaddress }));

                return new ReturnDefault() { Success = false, Message = InvalidApiKey };
            }

            try
            {
                var success = false;
                using (var db = new RadiusdbDB())
                {
                    Methods.AddMobileUserAction(user_id, "5");
                    success = (int)db.Aolinkdevice(Convert.ToInt32(user_id), macaddress) > 0;
                }

                return new ReturnDefault() { Success = success, Message = success ? "Device Link Successful" : "Device Link Failed" };
            }
            catch (Exception e)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "linkDevice", e.Message, (e.StackTrace ?? "") + "\nInnerException: " + Newtonsoft.Json.JsonConvert.SerializeObject((e.InnerException ?? new Exception(""))), Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key, user_id = user_id, macaddress = macaddress }));

                return new ReturnDefault() { Success = false, Message = "Error: " + e.Message };
            }
        }

        //7 Unlink device from account
        public ReturnDefault unlinkDevice(string api_key, string user_id, string macaddress)
        {
            int ihotspotid = CheckAPIKey(api_key);
            if (ihotspotid == -2)
            {
                return new ReturnDefault() { Success = false, Message = ReinstallApp };
            }
            else if (ihotspotid == -1)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "unlinkDevice", InvalidApiKey, "", Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key, user_id = user_id, macaddress = macaddress }));

                return new ReturnDefault() { Success = false, Message = InvalidApiKey };
            }

            try
            {
                var success = false;
                using (var db = new RadiusdbDB())
                {
                    Methods.AddMobileUserAction(user_id, "6");
                    success = (int)db.Aounlinkdevice(Convert.ToInt32(user_id), macaddress) > 0;
                }

                return new ReturnDefault() { Success = success, Message = success ? "Device Unlink Successful" : "Device Unlink Failed" };
            }
            catch (Exception e)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "unlinkDevice", e.Message, (e.StackTrace ?? "") + "\nInnerException: " + Newtonsoft.Json.JsonConvert.SerializeObject((e.InnerException ?? new Exception(""))), Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key, user_id = user_id, macaddress = macaddress }));

                return new ReturnDefault() { Success = false, Message = "Error: " + e.Message };
            }
        }

        //8 Link Existing Credential to account
        public ReturnDefault link_existing_credentials(string api_key, string user_id, string username, string password, string service_provider)
        {
            int ihotspotid = CheckAPIKey(api_key);
            if (ihotspotid == -2)
            {
                return new ReturnDefault() { Success = false, Message = ReinstallApp };
            }
            else if (ihotspotid == -1)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "link_existing_credentials", InvalidApiKey, "", Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key, user_id = user_id, username = username, password = password, service_provider = service_provider }));

                return new ReturnDefault() { Success = false, Message = InvalidApiKey };
            }

            try
            {
                if (service_provider == "0" || string.IsNullOrEmpty(service_provider))
                {
                    service_provider = "1";
                }

                string strPrefix = "";
                bool result = Methods.existsBeforeLink(username.Trim(), password.Trim());
                if (result)
                {
                    string id = "";
                    string type = "9";

                    //add to wifi_credentials
                    string samsung = Methods.isSamsungAccount(username.Trim());

                    if (samsung != null)
                    {
                        type = "2";
                        id = Methods.linkToAccount(username.Trim(), password.Trim(), user_id, type, service_provider, samsung, 200);
                    }
                    else
                    {
                        List<Voucher> vdetails = Methods.getVoucherUserName(username.Trim());
                        if (vdetails != null && vdetails.Count() > 0)
                        {
                            foreach (Voucher v in vdetails)
                            {
                                type = v.Expiration_Datetime == "No Expiration" ? "4" : "9";
                                id = Methods.linkToAccount(username.Trim(), password.Trim(), user_id, type, service_provider, null, 0);
                            }
                        }
                        else
                        {
                            id = Methods.linkToAccount(username.Trim(), password.Trim(), user_id, type, service_provider, null, 0);
                        }
                    }

                    if (id != "0")
                    {
                        if (id == "-1")
                        {
                            //already linked
                            return new ReturnDefault() { Success = false, Message = "Credentials is already linked to an AlwaysOn account" };
                        }
                        else
                        {
                            Methods.AddMobileUserAction(user_id, "7");
                            //success
                            return new ReturnDefault() { Success = true, Message = "Credentials successfully linked" };
                        }
                    }
                    else
                    {
                        //failed
                        return new ReturnDefault() { Success = false, Message = "An error occurred" };
                    }
                }
                else
                {
                    string strusername = "";
                    int intResult = 0;
                    List<ServiceProvider> rp = Methods.getServiceProviders();

                    foreach (ServiceProvider r in rp)
                    {
                        if (r.Id.ToString() == service_provider)
                        {
                            strPrefix = r.Routing_Prefix;
                            strusername = strPrefix + "/" + username.Trim();
                        }
                    }

                    if (strPrefix == "AOC")
                    {
                        SecureRadCheck securecheck = Methods.getSecureRadCheckVariable(username.Trim(), password.Trim());

                        if (securecheck != null)
                        { intResult = 0; }
                        else
                        { intResult = 1; }
                    }
                    else if (strPrefix != "AFRIHOST")
                    {
                        string strRadiusServer = System.Configuration.ConfigurationManager.AppSettings["RadiusServer"];
                        string strRadiusSecret = System.Configuration.ConfigurationManager.AppSettings["RadiusSecret"];


                        RadiusClient myRadius = new RadiusClient(strRadiusServer, strRadiusSecret, strusername, password.Trim());
                        myRadius.SetAttribute(4, "10.0.2.220");
                        int radsessionid = Methods.radSessionId();
                        myRadius.SetAttribute(44, radsessionid.ToString());
                        intResult = myRadius.Authenticate();
                    }
                    else
                    {
                        intResult = 0;
                    }

                    if (intResult == 0)
                    {
                        string id = Methods.linkToAccountSP(strusername, password.Trim(), user_id, "9", service_provider, null);
                        if (id != "0")
                        {
                            Methods.AddMobileUserAction(user_id, "7");
                            //success
                            return new ReturnDefault() { Success = true, Message = "Credentials successfully linked" };
                        }
                        else
                        {
                            //failed
                            return new ReturnDefault() { Success = false, Message = "An error occurred" };
                        }
                    }
                    else
                    {
                        //does not exist
                        return new ReturnDefault() { Success = false, Message = "Username and Password combination does not exist or is already linked to a different account" };
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "link_existing_credentials", e.Message, (e.StackTrace ?? "") + "\nInnerException: " + Newtonsoft.Json.JsonConvert.SerializeObject((e.InnerException ?? new Exception(""))), Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key, user_id = user_id, username = username, password = password, service_provider = service_provider }));

                return new ReturnDefault() { Success = false, Message = "Error: " + e.Message };
            }
        }

        //9 Unlink Existing Credential to account
        public ReturnDefault unlink_existing_credentials(string api_key, int user_package_id)
        {
            int ihotspotid = CheckAPIKey(api_key);
            if (ihotspotid == -2)
            {
                return new ReturnDefault() { Success = false, Message = ReinstallApp };
            }
            else if (ihotspotid == -1)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "unlink_existing_credentials", InvalidApiKey, "", Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key, user_package_id = user_package_id }));

                return new ReturnDefault() { Success = false, Message = InvalidApiKey };
            }

            try
            {
                using (var db = new RadiusdbDB())
                {
                    db.Deletewificredential(1, user_package_id);
                }

                return new ReturnDefault() { Success = true, Message = "Credentials successfully removed" };
            }
            catch (Exception e)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "unlink_existing_credentials", e.Message, (e.StackTrace ?? "") + "\nInnerException: " + Newtonsoft.Json.JsonConvert.SerializeObject((e.InnerException ?? new Exception(""))), Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key, user_package_id = user_package_id }));

                return new ReturnDefault() { Success = false, Message = "Error: " + e.Message };
            }
        }

        //10 Get Service Providers
        public ReturnServiceProviders getServiceProviders(string api_key)
        {
            int ihotspotid = CheckAPIKey(api_key);
            if (ihotspotid == -2)
            {
                return new ReturnServiceProviders() { Success = false, Message = ReinstallApp };
            }
            else if (ihotspotid == -1)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "getServiceProviders", InvalidApiKey, "", Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key }));

                return new ReturnServiceProviders() { Success = false, Message = InvalidApiKey, ServiceProviders = null };
            }

            try
            {
                var providers = new List<ServiceProvide>();
                using (var db = new RadiusdbDB())
                {
                    providers = Methods.getServiceProviders().Select(n => new ServiceProvide() { Id = n.Id, Description = n.Landingpage_Desc }).ToList();
                }

                if (providers != null && providers.Count > 0)
                {
                    return new ReturnServiceProviders() { Success = true, Message = "", ServiceProviders = providers };
                }
                else
                {
                    return new ReturnServiceProviders() { Success = false, Message = "There are no service providers", ServiceProviders = null };
                }
            }
            catch (Exception e)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "getServiceProviders", e.Message, (e.StackTrace ?? "") + "\nInnerException: " + Newtonsoft.Json.JsonConvert.SerializeObject((e.InnerException ?? new Exception(""))), Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key }));

                return new ReturnServiceProviders() { Success = false, Message = "Error: " + e.Message, ServiceProviders = null };
            }
        }

        //11 Get Hotspot Locations
        public ReturnHotspotMarkers getHotspotLocations(string api_key, string lata, string latb, string lnga, string lngb)
        {
            int ihotspotid = CheckAPIKey(api_key);
            if (ihotspotid == -2)
            {
                return new ReturnHotspotMarkers() { Success = false, Message = ReinstallApp };
            }
            else if (ihotspotid == -1)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "getHotspotLocations", InvalidApiKey, "", Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key, lata = lata, latb = latb, lnga = lnga, lngb = lngb }));

                return new ReturnHotspotMarkers() { Success = false, Message = InvalidApiKey, HotspotMarkers = null };
            }

            try
            {
                var dLatA = Convert.ToDouble(lata);
                var dLatB = Convert.ToDouble(latb);
                var dLngA = Convert.ToDouble(lnga);
                var dLngB = Convert.ToDouble(lngb);

                var latA = dLatA > dLatB ? dLatB : dLatA;
                var latB = dLatA > dLatB ? dLatA : dLatB;
                var lngA = dLngA > dLngB ? dLngB : dLngA;
                var lngB = dLngA > dLngB ? dLngA : dLngB;
                /*
                a.international,
                SQRT(POW(111.2 * (a.lat - '" + struserlat + @"'), 2) + POW(111.2 * ('" + struserlng + @"' - a.lng) * COS(a.lat / 57.2958), 2)) AS distanceinkilometers,
                 */
                var cmd = @"select  d.lat,
                                    d.lng,
                                    d.data,
                                    null as options,
                                    d.superwifi,
                                    0 as international,
                                    0 as distanceinkilometers
                            from (select CAST(trim(replace(regexp_replace(h.gps_y, '[^0-9.,-]+', '', 'g'),',','.')) as float) as lat,
		                                 CAST(trim(replace(regexp_replace(h.gps_x, '[^0-9.,-]+', '', 'g'),',','.')) as float) as lng,
		                                 h.locationname as data,
		                                 case when hs.id is null then 0 else 1 end as superwifi
                                  from hotspot as h
                                  left join (select h1.id from hotspot as h1 join location_type as lt1 on h1.location_type_id = lt1.id where h1.backhaul_type_id in (11, 12, 13, 14, 18)) as hs on h.id = hs.id
                                  where length(trim(replace(regexp_replace(h.gps_y, '[^0-9.,-]+', '', 'g'),',','.'))) > 0
                                  and length(trim(replace(regexp_replace(h.gps_x, '[^0-9.,-]+', '', 'g'),',','.'))) > 0
                                  and h.live = 'Y') as d
                            where d.lat <> 0
                            and d.lng <> 0
                            and d.lat between '" + latA.ToString().Replace(",", ".") + "' and '" + latB.ToString().Replace(",", ".") + @"'
                            and d.lng between '" + lngA.ToString().Replace(",", ".") + "' and '" + lngB.ToString().Replace(",", ".") + "'";

                var markers = new List<HotspotMarker>();
                using (var db = new RadiusdbDB())
                {
                    markers = db.ExecuteQuery<HotspotMarker>(cmd).ToList();
                }

                if (markers != null && markers.Count > 0)
                {
                    return new ReturnHotspotMarkers() { Success = true, Message = "", HotspotMarkers = markers };
                }
                else
                {
                    return new ReturnHotspotMarkers() { Success = false, Message = "No markers found", HotspotMarkers = null };
                }
            }
            catch (Exception e)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "getHotspotLocations", e.Message, (e.StackTrace ?? "") + "\nInnerException: " + Newtonsoft.Json.JsonConvert.SerializeObject((e.InnerException ?? new Exception(""))), Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key, lata = lata, latb = latb, lnga = lnga, lngb = lngb }));

                return new ReturnHotspotMarkers() { Success = false, Message = "Error: " + e.Message, HotspotMarkers = null };
            }
        }

        //12 Usage Summary
        public ReturnUsageSummary getUsageSummary(string api_key, string username)
        {
            int ihotspotid = CheckAPIKey(api_key);
            if (ihotspotid == -2)
            {
                return new ReturnUsageSummary() { Success = false, Message = ReinstallApp };
            }
            else if (ihotspotid == -1)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "getUsageSummary", InvalidApiKey, "", Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key, username = username }));

                return new ReturnUsageSummary() { Success = false, Message = InvalidApiKey, UsageSummary = null };
            }

            try
            {
                var summary = Methods.getPackageUsageSummary(username);

                if (summary != null)
                {
                    return new ReturnUsageSummary() { Success = true, Message = "", UsageSummary = summary };
                }
                else
                {
                    return new ReturnUsageSummary() { Success = false, Message = "No summary found for this package", UsageSummary = null };
                }
            }
            catch (Exception e)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "getUsageSummary", e.Message, (e.StackTrace ?? "") + "\nInnerException: " + Newtonsoft.Json.JsonConvert.SerializeObject((e.InnerException ?? new Exception(""))), Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key, username = username }));

                return new ReturnUsageSummary() { Success = false, Message = "Error: " + e.Message, UsageSummary = null };
            }
        }

        //13 Usage Detailed
        public ReturnUsageDetailed getUsageDetailed(string api_key, string username, string fromDate, string toDate)
        {
            int ihotspotid = CheckAPIKey(api_key);
            if (ihotspotid == -2)
            {
                return new ReturnUsageDetailed() { Success = false, Message = ReinstallApp };
            }
            else if (ihotspotid == -1)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "getUsageDetailed", InvalidApiKey, "", Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key, username = username, fromDate = fromDate, toDate = toDate }));

                return new ReturnUsageDetailed() { Success = false, Message = InvalidApiKey, UsageDetail = null };
            }

            try
            {
                var detail = Methods.getPackageUsageDetails(username, fromDate, toDate);
                if (detail != null && detail.Count > 0)
                {
                    return new ReturnUsageDetailed() { Success = true, Message = "", UsageDetail = detail };
                }
                else
                {
                    return new ReturnUsageDetailed() { Success = false, Message = "Found no details for the provided parameters", UsageDetail = null };
                }
            }
            catch (Exception e)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "getUsageDetailed", e.Message, (e.StackTrace ?? "") + "\nInnerException: " + Newtonsoft.Json.JsonConvert.SerializeObject((e.InnerException ?? new Exception(""))), Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key, username = username, fromDate = fromDate, toDate = toDate }));

                return new ReturnUsageDetailed() { Success = false, Message = "Error: " + e.Message, UsageDetail = null };
            }
        }

        //14 Forgot Password
        public ReturnDefault forgotPassword(string api_key, string email, string mobile)
        {
            int ihotspotid = CheckAPIKey(api_key);
            if (ihotspotid == -2)
            {
                return new ReturnDefault() { Success = false, Message = ReinstallApp };
            }
            else if (ihotspotid == -1)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "forgotPassword", InvalidApiKey, "", Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key, email = email, mobile = mobile }));

                return new ReturnDefault() { Success = false, Message = InvalidApiKey };
            }

            try
            {
                var message = Methods.ForgotPassword(email, mobile);

                return new ReturnDefault() { Success = true, Message = message };
            }
            catch (Exception e)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "forgotPassword", e.Message, (e.StackTrace ?? "") + "\nInnerException: " + Newtonsoft.Json.JsonConvert.SerializeObject((e.InnerException ?? new Exception(""))), Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key, email = email, mobile = mobile }));

                return new ReturnDefault() { Success = false, Message = "Error: " + e.Message };
            }
        }

        //15 Is Super WiFi
        public ReturnSuperWiFi is_superwifi(string api_key, string IP_address)
        {
            int ihotspotid = CheckAPIKey(api_key);
            if (ihotspotid == -2)
            {
                return new ReturnSuperWiFi() { Success = false, Message = ReinstallApp };
            }
            else if (ihotspotid == -1)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "is_superwifi", InvalidApiKey, "", Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key, IP_address = IP_address }));

                return new ReturnSuperWiFi() { Success = false, Message = InvalidApiKey };
            }

            try
            {
                bool issuperwifi = Methods.is_superwifi(IP_address);

                return new ReturnSuperWiFi() { Success = true, Message = "Successful", issuperwifi = issuperwifi };
            }
            catch (Exception e)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "is_superwifi", e.Message, (e.StackTrace ?? "") + "\nInnerException: " + Newtonsoft.Json.JsonConvert.SerializeObject((e.InnerException ?? new Exception(""))), Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key, IP_address = IP_address }));

                return new ReturnSuperWiFi() { Success = false, Message = "Error: " + e.Message };
            }
        }

        //3DSecure Token Check
        public ReturnToken ThreeDSecureTokenCheck(string api_key, string user_id)
        {
            int ihotspotid = CheckAPIKey(api_key);
            if (ihotspotid == -2)
            {
                return new ReturnToken() { Success = false, Message = ReinstallApp };
            }
            else if (ihotspotid == -1)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "ThreeDSecureTokenCheck", InvalidApiKey, "", Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key, user_id = user_id }));

                return new ReturnToken() { Success = false, Message = InvalidApiKey, TokenReturn = null };
            }

            try
            {
                var tokenReturn = Methods.getTokenDetail(user_id);

                return new ReturnToken() { Success = tokenReturn.TokenSuccess, Message = tokenReturn.ErrorMessage, TokenReturn = tokenReturn };
            }
            catch (Exception e)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "ThreeDSecureTokenCheck", e.Message, (e.StackTrace ?? "") + "\nInnerException: " + Newtonsoft.Json.JsonConvert.SerializeObject((e.InnerException ?? new Exception(""))), Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key, user_id = user_id }));

                return new ReturnToken() { Success = false, Message = "Error: " + e.Message, TokenReturn = null };
            }
        }

        //3DSecure Check
        public ReturnThreeDSecure ThreeDSecureCheck(string api_key, string email, string package_id, string card_number, string card_exp_month, string card_exp_year, string returnURL)
        {
            int ihotspotid = CheckAPIKey(api_key);
            if (ihotspotid == -2)
            {
                return new ReturnThreeDSecure() { Success = false, Message = ReinstallApp };
            }
            else if (ihotspotid == -1)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "ThreeDSecureCheck", InvalidApiKey, "", Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key, email = email, package_id = package_id, card_number = card_number, card_exp_month = card_exp_month, card_exp_year = card_exp_year, returnURL = returnURL }));

                return new ReturnThreeDSecure() { Success = false, Message = InvalidApiKey, ThreeDSecure = null };
            }

            try
            {
                ThreeDSecure ThreeD = new ThreeDSecure();

                //Get PackageDetail  
                Packageitems item = Methods.getPackageDetail(package_id);

                int intBlackList = staticFunctions.validateUser(email, "", card_number.Trim(), card_exp_month, card_exp_year, ihotspotid.ToString(), "1.1.1", (string)item.packageDesc);

                if (intBlackList == 0)
                {
                    using (var ivi = new iVeriInterface.iVeriInterface())
                    {
                        ThreeD.MerchantReference = ivi.generateMerchantReference(System.Configuration.ConfigurationManager.AppSettings["iVeri_Auth_Code"], //strAuthCode
                                                                         Boolean.Parse(System.Configuration.ConfigurationManager.AppSettings["test_mode"])); //blTestMode
                    }

                    var ThreeDSecureActive = Boolean.Parse(System.Configuration.ConfigurationManager.AppSettings["TDSecureActive"]);
                    ThreeD.CardType = staticFunctions.getCardType(card_number.Trim());
                    var CardTypes = new int[] { 3, 4 }.ToList(); //VISA AND MASTERCARD
                    if (CardTypes.Contains(ThreeD.CardType) && ThreeDSecureActive)
                    {
                        var TDProcessorId = System.Configuration.ConfigurationManager.AppSettings["TDProcessorId"].ToString();
                        var TDMerchantId = System.Configuration.ConfigurationManager.AppSettings["TDMerchantId"].ToString();
                        var TDTransactionPwd = System.Configuration.ConfigurationManager.AppSettings["TDTransactionPwd"].ToString();
                        var TDURL = System.Configuration.ConfigurationManager.AppSettings["TDURL"].ToString();

                        string xml = "<CardinalMPI>";
                        xml += "<MsgType>cmpi_lookup</MsgType>";
                        xml += "<Version>1.7</Version>";
                        xml += "<ProcessorId>" + TDProcessorId + "</ProcessorId>";
                        xml += "<MerchantId>" + TDMerchantId + "</MerchantId>";
                        xml += "<TransactionPwd>" + TDTransactionPwd + "</TransactionPwd>";
                        xml += "<TransactionType>C</TransactionType>";
                        xml += "<Amount>" + item.iveriPrice + "</Amount>";
                        xml += "<CurrencyCode>710</CurrencyCode>";
                        xml += "<OrderNumber>" + ThreeD.MerchantReference + "</OrderNumber>";
                        xml += "<CardNumber>" + card_number.Trim() + "</CardNumber>";
                        xml += "<CardExpMonth>" + card_exp_month + "</CardExpMonth>";
                        xml += "<CardExpYear>" + card_exp_year + "</CardExpYear>";
                        xml += "</CardinalMPI>";

                        var request = TDURL + "?cmpi_msg=" + xml;
                        var returnXML = "";
                        using (var client = new WebClient())
                        {
                            returnXML = client.UploadString(request, "");
                        }

                        ////XML string to Object
                        var obj = CardinalMPI.CardinalObjectXML<CardinalMPI.CMPILUResponse>.getCardinalObjectFromXMLString(returnXML);
                        var objstring = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(obj);
                        if (obj.ACSUrl != null)
                        {
                            if (Methods.CheckACSUrl(obj.ACSUrl))
                            {
                                var VEResStatus = new string[] { "Y", "N", "U" }.ToList();

                                if (obj.ErrorNo == "0" && VEResStatus.Contains(obj.Enrolled))
                                {
                                    if (obj.Enrolled == "Y")
                                    {
                                        //Lookup success, go to next step
                                        ThreeD.DirectTransaction = false;
                                        ThreeD.ACSUrl = "<HTML><BODY onload='document.frmLaunch.submit();'><FORM name='frmLaunch' method='POST' action='" + obj.ACSUrl + "'><input type=hidden name='PaReq' value='" + obj.Payload + "'><input type=hidden name='TermUrl' value='" + returnURL + "'><input type=hidden name='MD' value='" + obj.TransactionId + "'></FORM><sc\" + \"ript>document.frmLaunch.submit(); </scr\" + \"ipt></BODY></HTML>";
                                    }
                                    else if (obj.Enrolled == "U")
                                    {
                                        if (ThreeD.CardType == 4) //VISA
                                            ThreeD.error_message = staticFunctions.uppercaseFirstLetterOfWord("The 3D secure check failed, we were unable to continue with your request. Please contact support on 0861 HOTSPOT (0861 468 7768) or +27 11 759 7300...");
                                        else
                                            ThreeD.DirectTransaction = true; //Make Payment
                                    }
                                    else if (obj.Enrolled == "N")
                                    {
                                        ThreeD.DirectTransaction = true;
                                    }
                                    else
                                        ThreeD.error_message = staticFunctions.uppercaseFirstLetterOfWord("The 3D secure check failed, we were unable to continue with your request. Please contact support on 0861 HOTSPOT (0861 468 7768) or +27 11 759 7300...");
                                }
                                else
                                    ThreeD.error_message = staticFunctions.uppercaseFirstLetterOfWord("The 3D secure check failed, we were unable to continue with your request. Please contact support on 0861 HOTSPOT (0861 468 7768) or +27 11 759 7300...");
                            }
                            else
                                ThreeD.DirectTransaction = true;
                        }
                        else
                            ThreeD.error_message = staticFunctions.uppercaseFirstLetterOfWord("The 3D secure check failed, we were unable to continue with your request. Please contact support on 0861 HOTSPOT (0861 468 7768) or +27 11 759 7300...");
                    }
                    else
                        ThreeD.DirectTransaction = true;
                }
                else
                    ThreeD.error_message = staticFunctions.uppercaseFirstLetterOfWord("This client is blacklisted");

                if (ThreeD.error_message != null && ThreeD.error_message.Trim().Length > 0)
                {
                    return new ReturnThreeDSecure() { Success = false, Message = ThreeD.error_message, ThreeDSecure = ThreeD };
                }
                else
                {
                    return new ReturnThreeDSecure() { Success = true, Message = "", ThreeDSecure = ThreeD };
                }
            }
            catch (Exception e)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "ThreeDSecureCheck", e.Message, (e.StackTrace ?? "") + "\nInnerException: " + Newtonsoft.Json.JsonConvert.SerializeObject((e.InnerException ?? new Exception(""))), Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key, email = email, package_id = package_id, card_number = card_number, card_exp_month = card_exp_month, card_exp_year = card_exp_year, returnURL = returnURL }));

                return new ReturnThreeDSecure() { Success = false, Message = "Error: " + e.Message, ThreeDSecure = null };
            }
        }

        //3DSecure Payment
        public ReturnThreeDSecurePayment ThreeDSecurePayment(string api_key, string user_id, string macaddress, string email, string mobile, string cell_country_code, string package_id, string card_number, string cvv, string name_on_card, string card_exp_month, string card_exp_year, int card_type, string merchantRef, bool directTransaction, string MD, string PaRes)
        {
            int ihotspotid = CheckAPIKey(api_key);
            if (ihotspotid == -2)
            {
                return new ReturnThreeDSecurePayment() { Success = false, Message = ReinstallApp };
            }
            else if (ihotspotid == -1)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "ThreeDSecurePayment", InvalidApiKey, "", Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key, user_id = user_id, macaddress = macaddress, email = email, mobile = mobile, cell_country_code = cell_country_code, package_id = package_id, card_number = card_number, cvv = cvv, name_on_card = name_on_card, card_exp_month = card_exp_month, card_exp_year = card_exp_year, card_type = card_type, merchantRef = merchantRef, directTransaction = directTransaction, MD = MD, PaRes = PaRes }));

                return new ReturnThreeDSecurePayment() { Success = false, Message = InvalidApiKey, PaymentReturn = null };
            }

            try
            {
                var paymentReturn = new PaymentReturn();

                if (package_id != null && package_id.Trim() != "")
                {
                    //Get PackageDetail  
                    var package = Methods.getPackageDetail(package_id);

                    if (!directTransaction)
                    {
                        if (MD != null)
                        {
                            var TDProcessorId = System.Configuration.ConfigurationManager.AppSettings["TDProcessorId"].ToString();
                            var TDMerchantId = System.Configuration.ConfigurationManager.AppSettings["TDMerchantId"].ToString();
                            var TDTransactionPwd = System.Configuration.ConfigurationManager.AppSettings["TDTransactionPwd"].ToString();
                            var TDURL = System.Configuration.ConfigurationManager.AppSettings["TDURL"].ToString();

                            var xml = "<CardinalMPI>";
                            xml += "<Version>1.7</Version>";
                            xml += "<MsgType>cmpi_authenticate</MsgType>";
                            xml += "<ProcessorId>" + TDProcessorId + "</ProcessorId>";
                            xml += "<MerchantId>" + TDMerchantId + "</MerchantId>";
                            xml += "<TransactionType>C</TransactionType>";
                            xml += "<TransactionPwd>" + TDTransactionPwd + "</TransactionPwd>";
                            xml += "<TransactionId>" + MD + "</TransactionId>";
                            xml += "<PAResPayload>" + PaRes + "</PAResPayload>";
                            xml += "</CardinalMPI>";

                            var returnXML = "";
                            using (var client = new WebClient())
                            {
                                returnXML = client.UploadString(TDURL + "?cmpi_msg=" + HttpUtility.UrlEncode(xml), "");
                            }

                            ////XML string to Object
                            var obj = CardinalMPI.CardinalObjectXML<CardinalMPI.CMPIAuthResponse>.getCardinalObjectFromXMLString(returnXML);

                            Methods.logPurchaseAction(ihotspotid.ToString(),
                                                      macaddress,
                                                      "1.1.1",
                                                      name_on_card, //strName
                                                      email, //strEmail
                                                      staticFunctions.hideCardNumber(card_number.Trim()), //strCardNum
                                                      package.displayPrice, //strAmount
                                                      package_id, //PackageId
                                                      obj.ErrorNo + " - " + obj.ErrorDesc, //strError
                                                      "Purchase attempt", //strDescription
                                                      "", //strURL
                                                      "", //strEnrolled
                                                      "N", //strCompleted
                                                      MD, //strTransId
                                                      PaRes, //strPares
                                                      merchantRef); //strMerchantRef

                            var PAResStatus = new string[] { "Y", "N", "U", "A" }.ToList();
                            if (obj.ErrorNo == "0" && obj.SignatureVerification == "Y" && PAResStatus.Contains(obj.PAResStatus))
                            {
                                if (obj.PAResStatus == "Y")
                                {
                                    var ECIFlags = new string[] { "02", "2", "05", "5" }.ToList();
                                    if (ECIFlags.Contains(obj.EciFlag))
                                    {
                                        //Do normal process payment
                                        //3D SECURED
                                        paymentReturn = Methods.ProceedTransaction(ihotspotid.ToString(), macaddress, "1.1.1", package, name_on_card, email, card_number, card_exp_month, card_exp_year, cvv, mobile, cell_country_code, obj.Cavv, obj.Xid, merchantRef, true);
                                    }
                                    else
                                    {
                                        Methods.updatePurchaseAction(ihotspotid.ToString(), macaddress, "1.1.1", "obj.SignatureVerification=" + obj.SignatureVerification, "Transaction Failed from 3D secure page.", "F", MD, merchantRef);
                                        paymentReturn = new PaymentReturn()
                                        {
                                            PaymentSuccess = false,
                                            ErrorMessage = "The 3D secure check failed, we were unable to continue with your request. Please contact support on 0861 HOTSPOT (0861 468 7768) or +27 11 759 7300..."
                                        };
                                    }
                                }
                                else if (obj.PAResStatus == "U")
                                {
                                    if (card_type == 4) //VISA
                                    {
                                        Methods.updatePurchaseAction(ihotspotid.ToString(), macaddress, "1.1.1", "obj.SignatureVerification=" + obj.SignatureVerification, "Transaction Failed from 3D secure page.", "F", MD, merchantRef);
                                        paymentReturn = new PaymentReturn()
                                        {
                                            PaymentSuccess = false,
                                            ErrorMessage = "The 3D secure check failed, we were unable to continue with your request. Please contact support on 0861 HOTSPOT (0861 468 7768) or +27 11 759 7300..."
                                        };
                                    }
                                    else
                                    {
                                        //Do normal process payment
                                        //3D SECURED
                                        paymentReturn = Methods.ProceedTransaction(ihotspotid.ToString(), macaddress, "1.1.1", package, name_on_card, email, card_number, card_exp_month, card_exp_year, cvv, mobile, cell_country_code, obj.Cavv, obj.Xid, merchantRef, true);
                                    }
                                }
                                else if (obj.PAResStatus == "N")
                                {
                                    Methods.updatePurchaseAction(ihotspotid.ToString(), macaddress, "1.1.1", "obj.SignatureVerification=" + obj.SignatureVerification, "Transaction Failed from 3D secure page.", "F", MD, merchantRef);
                                    paymentReturn = new PaymentReturn()
                                    {
                                        PaymentSuccess = false,
                                        ErrorMessage = "The 3D secure check failed, we were unable to continue with your request. Please contact support on 0861 HOTSPOT (0861 468 7768) or +27 11 759 7300..."
                                    };
                                }
                                else if (obj.PAResStatus == "A")
                                {
                                    var ECIFlags = new string[] { "01", "1", "06", "6" }.ToList();
                                    if (ECIFlags.Contains(obj.EciFlag))
                                    {
                                        //Do normal process payment
                                        //3D SECURED
                                        paymentReturn = Methods.ProceedTransaction(ihotspotid.ToString(), macaddress, "1.1.1", package, name_on_card, email, card_number, card_exp_month, card_exp_year, cvv, mobile, cell_country_code, obj.Cavv, obj.Xid, merchantRef, true);
                                    }
                                    else
                                    {
                                        Methods.updatePurchaseAction(ihotspotid.ToString(), macaddress, "1.1.1", "obj.SignatureVerification=" + obj.SignatureVerification, "Transaction Failed from 3D secure page.", "F", MD, merchantRef);
                                        paymentReturn = new PaymentReturn()
                                        {
                                            PaymentSuccess = false,
                                            ErrorMessage = "The 3D secure check failed, we were unable to continue with your request. Please contact support on 0861 HOTSPOT (0861 468 7768) or +27 11 759 7300..."
                                        };
                                    }
                                }
                                else
                                {
                                    Methods.updatePurchaseAction(ihotspotid.ToString(), macaddress, "1.1.1", "obj.SignatureVerification=" + obj.SignatureVerification, "Transaction Failed from 3D secure page.", "F", MD, merchantRef);
                                    paymentReturn = new PaymentReturn()
                                    {
                                        PaymentSuccess = false,
                                        ErrorMessage = "The 3D secure check failed, we were unable to continue with your request. Please contact support on 0861 HOTSPOT (0861 468 7768) or +27 11 759 7300..."
                                    };
                                }
                            }
                            else
                            {
                                Methods.updatePurchaseAction(ihotspotid.ToString(), macaddress, "1.1.1", "obj.SignatureVerification=" + obj.SignatureVerification, "Transaction Failed from 3D secure page.", "F", MD, merchantRef);
                                paymentReturn = new PaymentReturn()
                                {
                                    PaymentSuccess = false,
                                    ErrorMessage = "The 3D secure check failed, we were unable to continue with your request. Please contact support on 0861 HOTSPOT (0861 468 7768) or +27 11 759 7300..."
                                };
                            }
                        }
                        else
                        {
                            Methods.logPurchaseAction(ihotspotid.ToString(),
                                                      macaddress,
                                                      "1.1.1",
                                                      name_on_card, //strName
                                                      email, //strEmail
                                                      staticFunctions.hideCardNumber(card_number.Trim()), //strCardNum
                                                      package.displayPrice, //strAmount
                                                      package_id, //PackageId
                                                      "3D Secure Redirection had no MD or PaRes", //strError
                                                      "Purchase attempt", //strDescription
                                                      "", //strURL
                                                      "", //strEnrolled
                                                      "N", //strCompleted
                                                      MD, //strTransId
                                                      PaRes, //strPares
                                                      merchantRef); //strMerchantRef

                            Methods.updatePurchaseAction(ihotspotid.ToString(), macaddress, "1.1.1", "MerchantReference=" + merchantRef, "Transaction Failed from 3D secure page.", "F", MD, merchantRef);
                            paymentReturn = new PaymentReturn()
                            {
                                PaymentSuccess = false,
                                ErrorMessage = "The 3D secure check failed, we were unable to continue with your request. Please contact support on 0861 HOTSPOT (0861 468 7768) or +27 11 759 7300..."
                            };
                        }
                    }
                    else
                    {
                        Methods.logPurchaseAction(ihotspotid.ToString(),
                                                  macaddress,
                                                  "1.1.1",
                                                  name_on_card, //strName
                                                  email, //strEmail
                                                  staticFunctions.hideCardNumber(card_number.Trim()), //strCardNum
                                                  package.displayPrice, //strAmount
                                                  package_id, //PackageId
                                                  "", //strError
                                                  "Purchase attempt", //strDescription
                                                  "", //strURL
                                                  "", //strEnrolled
                                                  "N", //strCompleted
                                                  "", //strTransId
                                                  "", //strPares
                                                  merchantRef); //strMerchantRef

                        //Do normal process payment
                        //NON-3D SECURED
                        paymentReturn = Methods.ProceedTransaction(ihotspotid.ToString(), macaddress, "1.1.1", package, name_on_card, email, card_number, card_exp_month, card_exp_year, cvv, mobile, cell_country_code, "", "", merchantRef, false);
                    }
                }
                else
                {
                    paymentReturn = new PaymentReturn()
                    {
                        PaymentSuccess = false,
                        ErrorMessage = "Invalid package_id"
                    };
                }

                if (paymentReturn != null && paymentReturn.PaymentSuccess)
                {
                    link_existing_credentials(api_key, user_id, paymentReturn.Username, paymentReturn.Password, "1");

                    return new ReturnThreeDSecurePayment() { Success = true, Message = (paymentReturn.ErrorMessage ?? ""), PaymentReturn = paymentReturn };
                }
                else
                {
                    return new ReturnThreeDSecurePayment() { Success = false, Message = (paymentReturn.ErrorMessage ?? ""), PaymentReturn = paymentReturn };
                }
            }
            catch (Exception e)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "ThreeDSecurePayment", e.Message, (e.StackTrace ?? "") + "\nInnerException: " + Newtonsoft.Json.JsonConvert.SerializeObject((e.InnerException ?? new Exception(""))), Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key, user_id = user_id, macaddress = macaddress, email = email, mobile = mobile, cell_country_code = cell_country_code, package_id = package_id, card_number = card_number, cvv = cvv, name_on_card = name_on_card, card_exp_month = card_exp_month, card_exp_year = card_exp_year, card_type = card_type, merchantRef = merchantRef, directTransaction = directTransaction, MD = MD, PaRes = PaRes }));

                return new ReturnThreeDSecurePayment() { Success = false, Message = "Error: " + e.Message };
            }
        }

        //SecureDirectPayment
        public ReturnThreeDSecurePayment SecureDirectPayment(string api_key, string user_id, string email, string mobile, string package_id, string card_number, string cvv, string name_on_card, string card_exp_month, string card_exp_year)
        {
            int ihotspotid = CheckAPIKey(api_key);
            if (ihotspotid == -2)
            {
                return new ReturnThreeDSecurePayment() { Success = false, Message = ReinstallApp };
            }
            else if (ihotspotid == -1)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "SecureDirectPayment", InvalidApiKey, "", Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key, user_id = user_id, email = email, mobile = mobile, package_id = package_id, card_number = card_number, cvv = cvv, name_on_card = name_on_card, card_exp_month = card_exp_month, card_exp_year = card_exp_year }));

                return new ReturnThreeDSecurePayment() { Success = false, Message = InvalidApiKey, PaymentReturn = null };
            }

            try
            {
                var paymentReturn = new PaymentReturn();

                if (!string.IsNullOrEmpty(package_id))
                {
                    //Get PackageDetail
                    var package = Methods.getPackageDetail(package_id);
                    if (package != null)
                    {
                        var merchantRef = "";
                        using (var ivi = new iVeriInterface.iVeriInterface())
                            merchantRef = ivi.generateMerchantReference(System.Configuration.ConfigurationManager.AppSettings["iVeri_Auth_Code"], bool.Parse(System.Configuration.ConfigurationManager.AppSettings["test_mode"]));

                        Methods.logPurchaseAction(ihotspotid.ToString(),
                                                  "00:00:00:00:00:00",
                                                  "1.1.1",
                                                  name_on_card, //strName
                                                  email, //strEmail
                                                  staticFunctions.hideCardNumber(card_number.Trim()), //strCardNum
                                                  package.displayPrice, //strAmount
                                                  package_id, //PackageId
                                                  "", //strError
                                                  "Purchase attempt", //strDescription
                                                  "", //strURL
                                                  "", //strEnrolled
                                                  "N", //strCompleted
                                                  "", //strTransId
                                                  "", //strPares
                                                  merchantRef); //strMerchantRef

                        //Do normal process payment
                        //NON-3D SECURED
                        paymentReturn = Methods.ProceedTransaction(ihotspotid.ToString(), "00:00:00:00:00:00", "1.1.1", package, name_on_card, email, card_number, card_exp_month, card_exp_year, cvv, mobile, "", "", "", merchantRef, false);

                        if (paymentReturn.PaymentSuccess)
                        {
                            link_existing_credentials(api_key, user_id, paymentReturn.Username, paymentReturn.Password, "1");

                            Methods.AddMobileUserAction(user_id, "10");

                            return new ReturnThreeDSecurePayment() { Success = true, Message = (paymentReturn.ErrorMessage ?? ""), PaymentReturn = paymentReturn };
                        }
                        else
                        {
                            return new ReturnThreeDSecurePayment() { Success = false, Message = (paymentReturn.ErrorMessage ?? ""), PaymentReturn = paymentReturn };
                        }
                    }
                    else
                    {
                        paymentReturn = new PaymentReturn()
                        {
                            PaymentSuccess = false,
                            ErrorMessage = "Invalid package_id"
                        };

                        return new ReturnThreeDSecurePayment() { Success = false, Message = paymentReturn.ErrorMessage, PaymentReturn = paymentReturn };
                    }
                }
                else
                {
                    paymentReturn = new PaymentReturn()
                    {
                        PaymentSuccess = false,
                        ErrorMessage = "Invalid package_id"
                    };

                    return new ReturnThreeDSecurePayment() { Success = false, Message = paymentReturn.ErrorMessage, PaymentReturn = paymentReturn };
                }
            }
            catch (Exception e)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "SecureDirectPayment", e.Message, (e.StackTrace ?? "") + "\nInnerException: " + Newtonsoft.Json.JsonConvert.SerializeObject((e.InnerException ?? new Exception(""))), Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key, user_id = user_id, email = email, mobile = mobile, package_id = package_id, card_number = card_number, cvv = cvv, name_on_card = name_on_card, card_exp_month = card_exp_month, card_exp_year = card_exp_year }));

                return new ReturnThreeDSecurePayment() { Success = false, Message = "Error: " + e.Message };
            }
        }

        //16 Get Info Button
        public ReturnInfoButton getInfoButton(string api_key)
        {
            int ihotspotid = CheckAPIKey(api_key);
            if (ihotspotid == -2)
            {
                return new ReturnInfoButton() { Success = false, Message = ReinstallApp };
            }
            else if (ihotspotid == -1)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "getInfoButton", InvalidApiKey, "", Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key }));

                return new ReturnInfoButton() { Success = false, Message = InvalidApiKey, InfoButton = null };
            }

            try
            {
                using (var db = new RadiusdbDB())
                {
                    var InfoButton = db.App_Info_Buttons.OrderByDescending(n => n.Id).FirstOrDefault();
                    if (InfoButton != null)
                    {
                        var Title = (InfoButton.Title ?? "").ToString().Trim();
                        var Url = (InfoButton.Url ?? "").ToString().Trim();

                        return new ReturnInfoButton() { Success = true, Message = "", InfoButton = new InfoButton() { title = Title, url = Url } };
                    }
                    else
                    {
                        return new ReturnInfoButton() { Success = false, Message = "There are no info button data", InfoButton = null };
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "getInfoButton", e.Message, (e.StackTrace ?? "") + "\nInnerException: " + Newtonsoft.Json.JsonConvert.SerializeObject((e.InnerException ?? new Exception(""))), Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key }));

                return new ReturnInfoButton() { Success = false, Message = "Error: " + e.Message, InfoButton = null };
            }
        }

        //17 Update Package Ranking
        public ReturnDefault updatePackageRanking(string api_key, string user_id, string package_ids, string package_ranks)
        {
            int ihotspotid = CheckAPIKey(api_key);
            if (ihotspotid == -2)
            {
                return new ReturnDefault() { Success = false, Message = ReinstallApp };
            }
            else if (ihotspotid == -1)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "updatePackageRanking", InvalidApiKey, "", Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key, user_id = user_id, package_ids = package_ids, package_ranks = package_ranks }));

                return new ReturnDefault() { Success = false, Message = InvalidApiKey };
            }

            try
            {
                using (var db = new RadiusdbDB())
                {
                    var userpackages = db.Wifi_Credentials.Where(n => n.Ao_Account_Id == Convert.ToInt32(user_id)).ToList();

                    var packageIdArray = package_ids.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    var packageRankArray = package_ranks.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);

                    for (int i = 0; i < packageIdArray.Length; i++)
                    {
                        var currentPkg = userpackages.Where(n => n.Id == Convert.ToInt32(packageIdArray[i])).FirstOrDefault();
                        currentPkg.Use_Rank = Convert.ToInt32(packageRankArray[i]);
                    }

                    userpackages.Where(n => !packageIdArray.Contains(n.Id.ToString())).ToList().ForEach(n =>
                    {
                        n.Use_Rank = 99;
                    });

                    db.SubmitChanges();

                    Methods.AddMobileUserAction(user_id, "11");

                    return new ReturnDefault() { Success = true, Message = "Package ranking updated successfully" };
                }
            }
            catch (Exception e)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "updatePackageRanking", e.Message, (e.StackTrace ?? "") + "\nInnerException: " + Newtonsoft.Json.JsonConvert.SerializeObject((e.InnerException ?? new Exception(""))), Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key, user_id = user_id, package_ids = package_ids, package_ranks = package_ranks }));

                return new ReturnDefault() { Success = false, Message = "Error: " + e.Message };
            }
        }

        //18 Get SSIDs
        public ReturnSSIDList getSSIDs(string api_key, string ssids)
        {
            int ihotspotid = CheckAPIKey(api_key);
            if (ihotspotid == -2)
            {
                return new ReturnSSIDList() { Success = false, Message = ReinstallApp };
            }
            else if (ihotspotid == -1)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "getSSIDs", InvalidApiKey, "", Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key, ssids = ssids }));

                return new ReturnSSIDList() { Success = false, Message = InvalidApiKey, SSIDList = null };
            }

            try
            {
                var splitList = ssids.Split(',');
                if (splitList.Count() == 0) return new ReturnSSIDList() { Success = true, Message = "", SSIDList = new List<string>() };

                var ssidsreturn = new List<string>();
                using (var db = new RadiusdbDB())
                {
                    var ipasslocs = (from i in db.Ipass_Locations
                                     where splitList.Contains(i.Ssid)
                                     select i.Ssid).Distinct();
                    var aolocs = (from h in db.Hotspots
                                  where splitList.Contains(h.Ssid)
                                  select h.Ssid).Distinct();

                    ssidsreturn = ipasslocs.Union(aolocs).Distinct().ToList();
                }

                //VAST Query Service
                try
                {
                    var parameters = new NameValueCollection()
                    {
                        ["api_key"] = "6942sa8z-7ao3-46ws-vast-0qu3ry10svcf",
                        ["ssids"] = ssids
                    };
                    var url = "https://portal.vast.services/VASTQueryService/Methods.svc/getSSIDs";
                    var ret = staticFunctions.RestGet(url, parameters);
                    var retssids = Newtonsoft.Json.JsonConvert.DeserializeObject<ReturnSSIDList>(ret);
                    if (retssids.Success && retssids.SSIDList.Count > 0)
                    {
                        ssidsreturn = ssidsreturn.Union(retssids.SSIDList).Distinct().ToList();
                    }
                }
                catch (Exception e2)
                {
                }

                return new ReturnSSIDList() { Success = true, Message = "", SSIDList = ssidsreturn };
            }
            catch (Exception e)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "getSSIDs", e.Message, (e.StackTrace ?? "") + "\nInnerException: " + Newtonsoft.Json.JsonConvert.SerializeObject((e.InnerException ?? new Exception(""))), Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key, ssids = ssids }));

                return new ReturnSSIDList() { Success = false, Message = "Error: " + e.Message, SSIDList = null };
            }
        }

        //19 Get Hotspot Locations International
        public ReturnHotspotMarkers getHotspotLocationsInternational(string api_key, string lata, string latb, string lnga, string lngb, string userlat, string userlng, string centerlat, string centerlng, string limit)
        {
            int ihotspotid = CheckAPIKey(api_key);
            if (ihotspotid == -2)
            {
                return new ReturnHotspotMarkers() { Success = false, Message = ReinstallApp };
            }
            else if (ihotspotid == -1)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "getHotspotLocationsInternational", InvalidApiKey, "", Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key, lata = lata, latb = latb, lnga = lnga, lngb = lngb, userlat = userlat, userlng = userlng, centerlat = centerlat, centerlng = centerlng, limit = limit }));

                return new ReturnHotspotMarkers() { Success = false, Message = InvalidApiKey, HotspotMarkers = null };
            }

            try
            {
                var duserlat = Convert.ToDouble(userlat.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
                var duserlng = Convert.ToDouble(userlng.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);

                var dcenterlat = Convert.ToDouble(centerlat.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
                var dcenterlng = Convert.ToDouble(centerlng.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);

                var dLatA = Convert.ToDouble(lata.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
                var dLatB = Convert.ToDouble(latb.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
                var dLngA = Convert.ToDouble(lnga.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
                var dLngB = Convert.ToDouble(lngb.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);

                //Methods.GetIPassHotspots(dcenterlat, dcenterlng);

                var struserlat = duserlat.ToString("0.000000").Replace(",", ".");
                var struserlng = duserlng.ToString("0.000000").Replace(",", ".");

                var strcenterlat = dcenterlat.ToString("0.000000").Replace(",", ".");
                var strcenterlng = dcenterlng.ToString("0.000000").Replace(",", ".");

                var latA = (dLatA > dLatB ? dLatB : dLatA).ToString().Replace(",", ".");
                var latB = (dLatA > dLatB ? dLatA : dLatB).ToString().Replace(",", ".");
                var lngA = (dLngA > dLngB ? dLngB : dLngA).ToString().Replace(",", ".");
                var lngB = (dLngA > dLngB ? dLngA : dLngB).ToString().Replace(",", ".");

                var ilimit = Convert.ToInt32(limit ?? "0");
                ilimit = ilimit == 0 || ilimit > 2500 ? 2500 : ilimit;

                var cmd = @"select  a.lat,
                                    a.lng,
                                    a.data,
                                    a.options,
                                    a.superwifi,
                                    a.international,
                                    SQRT(POW(111.2 * (a.lat - '" + struserlat + @"'), 2) + POW(111.2 * ('" + struserlng + @"' - a.lng) * COS(a.lat / 57.2958), 2)) AS distanceinkilometers,
                                    SQRT(POW(111.2 * (a.lat - '" + strcenterlat + @"'), 2) + POW(111.2 * ('" + strcenterlng + @"' - a.lng) * COS(a.lat / 57.2958), 2)) AS orderdistance
                            from (  select  d.lat,
                                            d.lng,
                                            d.data,
                                            null as options,
                                            d.superwifi,
                                            d.international
                                    from (select CAST(trim(replace(regexp_replace(h.gps_y, '[^0-9.,-]+', '', 'g'),',','.')) as float) as lat,
		                                            CAST(trim(replace(regexp_replace(h.gps_x, '[^0-9.,-]+', '', 'g'),',','.')) as float) as lng,
		                                            h.locationname as data,
		                                            case when hs.id is null then 0 else 1 end as superwifi,
                                                    0 as international --false
                                            from hotspot as h
                                            left join (select h1.id from hotspot as h1 join location_type as lt1 on h1.location_type_id = lt1.id where h1.backhaul_type_id in (11, 12, 13, 14, 18)) as hs on h.id = hs.id
                                            where length(trim(replace(regexp_replace(h.gps_y, '[^0-9.,-]+', '', 'g'),',','.'))) > 0
                                            and length(trim(replace(regexp_replace(h.gps_x, '[^0-9.,-]+', '', 'g'),',','.'))) > 0
                                            and h.live = 'Y') as d
                                    where d.lat <> 0
                                    and d.lng <> 0
                                    and d.lat between '" + latA + "' and '" + latB + @"'
                                    and d.lng between '" + lngA + "' and '" + lngB + @"'
                                    union all
                                    select 	ipl.lat::float,
		                                    ipl.lng::float,
                                            ipl.site_name,
                                            null as options,
                                            0 as superwifi,
                                            1 as international
                                    from ipass_locations as ipl
                                    where ipl.customer_id not in ('1034680','900491','1041248')
                                    and ipl.lat::float <> 0
                                    and ipl.lng::float <> 0
                                    and ipl.lat::float between '" + latA + "' and '" + latB + @"'
                                    and ipl.lng::float between '" + lngA + "' and '" + lngB + @"') as a
                            order by 8
                            limit " + ilimit;

                var markers = new List<HotspotMarker>();
                using (var db = new RadiusdbDB())
                {
                    markers = db.ExecuteQuery<HotspotMarker>(cmd).ToList();
                }

                if (markers != null && markers.Count > 0)
                {
                    return new ReturnHotspotMarkers() { Success = true, Message = "", HotspotMarkers = markers };
                }
                else
                {
                    return new ReturnHotspotMarkers() { Success = false, Message = "No markers found", HotspotMarkers = null };
                }
            }
            catch (Exception e)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "getHotspotLocationsInternational", e.Message, (e.StackTrace ?? "") + "\nInnerException: " + Newtonsoft.Json.JsonConvert.SerializeObject((e.InnerException ?? new Exception(""))), Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key, lata = lata, latb = latb, lnga = lnga, lngb = lngb, userlat = userlat, userlng = userlng, centerlat = centerlat, centerlng = centerlng, limit = limit }));

                return new ReturnHotspotMarkers() { Success = false, Message = "Error: " + e.Message, HotspotMarkers = null };
            }
        }

        #region Accuris
        //20 LoginAccuris
        public ReturnDefault LoginAccuris(string api_key, string username, string password, string sessionid)
        {
            int ihotspotid = CheckAPIKey(api_key);
            if (ihotspotid == -2)
            {
                return new ReturnDefault() { Success = false, Message = ReinstallApp };
            }
            else if (ihotspotid == -1)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "LoginAccuris", InvalidApiKey, "", Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key, username = username, password = password, sessionid = sessionid }));

                return new ReturnDefault() { Success = false, Message = InvalidApiKey };
            }

            try
            {
                using (var hs = new HotservTools.HotspotTools())
                {
                    var authresp = hs.LoginAccuris(username, password, sessionid);
                    if (authresp?.AuthenticateResponse?.statusCode == 200)
                    {
                        return new ReturnDefault() { Success = true, Message = "Login successful" };
                    }
                    else
                    {
                        Logger.Log("AlwaysOnMobileService.svc.cs", "LoginAccuris", "Login Failed", (authresp?.AuthenticateResponse?.statusCode ?? 99) + ": " + (authresp?.AuthenticateResponse?.reason ?? "Unable to login"), Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key, username = username, password = password, sessionid = sessionid }));

                        return new ReturnDefault() { Success = false, Message = (authresp?.AuthenticateResponse?.statusCode ?? 99) + ": " + (authresp?.AuthenticateResponse?.reason ?? "Unable to login") };
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "LoginAccuris", ex.Message, (ex.StackTrace ?? "") + "\nInnerException: " + Newtonsoft.Json.JsonConvert.SerializeObject((ex.InnerException ?? new Exception(""))), Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key, username = username, password = password, sessionid = sessionid }));

                return new ReturnDefault() { Success = false, Message = "99: " + ex.Message };
            }
        }

        //21 LogoutAccuris
        public ReturnDefault LogoutAccuris(string api_key, string sessionid)
        {
            int ihotspotid = CheckAPIKey(api_key);
            if (ihotspotid == -2)
            {
                return new ReturnDefault() { Success = false, Message = ReinstallApp };
            }
            else if (ihotspotid == -1)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "LogoutAccuris", InvalidApiKey, "", Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key, sessionid = sessionid }));

                return new ReturnDefault() { Success = false, Message = InvalidApiKey };
            }

            try
            {
                using (var hs = new HotservTools.HotspotTools())
                {
                    var logoutresp = hs.LogoutAccuris(sessionid);
                    if (logoutresp?.LogoutResponse?.statusCode == 200)
                    {
                        return new ReturnDefault() { Success = true, Message = "Logout Successful" };
                    }
                    else
                    {
                        Logger.Log("AlwaysOnMobileService.svc.cs", "LogoutAccuris", "Logout Failed", (logoutresp?.LogoutResponse?.statusCode ?? 99) + ": " + (logoutresp?.LogoutResponse?.reason ?? "Unable to logout"), Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key, sessionid = sessionid }));

                        return new ReturnDefault() { Success = false, Message = (logoutresp?.LogoutResponse?.statusCode ?? 99) + ": " + (logoutresp?.LogoutResponse?.reason ?? "Unable to logout") };
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "LogoutAccuris", e.Message, (e.StackTrace ?? "") + "\nInnerException: " + Newtonsoft.Json.JsonConvert.SerializeObject((e.InnerException ?? new Exception(""))), Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key, sessionid = sessionid }));

                return new ReturnDefault() { Success = false, Message = "98: " + e.Message };
            }
        }
        #endregion Accuris

        //22 linkSingleCode
        public ReturnDefault linkSingleCode(string api_key, string user_id, string code)
        {
            int ihotspotid = CheckAPIKey(api_key);
            if (ihotspotid == -2)
            {
                return new ReturnDefault() { Success = false, Message = ReinstallApp };
            }
            else if (ihotspotid == -1)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "linkSingleCode", InvalidApiKey, "", Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key, code = code, user_id = user_id }));

                return new ReturnDefault() { Success = false, Message = InvalidApiKey };
            }

            try
            {
                using (var hs = new HotservTools.HotspotTools())
                {
                    var single_code = hs.linkSingleCode(code, user_id);
                    return new ReturnDefault() { Success = single_code.success, Message = single_code.message };
                }
            }
            catch (Exception ex)
            {
                Logger.Log("AlwaysOnMobileService.svc.cs", "linkSingleCode", ex.Message, (ex.StackTrace ?? "") + "\nInnerException: " + Newtonsoft.Json.JsonConvert.SerializeObject((ex.InnerException ?? new Exception(""))), Newtonsoft.Json.JsonConvert.SerializeObject(new { api_key = api_key, code = code, user_id = user_id }));

                return new ReturnDefault() { Success = false, Message = "99: " + ex.Message };
            }
        }


        private int CheckAPIKey(string api_key)
        {
            if (api_key == System.Configuration.ConfigurationManager.AppSettings["api_key"])
            {
                //return HotspotIDAndroid; //Old Key
                return -2; //when this key is not valid anymore. 
            }
            else if (api_key == System.Configuration.ConfigurationManager.AppSettings["api_key_android"])
            {
                return HotspotIDAndroid; //New Android Key
            }
            else if (api_key == System.Configuration.ConfigurationManager.AppSettings["api_key_ios"])
            {
                return HotspotIDiOS; //New iOS Key
            }
            else
            {
                return -1; //Invalid Key
            }
        }
    }
}