#if GOOGLE_MOBILE

namespace Moimo.Google
{
    public class AdBase
    {
        public virtual void Initialize() { }
        public virtual bool CanShow() { return false; }
    }
}

#endif