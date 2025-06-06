using System;
using System.IO;
using System.Net;

using Unity.Plastic.Newtonsoft.Json;

using Codice.Client.Common.WebApi;
using Codice.CM.Common;
using Codice.LogWrapper;
using PlasticGui.WebApi.Responses;

namespace Unity.PlasticSCM.Editor.WebApi
{
    internal static class WebRestApiClient
    {
        internal static class PlasticScm
        {
            internal static TokenExchangeResponse TokenExchange(string unityAccessToken)
            {
                Uri endpoint = mWebApiUris.GetFullUri(
                    string.Format(TokenExchangeEndpoint, unityAccessToken));

                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(endpoint);
                    request.Method = "GET";
                    request.ContentType = "application/json";
                    return GetResponse<TokenExchangeResponse>(request);
                }
                catch (Exception ex)
                {
                    mLog.ErrorFormat(
                        "Unable to exchange tokens '{0}': {1}",
                        endpoint.ToString(), ex.Message);

                    mLog.DebugFormat(
                        "StackTrace:{0}{1}",
                        Environment.NewLine, ex.StackTrace);

                    return null;
                }
            }

            internal static NewVersionResponse GetLastVersion(Edition plasticEdition)
            {
                Uri endpoint = mWebApiUris.GetFullUri(
                        WebApiEndpoints.LastVersion.NewVersion,
                        "9.0.0.0",
                        WebApiEndpoints.LastVersion.GetEditionString(plasticEdition),
                        WebApiEndpoints.LastVersion.GetPlatformString());

                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(endpoint);
                    request.Method = "GET";
                    request.ContentType = "application/json";

                    return GetResponse<NewVersionResponse>(request);
                }
                catch (Exception ex)
                {
                    mLog.ErrorFormat(
                        "Unable to retrieve new versions from '{0}': {1}",
                        endpoint.ToString(), ex.Message);

                    mLog.DebugFormat(
                        "StackTrace:{0}{1}",
                        Environment.NewLine, ex.StackTrace);

                    return null;
                }
            }
            
            internal static CurrentUserAdminCheckResponse IsUserAdmin(
                string organizationName,
                string authToken)
            {
                Uri endpoint = mWebApiUris.GetFullUri(
                    IsUserAdminEnpoint,
                    organizationName);

                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(endpoint);
                    request.Method = "GET";
                    request.ContentType = "application/json";

                    string authenticationToken = "Basic " + authToken;

                    request.Headers.Add(
                       HttpRequestHeader.Authorization, authenticationToken);

                    return GetResponse<CurrentUserAdminCheckResponse>(request);
                }
                catch (Exception ex)
                {
                    mLog.ErrorFormat(
                       "Unable to retrieve is user admin '{0}': {1}",
                       endpoint.ToString(), ex.Message);

                    mLog.DebugFormat(
                        "StackTrace:{0}{1}",
                        Environment.NewLine, ex.StackTrace);

                    return new CurrentUserAdminCheckResponse
                    {
                        Error = BuildLoggedErrorFields(ex, endpoint)
                    };
                }
            }

            internal static SubscriptionDetailsResponse GetSubscriptionDetails(
                string organizationName,
                string authToken)
            {
                Uri endpoint = mWebApiUris.GetFullUri(
                    SubscriptionDetailsEndpoint,
                    organizationName);

                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(endpoint);
                    request.Method = "GET";
                    request.ContentType = "application/json";

                    string authenticationToken = "Basic " + authToken;

                    request.Headers.Add(
                       HttpRequestHeader.Authorization, authenticationToken);

                    return GetResponse<SubscriptionDetailsResponse>(request);
                }
                catch (Exception ex)
                {
                    mLog.ErrorFormat(
                       "Unable to retrieve subscription details '{0}': {1}",
                       endpoint.ToString(), ex.Message);

                    mLog.DebugFormat(
                        "StackTrace:{0}{1}",
                        Environment.NewLine, ex.StackTrace);

                    return null;
                }
            }

            const string TokenExchangeEndpoint = "api/oauth/unityid/exchange/{0}";
            const string IsUserAdminEnpoint = "api/cloud/organizations/{0}/is-user-admin";
            const string SubscriptionDetailsEndpoint = "api/cloud/organizations/{0}/subscription-details";

            static readonly PlasticWebApiUris mWebApiUris = PlasticWebApiUris.BuildDefault();
        }

        static TRes GetResponse<TRes>(WebRequest request)
        {
            using (WebResponse response = request.GetResponse())
            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                string json = reader.ReadToEnd();

                if (string.IsNullOrEmpty(json))
                    return default(TRes);

                return JsonConvert.DeserializeObject<TRes>(json);
            }
        }

        static ErrorResponse.ErrorFields BuildLoggedErrorFields(
            Exception ex, Uri endpoint)
        {
            LogException(ex, endpoint);

            return new ErrorResponse.ErrorFields
            {
                ErrorCode = ErrorCodes.ClientError,
                Message = ex.Message
            };
        }

        static void LogException(Exception ex, Uri endpoint)
        {
            mLog.ErrorFormat(
                "There was an error while calling '{0}': {1}",
                endpoint.ToString(), ex.Message);

            mLog.DebugFormat(
                "StackTrace:{0}{1}",
                Environment.NewLine, ex.StackTrace);
        }

        static readonly ILog mLog = PlasticApp.GetLogger("WebRestApiClient");
    }
}
