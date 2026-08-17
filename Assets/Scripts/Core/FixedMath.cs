namespace LABANAN
{
    /// <summary>
    /// Fixed-point math utilities for deterministic gameplay.
    /// All values use a scale of 1000 (1.0 = 1000).
    /// </summary>
    public static class FixedMath
    {
        public const int SCALE = 1000;
        public const int ONE = 1000;
        public const int ZERO = 0;

        public static int FromFloat(float value)
        {
            return (int)(value * SCALE);
        }

        public static float ToFloat(int value)
        {
            return (float)value / SCALE;
        }

        public static int Mul(int a, int b)
        {
            return (int)(((long)a * b) / SCALE);
        }

        public static int Div(int a, int b)
        {
            if (b == 0) return 0;
            return (int)(((long)a * SCALE) / b);
        }

        public static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public static int Abs(int value)
        {
            return value < 0 ? -value : value;
        }

        public static int Max(int a, int b)
        {
            return a > b ? a : b;
        }

        public static int Min(int a, int b)
        {
            return a < b ? a : b;
        }

        public static int Sign(int value)
        {
            if (value > 0) return 1;
            if (value < 0) return -1;
            return 0;
        }
    }
}
