#if SUPABASE

namespace Moimo.Cloud
{
    public class AuthProfile
    {
        public string UserId;
        public string Email;
        public bool IsLoggedIn;
        public bool HasCachedSession;
    }
}

#endif
