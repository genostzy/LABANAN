namespace LABANAN
{
    /// <summary>
    /// Manages game state snapshots for rollback netcode using a ring buffer.
    /// </summary>
    public class RollbackManager
    {
        private const int CAPACITY = 64;
        private GameState[] states = new GameState[CAPACITY];
        private bool[] occupied = new bool[CAPACITY];
        private int latestFrame = -1;
        private int rollbackCount;

        public int LatestFrame => latestFrame;
        public int RollbackCount => rollbackCount;

        private int Index(int frame) => ((frame % CAPACITY) + CAPACITY) % CAPACITY;

        public void SaveState(int frame, GameState state)
        {
            int idx = Index(frame);
            states[idx] = state.Clone();
            occupied[idx] = true;
            if (frame > latestFrame) latestFrame = frame;
        }

        public bool TryLoadState(int frame, out GameState state)
        {
            int idx = Index(frame);
            if (occupied[idx] && states[idx].frame == frame)
            {
                state = states[idx].Clone();
                return true;
            }
            state = default;
            return false;
        }

        public bool RollbackTo(int frame, out GameState state)
        {
            rollbackCount++;
            return TryLoadState(frame, out state);
        }

        public int GetEarliestFrame()
        {
            return latestFrame - CAPACITY + 1;
        }

        public void Clear()
        {
            for (int i = 0; i < CAPACITY; i++)
                occupied[i] = false;
            latestFrame = -1;
            rollbackCount = 0;
        }

        public int HistorySize
        {
            get
            {
                int count = 0;
                for (int i = 0; i < CAPACITY; i++)
                    if (occupied[i]) count++;
                return count;
            }
        }
    }
}
