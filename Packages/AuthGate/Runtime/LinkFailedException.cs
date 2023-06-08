using System;

namespace AuthGate
{
    public class LinkFailedException : Exception
    {
        public readonly string ProviderId;

        public LinkFailedException(string providerId, Exception e) : base($"link failed: {providerId}", e)
        {
            ProviderId = providerId;
        }
    }
}