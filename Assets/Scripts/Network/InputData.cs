namespace LABANAN
{
    /// <summary>
    /// Input data for a single frame from a single player.
    /// Packed into a single byte for minimal bandwidth.
    /// </summary>
    [System.Serializable]
    public struct InputData
    {
        public int frame;
        public byte buttons;

        // Button bit flags
        public const byte NONE = 0;
        public const byte LEFT = 1;
        public const byte RIGHT = 2;
        public const byte UP = 4;
        public const byte DOWN = 8;
        public const byte ATTACK = 16;
        public const byte SUNGKIT = 32;
        public const byte LAUNCH = 64;
        public const byte BLOCK = 128;

        public bool HasLeft => (buttons & LEFT) != 0;
        public bool HasRight => (buttons & RIGHT) != 0;
        public bool HasUp => (buttons & UP) != 0;
        public bool HasDown => (buttons & DOWN) != 0;
        public bool HasAttack => (buttons & ATTACK) != 0;
        public bool HasSungkit => (buttons & SUNGKIT) != 0;
        public bool HasLaunch => (buttons & LAUNCH) != 0;
        public bool HasBlock => (buttons & BLOCK) != 0;

        public static InputData Create(int frame)
        {
            return new InputData
            {
                frame = frame,
                buttons = NONE
            };
        }

        public byte[] Serialize()
        {
            return new byte[] { (byte)frame, buttons };
        }

        public static InputData Deserialize(byte[] data)
        {
            if (data == null || data.Length < 2)
                return Create(0);

            return new InputData
            {
                frame = data[0],
                buttons = data[1]
            };
        }

        public static bool operator ==(InputData a, InputData b)
        {
            return a.frame == b.frame && a.buttons == b.buttons;
        }

        public static bool operator !=(InputData a, InputData b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            if (obj is InputData other)
            {
                return this == other;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return frame.GetHashCode() ^ buttons.GetHashCode();
        }
    }
}
