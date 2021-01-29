using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.InformationProtection;

namespace MipSdk_CompoundFileProtection
{
    class ConsentDelegateImpl : IConsentDelegate
    {
        public Consent GetUserConsent(string url)
        {
            return Consent.AcceptAlways;
        }
    }
}
