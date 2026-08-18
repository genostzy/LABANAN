namespace LABANAN
{
    /// <summary>
    /// Serializable player state for rollback snapshots.
    /// All values are integers for deterministic simulation.
    /// </summary>
    [System.Serializable]
    public struct PlayerState
    {
        public int x;
        public int y;
        public int health;
        public int stamina;
        public int speed;
        public int yVelocity;
        public bool isOnGround;
        public bool facingLeft;
        public bool moving;
        public bool attacking;
        public bool sungkit;
        public bool launch;
        public bool blocking;
        public bool jumping;
        public bool crouching;
        public bool falling;
        public int animState;
        public int animTick;
        public int animIndex;

        // Knockback
        public bool isKnockedBack;
        public int knockbackDirection;
        public int knockbackTimer;

        // Attack cooldowns (in frames)
        public int attackCooldownLeft;
        public int jumpCooldownLeft;
        public int sungkitCooldownLeft;
        public int launchCooldownLeft;
        public int attackStartupFrames;

        // Block
        public int blockTimer;
        public int blockCooldownLeft;

        // Launch dash
        public int launchTimer;

        // Action lock
        public int actionLockFramesLeft;

        // Slow debuff (from sungkit hit)
        public int slowTimer;

        // Stamina regen timer (counts frames, regens +1 per second)
        public int staminaRegenTimer;

        public static PlayerState CreateDefault(int spawnX, int spawnY, bool facingLeft)
        {
            return new PlayerState
            {
                x = spawnX,
                y = spawnY,
                health = 100,
                stamina = 100,
                speed = 0,
                yVelocity = 0,
                isOnGround = true,
                facingLeft = facingLeft,
                moving = false,
                attacking = false,
                sungkit = false,
                launch = false,
                blocking = false,
                jumping = false,
                crouching = false,
                falling = false,
                animState = facingLeft ? 11 : 10,
                animTick = 0,
                animIndex = 0,
                isKnockedBack = false,
                knockbackDirection = 0,
                knockbackTimer = 0,
                attackCooldownLeft = 0,
                jumpCooldownLeft = 0,
                sungkitCooldownLeft = 0,
                launchCooldownLeft = 0,
                attackStartupFrames = 0,
                blockTimer = 0,
                blockCooldownLeft = 0,
                launchTimer = 0,
                actionLockFramesLeft = 0,
                slowTimer = 0,
                staminaRegenTimer = 0
            };
        }

        public PlayerState Clone()
        {
            return (PlayerState)this;
        }
    }
}
