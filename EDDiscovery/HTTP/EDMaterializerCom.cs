﻿using EDDiscovery;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Web.Script.Serialization;
using System.Configuration;

namespace EDDiscovery2.HTTP
{
    using System;
    using System.Web;

    public class EDMaterizliaerCom : HttpCom
    {
        private NameValueCollection _authTokens = null;
        private readonly string _authPath = "api/v1/auth";

        protected ResponseData RequestSecureGet(string action)
        {
            return ManagedRequest(null, action, RequestGetWrapper);
        }

        protected ResponseData RequestSecurePost(string json, string action)
        {
            return ManagedRequest(json, action, RequestPost);
        }

        protected ResponseData RequestSecurePatch(string json, string action)
        {
            return ManagedRequest(json, action, RequestPatch);
        }

        protected ResponseData RequestSecureDelete(string action)
        {
            return ManagedRequest(null, action, RequestDeleteWrapper);
        }


        private ResponseData SignIn()
        {
            _authTokens = null;
            var appSettings = ConfigurationManager.AppSettings;
            var username = appSettings["EDMaterializerUsername"];
            var password = appSettings["EDMaterializerPassword"];
            var json = $"{{\"email\": \"{username}\", \"password\": \"{password}\"}}";
            var response = RequestPost(json, $"{_authPath}/sign_in");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var headers = response.Headers;
                var tokens = new NameValueCollection();
                tokens["access-token"] = headers["access-token"];
                tokens["client"] = headers["client"];
                tokens["uid"] = headers["uid"]; ;
                _authTokens = tokens;
            }
            return response;
        }

        private ResponseData ManagedRequest(string json, 
                                            string action, 
                                            Func<string, string, NameValueCollection, ResponseData> requestMethod)
        {
            ResponseData response = new ResponseData(HttpStatusCode.BadRequest);
            // Attempt #1 with existing tokens
            if (_authTokens != null)
            {
                response = requestMethod(json, action, _authTokens);
                if (response.StatusCode == HttpStatusCode.Unauthorized ||
                    response.StatusCode == HttpStatusCode.BadRequest)
                {
                    _authTokens = null;
                }
                else
                {
                    return response;
                }
            }
            // Attempt #2 by logging in and obtaining fresh tokens
            if (_authTokens == null)
            {
                response = SignIn();
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    response = requestMethod(json, action, _authTokens);
                }
            }
            return response;
        }

        private ResponseData RequestGetWrapper(string json, string action, NameValueCollection headers)
        {
            return RequestGet(action, headers);
        }

        private ResponseData RequestDeleteWrapper(string json, string action, NameValueCollection headers)
        {
            return RequestDelete(action, headers);
        }


        private string AuthKeyToJson()
        {
            var json = new JavaScriptSerializer().Serialize(
                _authTokens.AllKeys.ToDictionary(k => k, k => _authTokens[k])
            );
            return json;
        }

    }
}