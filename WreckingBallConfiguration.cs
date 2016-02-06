using Rocket.API;

namespace ApokPT.RocketPlugins
{
    public class WreckingBallConfiguration : IRocketPluginConfiguration
    {
        public bool Enabled = true;
        public uint DestructionInterval = 10;
        public bool PrintToRCON = false;
        public bool LogScans = false;
        public bool PrintToChat = false;

        public void LoadDefaults() { }
    }
}
