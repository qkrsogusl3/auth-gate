namespace AuthGate
{
    public readonly struct UserInfo
    {
        public readonly string UserId;
        public readonly string Email;
        public readonly bool IsAnonymous;

        public UserInfo(string userId, string email, bool isAnonymous)
        {
            UserId = userId;
            Email = email;
            IsAnonymous = isAnonymous;
        }

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(UserId);
        }

        public override string ToString()
        {
            return $"{UserId}, {Email}";
        }
    }
}