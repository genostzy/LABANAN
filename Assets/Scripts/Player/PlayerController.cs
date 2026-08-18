using UnityEngine;

namespace LABANAN
{
    [System.Serializable]
    public class PlayerController
    {
        // ── Physics (fixed-point, scale = 1000) ──
        public const int GRAVITY = 35;
        public const int JUMP_FORCE = 500;
        public const int MOVE_SPEED = 120;
        public const int MAX_SPEED = 120;
        public const int SLOW_MAX_SPEED = 60;
        public const int ACCELERATION = 15;
        public const int DECELERATION = 8;
        public const int KNOCKBACK_FORCE = 80;
        public const int PARRY_KNOCKBACK_FORCE = 120;
        public const int LAUNCH_SPEED = 350;
        public const int LAUNCH_DURATION = 10;

        // ── Damage ──
        public const int MAX_HEALTH = 100;
        public const int SWORD_DAMAGE = 25;
        public const int SUNGKIT_DAMAGE = 25;
        public const int LAUNCH_DAMAGE = 30;
        public const int PLATFORM_DAMAGE = 500;

        // ── Stamina ──
        public const int MAX_STAMINA = 100;
        public const int SWORD_STAMINA_COST = 5;
        public const int SUNGKIT_STAMINA_COST = 20;
        public const int LAUNCH_STAMINA_COST = 60;
        public const int STAMINA_REGEN = 2;

        // ── Body ──
        public const int BODY_WIDTH = 640;
        public const int BODY_HEIGHT = 1000;

        // ── Cooldowns (frames @ 60fps) ──
        public const int ATTACK_COOLDOWN = 30;
        public const int JUMP_COOLDOWN = 15;
        public const int SUNGKIT_COOLDOWN = 180;
        public const int LAUNCH_COOLDOWN = 300;
        public const int KNOCKBACK_DURATION = 18;
        public const int ACTION_LOCK_DURATION = 2;
        public const int BLOCK_MAX_DURATION = 180;
        public const int BLOCK_COOLDOWN = 60;
        public const int ATTACK_STARTUP = 3;
        public const int CROUCH_SPEED = 40;

        // ── Parry ──
        public const int PARRY_WINDOW = 10;
        public const int PARRY_HEAL = 10;

        // ── Slow debuff ──
        public const int SUNGKIT_SLOW_DURATION = 90;

        // Animation states
        public const int IDLE_RIGHT = 10;
        public const int IDLE_LEFT = 11;
        public const int RUNNING_RIGHT = 2;
        public const int RUNNING_LEFT = 3;
        public const int JUMP_RIGHT = 6;
        public const int JUMP_LEFT = 7;
        public const int ATTACK_RIGHT = 0;
        public const int ATTACK_LEFT = 1;
        public const int SUNGKIT_RIGHT = 14;
        public const int SUNGKIT_LEFT = 15;
        public const int LAUNCH_RIGHT = 8;
        public const int LAUNCH_LEFT = 9;
        public const int BLOCK_RIGHT = 12;
        public const int BLOCK_LEFT = 13;
        public const int CROUCH_RIGHT = 16;
        public const int CROUCH_LEFT = 17;

        public static int GetAnimFrames(int animState)
        {
            switch (animState)
            {
                case RUNNING_RIGHT: case RUNNING_LEFT: return 12;
                case IDLE_RIGHT: case IDLE_LEFT: return 6;
                case JUMP_RIGHT: case JUMP_LEFT: return 6;
                case ATTACK_RIGHT: case ATTACK_LEFT: return 5;
                case SUNGKIT_RIGHT: case SUNGKIT_LEFT: return 5;
                case LAUNCH_RIGHT: case LAUNCH_LEFT: return 6;
                case BLOCK_RIGHT: case BLOCK_LEFT: return 1;
                case CROUCH_RIGHT: case CROUCH_LEFT: return 1;
                default: return 1;
            }
        }

        public static int GetAnimSpeed(int animState)
        {
            switch (animState)
            {
                case RUNNING_RIGHT: case RUNNING_LEFT: return 8;
                case ATTACK_RIGHT: case ATTACK_LEFT: return 6;
                case SUNGKIT_RIGHT: case SUNGKIT_LEFT: return 6;
                case LAUNCH_RIGHT: case LAUNCH_LEFT: return 6;
                case IDLE_RIGHT: case IDLE_LEFT: return 12;
                default: return 10;
            }
        }

        public static PlayerState Update(PlayerState state, InputData input, PlatformManager platforms)
        {
            if (state.health <= 0) return state;

            state.attackCooldownLeft = Max(0, state.attackCooldownLeft - 1);
            state.jumpCooldownLeft = Max(0, state.jumpCooldownLeft - 1);
            state.sungkitCooldownLeft = Max(0, state.sungkitCooldownLeft - 1);
            state.launchCooldownLeft = Max(0, state.launchCooldownLeft - 1);
            state.actionLockFramesLeft = Max(0, state.actionLockFramesLeft - 1);
            state.blockCooldownLeft = Max(0, state.blockCooldownLeft - 1);
            state.slowTimer = Max(0, state.slowTimer - 1);

            // Stamina regen
            state.stamina = Min(state.stamina + STAMINA_REGEN, MAX_STAMINA);

            if (state.isKnockedBack)
            {
                state.knockbackTimer -= 1;
                state.x += KNOCKBACK_FORCE * state.knockbackDirection;
                if (state.knockbackTimer <= 0)
                {
                    state.isKnockedBack = false;
                    state.knockbackTimer = 0;
                }
                state = ApplyPhysics(state, platforms);
                state = UpdateAnimation(state);
                return state;
            }

            state = ProcessInput(state, input);
            state = ApplyPhysics(state, platforms);
            state = UpdateAnimation(state);
            return state;
        }

        private static PlayerState ProcessInput(PlayerState state, InputData input)
        {
            // Block / Parry logic
            if (input.HasBlock && state.blockCooldownLeft <= 0 && !state.blocking)
            {
                state.blocking = true;
                state.blockTimer = 0;
            }

            if (state.blocking)
            {
                if (input.HasBlock)
                {
                    state.blockTimer++;
                    if (state.blockTimer >= BLOCK_MAX_DURATION)
                    {
                        state.blocking = false;
                        state.blockTimer = 0;
                        state.blockCooldownLeft = BLOCK_COOLDOWN;
                    }
                }
                else
                {
                    state.blocking = false;
                    state.blockTimer = 0;
                }
            }

            int effectiveMaxSpeed = state.slowTimer > 0 ? SLOW_MAX_SPEED : MAX_SPEED;

            // Movement
            if (!state.attacking && !state.sungkit && !state.launch && !state.blocking && !state.crouching)
            {
                if (input.HasLeft && !input.HasRight)
                {
                    state.speed = Max(state.speed - ACCELERATION, -effectiveMaxSpeed);
                    state.facingLeft = true;
                    state.moving = true;
                }
                else if (input.HasRight && !input.HasLeft)
                {
                    state.speed = Min(state.speed + ACCELERATION, effectiveMaxSpeed);
                    state.facingLeft = false;
                    state.moving = true;
                }
                else
                {
                    if (state.speed > 0)
                        state.speed = Max(state.speed - DECELERATION, 0);
                    else if (state.speed < 0)
                        state.speed = Min(state.speed + DECELERATION, 0);
                    state.moving = Abs(state.speed) > 100;
                }
            }
            else
            {
                if (state.speed > 0)
                    state.speed = Max(state.speed - DECELERATION, 0);
                else if (state.speed < 0)
                    state.speed = Min(state.speed + DECELERATION, 0);
                state.moving = Abs(state.speed) > 100;
            }

            // Jump
            if (input.HasUp && state.isOnGround && state.jumpCooldownLeft <= 0)
            {
                state.yVelocity = JUMP_FORCE;
                state.isOnGround = false;
                state.jumping = true;
                state.jumpCooldownLeft = JUMP_COOLDOWN;
            }

            state.crouching = input.HasDown;

            // Attacks - check stamina first
            if (!state.attacking && !state.sungkit && !state.launch && state.actionLockFramesLeft <= 0 && !state.crouching)
            {
                if (input.HasAttack && state.stamina >= SWORD_STAMINA_COST)
                {
                    state.attacking = true;
                    state.stamina -= SWORD_STAMINA_COST;
                    state.attackCooldownLeft = ATTACK_COOLDOWN;
                    state.actionLockFramesLeft = ACTION_LOCK_DURATION;
                    state.attackStartupFrames = ATTACK_STARTUP;
                }
                else if (input.HasSungkit && state.sungkitCooldownLeft <= 0 && state.stamina >= SUNGKIT_STAMINA_COST)
                {
                    state.sungkit = true;
                    state.stamina -= SUNGKIT_STAMINA_COST;
                    state.sungkitCooldownLeft = SUNGKIT_COOLDOWN;
                    state.actionLockFramesLeft = ACTION_LOCK_DURATION;
                }
                else if (input.HasLaunch && state.stamina >= LAUNCH_STAMINA_COST)
                {
                    state.launch = true;
                    state.stamina -= LAUNCH_STAMINA_COST;
                    state.launchTimer = LAUNCH_DURATION;
                    state.launchCooldownLeft = LAUNCH_COOLDOWN;
                    state.actionLockFramesLeft = ACTION_LOCK_DURATION;
                }
            }

            // Tick attack startup
            if (state.attackStartupFrames > 0)
                state.attackStartupFrames--;

            return state;
        }

        private static PlayerState ApplyPhysics(PlayerState state, PlatformManager platforms)
        {
            if (state.launch)
            {
                int launchDir = state.facingLeft ? -1 : 1;
                state.x += LAUNCH_SPEED * launchDir;
                state.launchTimer--;
                if (state.launchTimer <= 0)
                {
                    state.launch = false;
                    state.launchTimer = 0;
                }
            }

            if (!state.isOnGround)
            {
                state.yVelocity -= GRAVITY;
            }

            int nextX = state.x + state.speed;
            int nextY = state.y + state.yVelocity;

            state.isOnGround = false;

            CheckPlatform(ref nextX, ref nextY, ref state.isOnGround, ref state.yVelocity, ref state.jumping,
                platforms.MainX, platforms.MainWidth, platforms.MainY);
            CheckPlatform(ref nextX, ref nextY, ref state.isOnGround, ref state.yVelocity, ref state.jumping,
                platforms.LeftX, platforms.LeftWidth, platforms.LeftY);
            CheckPlatform(ref nextX, ref nextY, ref state.isOnGround, ref state.yVelocity, ref state.jumping,
                platforms.RightX, platforms.RightWidth, platforms.RightY);

            state.x = ClampX(nextX);
            state.y = nextY;

            state.falling = !state.isOnGround && state.yVelocity < 0;

            return state;
        }

        private static void CheckPlatform(ref int nextX, ref int nextY, ref bool isOnGround, ref int yVelocity, ref bool jumping,
            int platX, int platWidth, int platY)
        {
            int halfW = BODY_WIDTH / 2;

            bool horizontallyOverlapping = nextX + halfW > platX && nextX - halfW < platX + platWidth;
            if (!horizontallyOverlapping) return;

            int prevY = nextY - yVelocity;
            bool wasAbove = prevY >= platY;
            bool nowBelow = nextY <= platY;

            if (wasAbove && nowBelow)
            {
                nextY = platY;
                yVelocity = 0;
                isOnGround = true;
                jumping = false;
            }
            else if (isOnGround && nextY == platY)
            {
                yVelocity = 0;
            }
        }

        private static int ClampX(int x)
        {
            return x;
        }

        private static PlayerState UpdateAnimation(PlayerState state)
        {
            int prevAnim = state.animState;

            if (state.attacking)
                state.animState = state.facingLeft ? ATTACK_LEFT : ATTACK_RIGHT;
            else if (state.sungkit)
                state.animState = state.facingLeft ? SUNGKIT_LEFT : SUNGKIT_RIGHT;
            else if (state.launch)
                state.animState = state.facingLeft ? LAUNCH_LEFT : LAUNCH_RIGHT;
            else if (state.blocking)
                state.animState = state.facingLeft ? BLOCK_LEFT : BLOCK_RIGHT;
            else if (state.crouching)
                state.animState = state.facingLeft ? CROUCH_LEFT : CROUCH_RIGHT;
            else if (state.falling)
                state.animState = state.facingLeft ? JUMP_LEFT : JUMP_RIGHT;
            else if (state.jumping)
                state.animState = state.facingLeft ? JUMP_LEFT : JUMP_RIGHT;
            else if (state.moving)
                state.animState = state.facingLeft ? RUNNING_LEFT : RUNNING_RIGHT;
            else
                state.animState = state.facingLeft ? IDLE_LEFT : IDLE_RIGHT;

            bool jumpAnim = state.animState == JUMP_LEFT || state.animState == JUMP_RIGHT;

            if (prevAnim != state.animState)
            {
                state.animTick = 0;
                state.animIndex = state.falling && jumpAnim ? GetAnimFrames(state.animState) - 1 : 0;
            }
            else if (state.falling && jumpAnim && state.animIndex < GetAnimFrames(state.animState) - 1)
            {
                state.animIndex = GetAnimFrames(state.animState) - 1;
            }

            state.animTick++;
            int tickThreshold = GetAnimSpeed(state.animState);
            if (state.animTick >= tickThreshold)
            {
                state.animTick = 0;
                int maxFrames = GetAnimFrames(state.animState);

                if (state.falling && jumpAnim)
                {
                    state.animIndex--;
                    if (state.animIndex < 0)
                        state.animIndex = 0;
                }
                else
                {
                    state.animIndex++;
                    if (state.animIndex >= maxFrames)
                    {
                        state.animIndex = 0;
                        if (state.attacking || state.sungkit)
                        {
                            state.attacking = false;
                            state.sungkit = false;
                        }
                    }
                }
            }

            return state;
        }

        public struct CombatResult
        {
            public int damage;
            public bool knockbackDefender;
            public bool knockbackAttacker;
            public bool perfectParry;
            public bool isSungkit;
            public int attackType; // 1=sword, 2=sungkit, 3=launch
        }

        public static CombatResult CheckAttackCollisions(
            PlayerState attacker, PlayerState defender)
        {
            var result = new CombatResult();

            Rect attackHitbox = GetAttackHitbox(attacker);
            Rect defenderHitbox = GetPlayerHitbox(defender);

            bool hit = attackHitbox.Overlaps(defenderHitbox) && IsAttacking(attacker);
            if (!hit) return result;

            result.attackType = attacker.attacking ? 1 : attacker.sungkit ? 2 : 3;

            if (!defender.blocking)
            {
                result.damage = GetAttackDamage(attacker);
                result.knockbackDefender = true;
                result.isSungkit = attacker.sungkit;
            }
            else
            {
                bool isPerfectParry = defender.blockTimer <= PARRY_WINDOW;

                if (isPerfectParry)
                {
                    result.knockbackAttacker = true;
                    result.perfectParry = true;
                }
                else
                {
                    result.knockbackAttacker = true;
                }
            }

            return result;
        }

        public static int GetAttackDamage(PlayerState attacker)
        {
            if (attacker.attacking) return SWORD_DAMAGE;
            if (attacker.sungkit) return SUNGKIT_DAMAGE;
            if (attacker.launch) return LAUNCH_DAMAGE;
            return 0;
        }

        private static bool IsAttacking(PlayerState state)
        {
            int maxFrames = GetAnimFrames(state.animState);
            bool inHitFrames = state.animIndex >= maxFrames - 2;
            return inHitFrames && (state.attacking || state.sungkit || state.launch);
        }

        public static Rect GetAttackHitbox(PlayerState state)
        {
            int hitboxX, hitboxY, hitboxW, hitboxH;

            if (state.attacking)
            {
                hitboxW = 500;
                hitboxH = 100;
                hitboxY = state.y + 500;
                hitboxX = state.facingLeft ? state.x - 500 : state.x + 150;
            }
            else if (state.sungkit)
            {
                hitboxW = 400;
                hitboxH = 100;
                hitboxY = state.y + 200;
                hitboxX = state.facingLeft ? state.x - 400 : state.x + 150;
            }
            else if (state.launch)
            {
                hitboxW = 500;
                hitboxH = 150;
                hitboxY = state.y + 500;
                hitboxX = state.facingLeft ? state.x - 500 : state.x + 150;
            }
            else
            {
                return new Rect(0, 0, 0, 0);
            }

            return new Rect(
                FixedMath.ToFloat(hitboxX),
                FixedMath.ToFloat(hitboxY),
                FixedMath.ToFloat(hitboxW),
                FixedMath.ToFloat(hitboxH)
            );
        }

        public static Rect GetPlayerHitbox(PlayerState state)
        {
            int h = state.crouching ? BODY_HEIGHT / 2 : BODY_HEIGHT;
            return new Rect(
                FixedMath.ToFloat(state.x - BODY_WIDTH / 2),
                FixedMath.ToFloat(state.y),
                FixedMath.ToFloat(BODY_WIDTH),
                FixedMath.ToFloat(h)
            );
        }

        private static int Max(int a, int b) => a > b ? a : b;
        private static int Min(int a, int b) => a < b ? a : b;
        private static int Abs(int a) => a < 0 ? -a : a;
    }
}
