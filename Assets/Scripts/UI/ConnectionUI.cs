using UnityEngine;
using UnityEngine.UI;

namespace LABANAN
{
    /// <summary>
    /// Shows connection quality info: ping, rollback frames, connection status.
    /// </summary>
    public class ConnectionUI : MonoBehaviour
    {
        [Header("References")]
        public Text pingText;
        public Image connectionIndicator;
        public Text rollbackText;

        [Header("Colors")]
        public Color goodColor = Color.green;
        public Color mediumColor = Color.yellow;
        public Color badColor = Color.red;

        private void Update()
        {
            if (NetworkManager.Instance == null) return;

            NetworkManager.ConnectionState state = NetworkManager.Instance.State;
            int ping = NetworkManager.Instance.PingMs;
            bool isRollingBack = NetworkManager.Instance.Rollback.RollbackCount > 0;

            // Update ping display
            if (pingText != null)
            {
                pingText.text = $"Ping: {ping}ms";
            }

            // Update connection indicator color
            if (connectionIndicator != null)
            {
                if (ping < 50)
                    connectionIndicator.color = goodColor;
                else if (ping < 100)
                    connectionIndicator.color = mediumColor;
                else
                    connectionIndicator.color = badColor;
            }

            // Update rollback indicator
            if (rollbackText != null)
            {
                if (isRollingBack)
                {
                    rollbackText.text = "ROLLBACK";
                    rollbackText.color = Color.red;
                }
                else
                {
                    rollbackText.text = "";
                }
            }
        }
    }
}
