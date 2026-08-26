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

        // Left platform (elevated): X=0.4-5.6, top surface Y=4.9
        public int LeftX = 400;
        public int LeftY = 4900;
        public int LeftWidth = 5200;
        public int LeftHeight = 2000;

        // Right platform (elevated): X=12.4-17.6, top surface Y=4.9
        public int RightX = 12400;
        public int RightY = 4900;
        public int RightWidth = 5200;
        public int RightHeight = 2000;

        public int DeathY = -5000;

        public bool IsOnDeathZone(PlayerState player)
        {
            return player.y < DeathY;
        }
    }
}
