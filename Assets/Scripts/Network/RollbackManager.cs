using System.Collections.Generic;

namespace LABANAN
{
    /// <summary>
    /// Manages game state snapshots for rollback netcode.
    /// Saves states every frame, loads/rewinds when predictions are wrong.
    /// </summary>
    public class RollbackManager
    {
        private const int MAX_HISTORY = 64;
        private Dictionary<int, GameState> stateHistory = new Dictionary<int, GameState>();

        public int LatestFrame { get; private set; }
        public int RollbackCount { get; private set; }

        public void SaveState(int frame, GameState state)
        {
            stateHistory[frame] = state.Clone();
            LatestFrame = frame;

            // Trim old states
            if (stateHistory.Count > MAX_HISTORY)
            {
                int oldest = frame - MAX_HISTORY;
                foreach (var key in new List<int>(stateHistory.Keys))
                {
                    if (key < oldest)
                        stateHistory.Remove(key);
                }
            }
        }

        public bool TryLoadState(int frame, out GameState state)
        {
            if (stateHistory.TryGetValue(frame, out GameState found))
            {
                state = found.Clone();
                return true;
            }
            state = default;
            return false;
        }

        /// <summary>
        /// Rollback to a specific frame and return the state.
        /// The game will re-simulate from this frame forward.
        /// </summary>
        public bool RollbackTo(int frame, out GameState state)
        {
            RollbackCount++;
            return TryLoadState(frame, out state);
        }

        /// <summary>
        /// Get the earliest available frame for rollback.
        /// </summary>
        public int GetEarliestFrame()
        {
            int earliest = int.MaxValue;
            foreach (var key in stateHistory.Keys)
            {
                if (key < earliest)
                    earliest = key;
            }
            return earliest;
        }

        /// <summary>
        /// Reset all saved states.
        /// </summary>
        public void Clear()
        {
            stateHistory.Clear();
            LatestFrame = 0;
            RollbackCount = 0;
        }

        /// <summary>
        /// Get the number of frames currently stored.
        /// </summary>
        public int HistorySize => stateHistory.Count;
    }
}
