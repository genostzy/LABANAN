using UnityEngine;

namespace LABANAN
{
    [System.Serializable]
    public class PlatformManager
    {
        // Main platform: visual (9, 3) size 32x3, surface at Y=0.5
        public int MainX = -7000;
        public int MainY = 500;
        public int MainWidth = 32000;
        public int MainHeight = 3000;

        // Left platform: visual (3, 5.5) size 9x2, surface at Y=3.5
        public int LeftX = -1500;
        public int LeftY = 3500;
        public int LeftWidth = 9000;
        public int LeftHeight = 2000;

        // Right platform: visual (15, 5.5) size 9x2, surface at Y=3.5
        public int RightX = 10500;
        public int RightY = 3500;
        public int RightWidth = 9000;
        public int RightHeight = 2000;

        public int DeathY = -5000;

        public bool IsOnDeathZone(PlayerState player)
        {
            return player.y < DeathY;
        }
    }
}
