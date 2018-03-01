using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace AlwaysOnMobileService
{
    public class UserPackage
    {
        public int id { get; set; }
        public string login_username { get; set; }
        public string login_password { get; set; }
        public string active { get; set; }
        public string expiry_date { get; set; }
        public string credential_desc { get; set; }
        public string accounttype_desc { get; set; }
        public string group_desc { get; set; }
        public string group_name { get; set; }
        public string mac_address { get; set; }
        public string service_provider_id { get; set; }
        public string create_date { get; set; }
        public int package_id { get; set; }
        public string usageleft_percentage { get; set; }
        public string usageleft_value { get; set; }
        public int use_rank { get; set; }
    }

    public class UserProfile
    {
        public string user_id { get; set; }
        public string accountstatus_id { get; set; }
        public string country_id { get; set; }
        public string date_created { get; set; }
        public string email_enc { get; set; }
        public string login_credential { get; set; }
        public string mobile_number { get; set; }
        public string surname { get; set; }
        public string name { get; set; }
        public string title { get; set; }
    }

    public class Packageitems
    {
        public string listOrder { get; set; }
        public string id { get; set; }
        public string packageDesc { get; set; }
        public string defaultPackage { get; set; }
        public string displayPrice { get; set; }
        public string optionDesc { get; set; }
        public string iveriPrice { get; set; }
        public string packageName { get; set; }
        public string packageType { get; set; }
        public string usageDescription { get; set; }
        public string expirationHours { get; set; }
        public string expirationDays { get; set; }
        public string currency { get; set; }
        public string expiryDays { get; set; }
        public string isfromfirstlogin { get; set; }
        public string errormessage { get; set; }
        public string radgroupcheckid { get; set; }
        public AdditionalInfo additionalInfo { get; set; }
    }

    public class AdditionalInfo
    {
        public string Value { get; set; }
        public string HeaderPre { get; set; }
        public string HeaderPost { get; set; }
        public string Price { get; set; }
        public string PricePer { get; set; }
        public string Period { get; set; }
        public string ExpiryDays { get; set; }
        public string Description { get; set; }
        public string Songs { get; set; }
        public string Videos { get; set; }
        public string Voice { get; set; }
    }
    
    public class PackageGroup
    {
        public string Attribute { get; set; }
        public string Value { get; set; }
    }

    public class CompletePackageDetail
    {
        public double dblTotalValue { get; set; }
        public string lblAccountType { get; set; }
        public string lblAccountAllowance { get; set; }
        public bool boolData { get; set; }
        public string strAdditionalText { get; set; }
        public double dblTotalValueUsed { get; set; }
        public double dblBalance { get; set; }
        public string lblAccountTotalUsage { get; set; }
        public string lblAccountBalance { get; set; }
        public string lblAccountExpiration { get; set; }
    }

    public class PackageValue
    {
        public decimal Total { get; set; }
        public decimal DataTotal { get; set; }
    }

    public class UsageLeft
    {
        public double percentage { get; set; }
        public string usageleft { get; set; }
    }

    public class ThreeDSecure
    {
        public string ACSUrl { get; set; }
        public string error_message { get; set; }
        public int CardType { get; set; }
        public bool DirectTransaction { get; set; }
        public string MerchantReference { get; set; }
    }

    public class ServiceProvider
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public string List { get; set; }
        public int Unit_Cost { get; set; }
        public string Currency { get; set; }
        public string Hotspot_List { get; set; }
        public string Summary_List { get; set; }
        public string Unit_Type { get; set; }
        public string Is_Debtor_Code { get; set; }
        public string Vat_Number { get; set; }
        public string Postal_Addr1 { get; set; }
        public string Postal_Addr2 { get; set; }
        public string Postal_City { get; set; }
        public string Postal_Code { get; set; }
        public string Contact_Name { get; set; }
        public string Contact_Telno { get; set; }
        public string Contact_Fax { get; set; }
        public string Contact_Email { get; set; }
        public string List_Landingpage { get; set; }
        public string Landingpage_Desc { get; set; }
        public string Routing_Prefix { get; set; }
        public string Hosted_Uam_Url { get; set; }
        public string Hosted_Uam { get; set; }
        public int List_Order { get; set; }
        public string Per_User_Charge { get; set; }
        public string DropDownValue { get; set; }
    }

    public class ServiceProvide
    {
        public int Id { get; set; }
        public string Description { get; set; }
    }

    public class SecureRadCheck
    {
        public int Id { get; set; }
        public string Attribute { get; set; }
        public string Op { get; set; }
        public string Username { get; set; }
        public string Value { get; set; }
    }

    public class HotspotMarker
    {
        public double lat { get; set; }
        public double lng { get; set; }
        public string data { get; set; }
        public bool superwifi { get; set; }
        public bool international { get; set; }
        public double distanceinkilometers { get; set; }
    }

    public class paymentSelects
    {
        public string id { get; set; }
        public string allow_credit { get; set; }
        public string allow_pocit { get; set; }
        public string allow_room { get; set; }
        public string allow_paypal { get; set; }
        public string terminal { get; set; }
        public string verify_surname { get; set; }
        public string verify_reservation_no { get; set; }
        public string landingpage_configuration_id { get; set; }
        public string currency_symbol { get; set; }
        public string ccyears { get; set; }
        public string parameter { get; set; }
        public string value { get; set; }
        public string groupname { get; set; }
        public string package_type { get; set; }
    }

    public class PaymentReturn
    {
        public bool PaymentSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Package_Type { get; set; }
        public string Package_Bought { get; set; }
        public string Package_Bought_Expiration { get; set; }
    }

    public class TokenReturn
    {
        public bool TokenSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public string ccname { get; set; }
        public string email { get; set; }
        public string last_package { get; set; }
        public string last_price { get; set; }
        public string cctimestamp { get; set; }
        public string transaction_index { get; set; }
        public string cc_number { get; set; }
        public string expiration { get; set; }
    }

    public class Token
    {
        public string ccname { get; set; }
        public string email { get; set; }
        public string last_package { get; set; }
        public string last_price { get; set; }
        public string cctimestamp { get; set; }
        public string transaction_index { get; set; }
        public string cc_number { get; set; }
        public string expiration { get; set; }
    }

    public class UsageSummary
    {
        public string Username { get; set; }
        public string Allowance { get; set; }
        public string Type { get; set; }
        public string TotalUsage { get; set; }
        public string Balance { get; set; }
        public string Expiration { get; set; }
        public string AdditionalText { get; set; }
        public double dblTotalValue { get; set; }
        public double dblTotalValueUsed { get; set; }
        public double dblBalance { get; set; }
    }

    public class UsageDetail
    {
        public string username { get; set; }
        public decimal timeused { get; set; }
        public DateTime startime { get; set; }
        public DateTime endtime { get; set; }
        public string inputkb { get; set; }
        public string outputkb { get; set; }
        public decimal totalkbsec { get; set; }
        public string areadescription { get; set; }
        public string ipaddr { get; set; }
        public string mac { get; set; }
    }

    public class InfoButton
    {
        public string title { get; set; }
        public string url { get; set; }
    }

    public class ipObject
    {
        public List<iphotspot> hotspots { get; set; }
        public ipCityInfo cityinfo { get; set; }
    }

    public class iphotspot
    {
        public string SSID { get; set; }
        public string PopID { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Region { get; set; }
        public string Country { get; set; }
        public string CountryCode { get; set; }
        public string State { get; set; }
        public string City { get; set; }
        public string SiteAddress { get; set; }
        public string SiteName { get; set; }
        public string MapSearchName { get; set; }
        public string SiteType { get; set; }
        public string MediaAccessType { get; set; }
        public string ProviderName { get; set; }
        public string CustomerID { get; set; }
        public string SiteDescription { get; set; }
        public string MACAddress { get; set; }
        public int zomgid { get; set; }
        public double distance { get; set; }
        public string UID { get; set; }
        public string ImageUrl { get; set; }
    }

    public class oPackageDetail : IDisposable
    {
        public double dblPercentageLeft { get; set; }
        public double dblTotalValue { get; set; }
        public string lblAccountType { get; set; }
        public string lblAccountAllowance { get; set; }
        public bool boolData { get; set; }
        public string strAdditionalText { get; set; }
        public double dblTotalValueUsed { get; set; }
        public double dblBalance { get; set; }
        public string lblAccountTotalUsage { get; set; }
        public string lblAccountBalance { get; set; }
        public decimal PackageTotal { get; set; }
        public decimal PackageDataTotal { get; set; }
        public string strUsedUp { get; set; }
        public string strExpiration { get; set; }
        public int iDaysExpireAfter { get; set; }
        public string RadcheckUserExpiration { get; set; } = "";

        public void Dispose() { }
    }

    public class ipCityInfo
    {
        public bool FeaturedCity { get; set; }
    }
}