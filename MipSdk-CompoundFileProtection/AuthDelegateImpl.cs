using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using Microsoft.InformationProtection;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MipSdk_CompoundFileProtection
{
    class AuthDelegateImpl : IAuthDelegate
    {
        // Set the redirect URI from the AAD Application Registration.
        private static string redirectUri;
        private static bool isMultitenantApp;
        private static string tenantId;
        private static string appVersion;
        private static string appName;
        private static string clientId;
        private readonly IConfidentialClientApplication _app;        
  
        public AuthDelegateImpl(AppInfoExtended appInfo)
        {
            ConfigSettings config = new ConfigSettings();

            redirectUri = appInfo.RedirectUri;
            isMultitenantApp = appInfo.IsMultiTenantApp;
            tenantId = appInfo.TenantId;
            appVersion = appInfo.ApplicationVersion;
            appName = appInfo.ApplicationName;
            clientId = appInfo.ApplicationId;

            // Create an auth context using the provided authority and token cache

            ConfidentialClientApplicationOptions options = new ConfidentialClientApplicationOptions()
            {
                ClientSecret = config.AppSecret,
                ClientId = clientId,
                TenantId = tenantId,
                RedirectUri = redirectUri,
                Instance = "https://login.microsoftonline.com/"
            };
                        
            _app = ConfidentialClientApplicationBuilder
                .CreateWithApplicationOptions(options)
                .WithRedirectUri(redirectUri)
                .Build();                
        }

        public string AcquireToken(Identity identity, string authority, string resource, string claims)
        {
            return AcquireTokenAsync(authority, resource, claims, isMultitenantApp).Result.AccessToken;
        }

        /// <summary>
        /// Implements token acquisition logic via the Microsoft Authentication Library.
        /// 
        /// /// </summary>
        /// <param name="identity"></param>
        /// <param name="authority"></param>
        /// <param name="resource"></param>
        /// <param name="claims"></param>
        /// <returns></returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(string authority, string resource, string claims, bool isMultiTenantApp = true)
        {
            AuthenticationResult result = null;

            // Append .default to the resource passed in to AcquireToken().
            string[] scopes = new string[] { resource[resource.Length - 1].Equals('/') ? $"{resource}.default" : $"{resource}/.default" };

            result = await _app.AcquireTokenForClient(scopes)
               .ExecuteAsync();

            // Return the token. The token is sent to the resource.                           
            return result;
        }       
    }
}
