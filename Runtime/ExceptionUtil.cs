using System;
using System.Diagnostics;
using System.Linq;

namespace AuthGate
{
    public static class ExceptionUtil
    {
        // [DebuggerHidden]
        // [StackTraceHidden]
        public static void ThrowIfCreatedUser(in UserInfo user)
        {
            if (user.IsValid())
            {
                throw new SignInFailedException(SignInFailReason.CreatedUser);
            }
        }

        public static void ThrowIfNotSupportedProvider(bool isSupported, string providerId)
        {
            if (!isSupported)
            {
                throw new InvalidCredentialException(InvalidCredentialReason.NotSupportedProvider, providerId);
            }
        }

        public static void ThrowIfInvalidUser(in UserInfo user)
        {
            if (!user.IsValid())
            {
                throw new InvalidUserException();
            }
        }

        public static void ThrowIfNotInitialized(IGate gate)
        {
            if (gate == null)
            {
                throw new NotInitializedException();
            }
        }

        [AttributeUsage(AttributeTargets.Method, Inherited = false)]
        public sealed class StackTraceHiddenAttribute : Attribute
        {
        }

        public class BaseException : Exception
        {
            public BaseException(string message) : base(message)
            {
            }

            public override string StackTrace
            {
                get
                {
                    return string.Concat(
                        (new StackTrace(this, true)
                            .GetFrames() ?? Array.Empty<StackFrame>())
                        .Where(frame => !frame.GetMethod().IsDefined(typeof(StackTraceHiddenAttribute), true))
                        .Select(frame => new StackTrace(frame).ToString())
                        .ToArray());
                }
            }
        }
    }
}