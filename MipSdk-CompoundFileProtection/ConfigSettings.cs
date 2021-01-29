using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;

namespace MipSdk_CompoundFileProtection
{
    public class ConfigSettings
    {
        private readonly IConfiguration _configuration;
        
        public ConfigSettings()
        {
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false, false)
                .Build();
           
        }

        public string AppName
        {
            get { return GetSetting("Application:Name"); }
        }

        public string AppVersion
        {
            get { return GetSetting("Application:Version"); }
        }

        public string ClientId
        {
            get { return GetSetting("Identity:ClientId"); }
        }

        public string TenantId
        {
            get { return GetSetting("Identity:TenantId"); }
        }

        public string RedirectUri
        {
            get { return GetSetting("Identity:RedirectUri"); }
        }

        public string IsMultiTenantApp
        {
            get { return GetSetting("Identity:IsMultiTenantApp"); }
        }
        
        public string AppSecret
        {
            get { return GetSetting("Identity:AppSecret"); }
        }
        
        private string GetSetting(string setting)
        {
            return _configuration[setting].ToString();
        }

    }
}