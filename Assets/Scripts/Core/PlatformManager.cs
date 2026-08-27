using UnityEngine;

namespace LABANAN
{
    [System.Serializable]
    public class PlatformManager
    {
        // Main platform - matches visual sprite (9, 1.1) size 20x3, surface at Y=0.5
        public int MainX = -1000;
        public int MainY = 500;
        public int MainWidth = 20000;
        public int MainHeight = 3000;

        // Left platform (elevated): X=0.4-5.6, top surface Y=3.5
        public int LeftX = 400;
        public int LeftY = 3600;
        public int LeftWidth = 5200;
        public int LeftHeight = 2000;

        // Right platform (elevated): X=12.4-17.6, top surface Y=3.5
        public int RightX = 12400;
        public int RightY = 3600;
        public int RightWidth = 5200;
        public int RightHeight = 2000;

        public int DeathY = -5000;

        public bool IsOnDeathZone(PlayerState player)
        {
            return player.y < DeathY;
        }
    }
}
