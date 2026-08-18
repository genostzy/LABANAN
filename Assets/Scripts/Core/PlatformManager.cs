using UnityEngine;

namespace LABANAN
{
    [System.Serializable]
    public class PlatformManager
    {
        // Main platform - narrower so players can fall off edges
        public int MainX = 3000;
        public int MainY = 500;
        public int MainWidth = 12000;
        public int MainHeight = 500;

        // Left platform (elevated)
        public int LeftX = 1500;
        public int LeftY = 3500;
        public int LeftWidth = 3000;
        public int LeftHeight = 500;

        // Right platform (elevated)
        public int RightX = 13500;
        public int RightY = 3500;
        public int RightWidth = 3000;
        public int RightHeight = 500;

        public int DeathY = -5000;

        public bool IsOnDeathZone(PlayerState player)
        {
            return player.y < DeathY;
        }
    }
}
