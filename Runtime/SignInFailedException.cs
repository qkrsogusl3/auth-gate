using System;

namespace AuthGate
{
    public enum SignInFailReason
    {
        NotSupportProvider,
        PlatformCredentialFailed,
        PlatformCredentialCanceled,
        CreatedUser,
    }

    public class SignInFailedException : Exception
    {
        public readonly SignInFailReason Reason;
        public readonly string ProviderId;

        public SignInFailedException(SignInFailReason reason)
            : base($"signin process failed: {reason.ToString()}")
        {
            Reason = reason;
        }

        public SignInFailedException(SignInFailReason reason, string providerId)
            : base($"{providerId} signin process failed: {reason.ToString()}")
        {
            Reason = reason;
            ProviderId = providerId;
        }

        public SignInFailedException(SignInFailReason reason, string providerId, Exception e)
            : base($"{providerId} signin process failed: {reason.ToString()}", e)
        {
            Reason = reason;
            ProviderId = providerId;
        }
    }
}