namespace LABANAN
{
    /// <summary>
    /// Complete game state snapshot for rollback.
    /// Stores everything needed to restore the game to any frame.
    /// </summary>
    [System.Serializable]
    public struct GameState
    {
        public int frame;

        // Player states
        public PlayerState player1;
        public PlayerState player2;

        // Game state
        public int round;
        public int player1Wins;
        public int player2Wins;
        public int timer; // seconds remaining
        public int timerFrameCounter; // frames until next second tick
        public bool isPaused;
        public bool isGameOver;
        public bool showLaban;
        public int labanTimerFrames;

        // Win display
        public bool showBlueWin;
        public bool showRedWin;
        public int winDisplayTimerFrames;

        public static GameState CreateDefault()
        {
            return new GameState
            {
                frame = 0,
                player1 = PlayerState.CreateDefault(6000, 500, false),
                player2 = PlayerState.CreateDefault(12000, 500, true),
                round = 1,
                player1Wins = 0,
                player2Wins = 0,
                timer = 60,
                timerFrameCounter = 0,
                isPaused = false,
                isGameOver = false,
                showLaban = true,
                labanTimerFrames = 120, // 2 seconds at 60fps
                showBlueWin = false,
                showRedWin = false,
                winDisplayTimerFrames = 0
            };
        }

        public GameState Clone()
        {
            GameState copy = this;
            copy.player1 = this.player1.Clone();
            copy.player2 = this.player2.Clone();
            return copy;
        }

        public byte[] Serialize()
        {
            using (var ms = new System.IO.MemoryStream())
            using (var writer = new System.IO.BinaryWriter(ms))
            {
                writer.Write(frame);
                WritePlayerState(writer, player1);
                WritePlayerState(writer, player2);
                writer.Write(round);
                writer.Write(player1Wins);
                writer.Write(player2Wins);
                writer.Write(timer);
                writer.Write(timerFrameCounter);
                writer.Write(isPaused);
                writer.Write(isGameOver);
                writer.Write(showLaban);
                writer.Write(labanTimerFrames);
                writer.Write(showBlueWin);
                writer.Write(showRedWin);
                writer.Write(winDisplayTimerFrames);
                return ms.ToArray();
            }
        }

        public static GameState Deserialize(byte[] data)
        {
            using (var ms = new System.IO.MemoryStream(data))
            using (var reader = new System.IO.BinaryReader(ms))
            {
                var state = new GameState();
                state.frame = reader.ReadInt32();
                state.player1 = ReadPlayerState(reader);
                state.player2 = ReadPlayerState(reader);
                state.round = reader.ReadInt32();
                state.player1Wins = reader.ReadInt32();
                state.player2Wins = reader.ReadInt32();
                state.timer = reader.ReadInt32();
                state.timerFrameCounter = reader.ReadInt32();
                state.isPaused = reader.ReadBoolean();
                state.isGameOver = reader.ReadBoolean();
                state.showLaban = reader.ReadBoolean();
                state.labanTimerFrames = reader.ReadInt32();
                state.showBlueWin = reader.ReadBoolean();
                state.showRedWin = reader.ReadBoolean();
                state.winDisplayTimerFrames = reader.ReadInt32();
                return state;
            }
        }

        private static void WritePlayerState(System.IO.BinaryWriter w, PlayerState p)
        {
            w.Write(p.x);
            w.Write(p.y);
            w.Write(p.health);
            w.Write(p.stamina);
            w.Write(p.speed);
            w.Write(p.yVelocity);
            w.Write(p.isOnGround);
            w.Write(p.facingLeft);
            w.Write(p.moving);
            w.Write(p.attacking);
            w.Write(p.sungkit);
            w.Write(p.launch);
            w.Write(p.blocking);
            w.Write(p.jumping);
            w.Write(p.crouching);
            w.Write(p.animState);
            w.Write(p.animTick);
            w.Write(p.animIndex);
            w.Write(p.isKnockedBack);
            w.Write(p.knockbackDirection);
            w.Write(p.knockbackTimer);
            w.Write(p.attackCooldownLeft);
            w.Write(p.jumpCooldownLeft);
            w.Write(p.sungkitCooldownLeft);
            w.Write(p.launchCooldownLeft);
            w.Write(p.attackStartupFrames);
            w.Write(p.blockTimer);
            w.Write(p.blockCooldownLeft);
            w.Write(p.launchTimer);
            w.Write(p.actionLockFramesLeft);
        }

        private static PlayerState ReadPlayerState(System.IO.BinaryReader r)
        {
            return new PlayerState
            {
                x = r.ReadInt32(),
                y = r.ReadInt32(),
                health = r.ReadInt32(),
                stamina = r.ReadInt32(),
                speed = r.ReadInt32(),
                yVelocity = r.ReadInt32(),
                isOnGround = r.ReadBoolean(),
                facingLeft = r.ReadBoolean(),
                moving = r.ReadBoolean(),
                attacking = r.ReadBoolean(),
                sungkit = r.ReadBoolean(),
                launch = r.ReadBoolean(),
                blocking = r.ReadBoolean(),
                jumping = r.ReadBoolean(),
                crouching = r.ReadBoolean(),
                animState = r.ReadInt32(),
                animTick = r.ReadInt32(),
                animIndex = r.ReadInt32(),
                isKnockedBack = r.ReadBoolean(),
                knockbackDirection = r.ReadInt32(),
                knockbackTimer = r.ReadInt32(),
                attackCooldownLeft = r.ReadInt32(),
                jumpCooldownLeft = r.ReadInt32(),
                sungkitCooldownLeft = r.ReadInt32(),
                launchCooldownLeft = r.ReadInt32(),
                attackStartupFrames = r.ReadInt32(),
                blockTimer = r.ReadInt32(),
                blockCooldownLeft = r.ReadInt32(),
                launchTimer = r.ReadInt32(),
                actionLockFramesLeft = r.ReadInt32()
            };
        }
    }
}
