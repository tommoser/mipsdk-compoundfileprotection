using Microsoft.InformationProtection;
using System;
using System.Collections.Generic;
using System.Text;


namespace MipSdk_CompoundFileProtection
{
    public class AppInfoExtended : ApplicationInfo
    {
        public string TenantId
        {
            get;set;
        }
        public bool IsMultiTenantApp
        {
            get;set;
        }
        public string RedirectUri
        {
            get;set;
        }
    }
}
