using System;

namespace AuthGate
{
    public enum InvalidCredentialReason
    {
        InvalidCredential,
        NotSupportedProvider
    }

    public class InvalidCredentialException : Exception
    {
        public readonly InvalidCredentialReason Reason;

        public InvalidCredentialException(InvalidCredentialReason reason)
            : base($"validate credential failed: {reason.ToString()}")
        {
            Reason = reason;
        }

        public InvalidCredentialException(InvalidCredentialReason reason, string providerId)
            : base($"validate credential failed: {reason.ToString()}, {providerId}")
        {
            Reason = reason;
        }
    }
}