using Moimo.Core;
using Moimo.Modules;

namespace Moimo
{
    public class PackStorage : BaseStorage
    {
        public string language;
        public FPSLimit fps;

        public override void OnDefault()
        {
            var _setting = GAME.GetSO<PackSetting>();
            language = string.IsNullOrEmpty(_setting.language) ? LOCALIZE.GetDefault() : _setting.language;
            fps = _setting.fps;
        }

        public override void OnLoad()
        {
            LOCALIZE.Initialize();
            FPS.Set(fps);
        }
    }
}
