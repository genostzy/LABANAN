using UnityEngine;

namespace LABANAN
{
    [System.Serializable]
    public class PlatformManager
    {
        // Main platform - matches visual (9, -1.0) size 26x3, surface at Y=0.5
        public int MainX = -4000;
        public int MainY = 500;
        public int MainWidth = 26000;
        public int MainHeight = 3000;

        // Left platform (elevated): matches visual (3, 2.5) size 7x2, surface at Y=3.5
        public int LeftX = -500;
        public int LeftY = 3500;
        public int LeftWidth = 7000;
        public int LeftHeight = 2000;

        // Right platform (elevated): matches visual (15, 2.5) size 7x2, surface at Y=3.5
        public int RightX = 11500;
        public int RightY = 3500;
        public int RightWidth = 7000;
        public int RightHeight = 2000;

        public int DeathY = -5000;

        public bool IsOnDeathZone(PlayerState player)
        {
            return player.y < DeathY;
        }
    }
}
