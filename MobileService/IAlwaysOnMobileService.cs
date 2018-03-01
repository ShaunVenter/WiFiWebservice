using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace AlwaysOnMobileService
{
    [ServiceContract]
    public interface IAlwaysOnMobileService
    {
        //1 Register User
        [OperationContract]
        [WebInvoke(ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, UriTemplate = "/RegisterUser?api_key={api_key}&name={name}&surname={surname}&username={username}&password={password}&mobile={mobile}")]
        ReturnRegistration RegisterUser(string api_key, string name, string surname, string username, string password, string mobile);

        //2 Get User Profile
        [OperationContract]
        [WebInvoke(ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, UriTemplate = "/getUserProfile?api_key={api_key}&username={username}&password={password}")]
        ReturnUserProfile getUserProfile(string api_key, string username, string password);

        //3 Update User Profile
        [OperationContract]
        [WebInvoke(ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, UriTemplate = "/updateUserProfile?api_key={api_key}&user_id={user_id}&title={title}&name={name}&surname={surname}&email={email}&mobile={mobile}")]
        ReturnDefault updateUserProfile(string api_key, string user_id, string title, string name, string surname, string email, string mobile);

        //4 Get User Packages
        [OperationContract]
        [WebInvoke(ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, UriTemplate = "/getUserPackages?api_key={api_key}&user_id={user_id}&macaddress={macaddress}")]
        ReturnUserPackages getUserPackages(string api_key, string user_id, string macaddress);

        //4.5 Get User Packages
        [OperationContract]
        [WebInvoke(ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, UriTemplate = "/getUserPackagesV2?api_key={api_key}&user_id={user_id}&macaddress={macaddress}&sessionid={sessionid}")]
        ReturnUserPackages getUserPackagesV2(string api_key, string user_id, string macaddress, string sessionid);

        //5 Get available Packages to Purchase
        [OperationContract]
        [WebInvoke(ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, UriTemplate = "/getVoucherPackages?api_key={api_key}")]
        ReturnVoucherPackages getVoucherPackages(string api_key);

        //6 Link Device TO account
        [OperationContract]
        [WebInvoke(ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, UriTemplate = "/linkDevice?api_key={api_key}&user_id={user_id}&macaddress={macaddress}")]
        ReturnDefault linkDevice(string api_key, string user_id, string macaddress);

        //7 Unlink device from account
        [OperationContract]
        [WebInvoke(ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, UriTemplate = "/unlinkDevice?api_key={api_key}&user_id={user_id}&macaddress={macaddress}")]
        ReturnDefault unlinkDevice(string api_key, string user_id, string macaddress);

        //8 Link Existing Credential to account
        [OperationContract]
        [WebInvoke(ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, UriTemplate = "/link_existing_credentials?api_key={api_key}&user_id={user_id}&username={username}&password={password}&service_provider={service_provider}")]
        ReturnDefault link_existing_credentials(string api_key, string user_id, string username, string password, string service_provider);

        //9 Unlink Existing Credential to account
        [OperationContract]
        [WebInvoke(ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, UriTemplate = "/unlink_existing_credentials?api_key={api_key}&user_package_id={user_package_id}")]
        ReturnDefault unlink_existing_credentials(string api_key, int user_package_id);

        //10 Get Service Providers
        [OperationContract]
        [WebInvoke(ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, UriTemplate = "/getServiceProviders?api_key={api_key}")]
        ReturnServiceProviders getServiceProviders(string api_key);

        //11 Get Hotspot Locations
        [OperationContract]
        [WebInvoke(ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, UriTemplate = "/getHotspotLocations?api_key={api_key}&lata={lata}&latb={latb}&lnga={lnga}&lngb={lngb}")]
        ReturnHotspotMarkers getHotspotLocations(string api_key, string lata, string latb, string lnga, string lngb);

        //12 Usage Summary
        [OperationContract]
        [WebInvoke(ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, UriTemplate = "/getUsageSummary?api_key={api_key}&username={username}")]
        ReturnUsageSummary getUsageSummary(string api_key, string username);

        //13 Usage Detailed
        [OperationContract]
        [WebInvoke(ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, UriTemplate = "/getUsageDetailed?api_key={api_key}&username={username}&fromDate={fromDate}&toDate={toDate}")]
        ReturnUsageDetailed getUsageDetailed(string api_key, string username, string fromDate, string toDate);

        //14 Forgot Password
        [OperationContract]
        [WebInvoke(ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, UriTemplate = "/forgotPassword?api_key={api_key}&email={email}&mobile={mobile}")]
        ReturnDefault forgotPassword(string api_key, string email, string mobile);

        //15 Is Super WiFi
        [OperationContract]
        [WebInvoke(ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, UriTemplate = "/is_superwifi?api_key={api_key}&ip_address={ip_address}")]
        ReturnSuperWiFi is_superwifi(string api_key, string ip_address);

        //3DSecure Token Check
        [OperationContract]
        [WebInvoke(ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, UriTemplate = "/ThreeDSecureTokenCheck?api_key={api_key}&user_id={user_id}")]
        ReturnToken ThreeDSecureTokenCheck(string api_key, string user_id);

        //3DSecure Check
        [OperationContract]
        [WebInvoke(ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, UriTemplate = "/ThreeDSecureCheck?api_key={api_key}&email={email}&package_id={package_id}&card_number={card_number}&card_exp_month={card_exp_month}&card_exp_year={card_exp_year}&returnURL={returnURL}")]
        ReturnThreeDSecure ThreeDSecureCheck(string api_key, string email, string package_id, string card_number, string card_exp_month, string card_exp_year, string returnURL);

        //3DSecure Payment
        [OperationContract]
        [WebInvoke(ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, UriTemplate = "/ThreeDSecurePayment?api_key={api_key}&user_id={user_id}&macaddress={macaddress}&email={email}&mobile={mobile}&cell_country_code={cell_country_code}&package_id={package_id}&card_number={card_number}&cvv={cvv}&name_on_card={name_on_card}&card_exp_month={card_exp_month}&card_exp_year={card_exp_year}&card_type={card_type}&merchantRef={merchantRef}&directTransaction={directTransaction}&MD={MD}&PaRes={PaRes}")]
        ReturnThreeDSecurePayment ThreeDSecurePayment(string api_key, string user_id, string macaddress, string email, string mobile, string cell_country_code, string package_id, string card_number, string cvv, string name_on_card, string card_exp_month, string card_exp_year, int card_type, string merchantRef, bool directTransaction, string MD, string PaRes);

        //SecureDirectPayment
        [OperationContract]
        [WebInvoke(ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, UriTemplate = "/SecureDirectPayment?api_key={api_key}&user_id={user_id}&email={email}&mobile={mobile}&package_id={package_id}&card_number={card_number}&cvv={cvv}&name_on_card={name_on_card}&card_exp_month={card_exp_month}&card_exp_year={card_exp_year}")]
        ReturnThreeDSecurePayment SecureDirectPayment(string api_key, string user_id, string email, string mobile, string package_id, string card_number, string cvv, string name_on_card, string card_exp_month, string card_exp_year);

        //16 Get Info Button
        [OperationContract]
        [WebInvoke(ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, UriTemplate = "/getInfoButton?api_key={api_key}")]
        ReturnInfoButton getInfoButton(string api_key);

        //17 Update Package Ranking
        [OperationContract]
        [WebInvoke(ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, UriTemplate = "/updatePackageRanking?api_key={api_key}&user_id={user_id}&package_ids={package_ids}&package_ranks={package_ranks}")]
        ReturnDefault updatePackageRanking(string api_key, string user_id, string package_ids, string package_ranks);

        //18 Get SSIDs
        [OperationContract]
        [WebInvoke(ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, UriTemplate = "/getSSIDs?api_key={api_key}&ssids={ssids}")]
        ReturnSSIDList getSSIDs(string api_key, string ssids);

        //19 Get Hotspot Locations International
        [OperationContract]
        [WebInvoke(ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, UriTemplate = "/getHotspotLocationsInternational?api_key={api_key}&lata={lata}&latb={latb}&lnga={lnga}&lngb={lngb}&userlat={userlat}&userlng={userlng}&centerlat={centerlat}&centerlng={centerlng}&limit={limit}")]
        ReturnHotspotMarkers getHotspotLocationsInternational(string api_key, string lata, string latb, string lnga, string lngb, string userlat, string userlng, string centerlat, string centerlng, string limit);

        #region Accuris
        //20 LoginAccuris
        [OperationContract]
        [WebInvoke(ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, UriTemplate = "/LoginAccuris?api_key={api_key}&username={username}&password={password}&sessionid={sessionid}")]
        ReturnDefault LoginAccuris(string api_key, string username, string password, string sessionid);

        //21 LogoutAccuris
        [OperationContract]
        [WebInvoke(ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, UriTemplate = "/LogoutAccuris?api_key={api_key}&sessionid={sessionid}")]
        ReturnDefault LogoutAccuris(string api_key, string sessionid);
        #endregion Accuris

        //22 Link Single Code
        [OperationContract]
        [WebInvoke(ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, UriTemplate = "/linkSingleCode?api_key={api_key}&user_id={user_id}&code={code}")]
        ReturnDefault linkSingleCode(string api_key, string user_id, string code);
    }
}