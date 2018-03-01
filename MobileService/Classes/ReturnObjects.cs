using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace AlwaysOnMobileService
{
    public class ReturnDefault
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    public class ReturnRegistration
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        //public string user_id { get; set; }
        public UserProfile UserProfile { get; set; }
    }

    public class ReturnUserPackages
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<UserPackage> UserPackages { get; set; }
    }

    public class ReturnUserProfile
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public UserProfile UserProfile { get; set; }
        public bool IsPackageCredentials { get; set; } = false;
        public bool IsPackageLinked { get; set; } = false;
        public string PackageLinkedAccount { get; set; } = "";
    }

    public class ReturnVoucherPackages
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<Packageitems> PackageItems { get; set; }
    }

    public class ReturnServiceProviders
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<ServiceProvide> ServiceProviders { get; set; }
    }

    public class ReturnHotspotMarkers
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<HotspotMarker> HotspotMarkers { get; set; }
    }

    public class ReturnThreeDSecure
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public ThreeDSecure ThreeDSecure { get; set; }
    }

    public class ReturnThreeDSecurePayment
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public PaymentReturn PaymentReturn { get; set; }
    }

    public class ReturnToken
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public TokenReturn TokenReturn { get; set; }
    }

    public class ReturnUsageSummary
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public UsageSummary UsageSummary { get; set; }
    }

    public class ReturnUsageDetailed
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<UsageDetail> UsageDetail { get; set; }
    }

    public class ReturnSuperWiFi
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public bool issuperwifi { get; set; }
    }

    public class ReturnInfoButton
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public InfoButton InfoButton { get; set; }
    }

    public class ReturnSSIDList
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<string> SSIDList { get; set; }
    }
}