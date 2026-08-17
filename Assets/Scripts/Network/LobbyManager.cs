using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace LABANAN
{
    /// <summary>
    /// Manages game lobby - creating rooms and joining by IP.
    /// </summary>
    public class LobbyManager : MonoBehaviour
    {
        public static LobbyManager Instance { get; private set; }

        public string LocalIPAddress { get; private set; }
        public bool IsHosting { get; private set; }
        public bool IsInLobby { get; private set; }

        public event Action OnLobbyCreated;
        public event Action OnLobbyJoined;
        public event Action<string> OnLobbyError;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }

            LocalIPAddress = GetLocalIPAddress();
        }

        /// <summary>
        /// Create a lobby (host a game).
        /// </summary>
        public void CreateLobby()
        {
            IsHosting = true;
            IsInLobby = true;

            NetworkManager.Instance.Host();
            NetworkManager.Instance.OnConnected += () =>
            {
                OnLobbyJoined?.Invoke();
            };

            OnLobbyCreated?.Invoke();
            Debug.Log($"Lobby created. Share IP: {LocalIPAddress}");
        }

        /// <summary>
        /// Join an existing lobby by IP address.
        /// </summary>
        public void JoinLobby(string hostAddress)
        {
            if (string.IsNullOrEmpty(hostAddress))
            {
                OnLobbyError?.Invoke("Please enter an IP address");
                return;
            }

            IsHosting = false;
            IsInLobby = true;

            NetworkManager.Instance.OnConnected += () =>
            {
                OnLobbyJoined?.Invoke();
            };

            NetworkManager.Instance.Join(hostAddress);
        }

        /// <summary>
        /// Leave the current lobby.
        /// </summary>
        public void LeaveLobby()
        {
            IsInLobby = false;
            IsHosting = false;
            NetworkManager.Instance.Disconnect();
        }

        /// <summary>
        /// Get the local machine's IP address for sharing.
        /// </summary>
        private string GetLocalIPAddress()
        {
            try
            {
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    socket.Connect("8.8.8.8", 80);
                    IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                    return endPoint.Address.ToString();
                }
            }
            catch
            {
                return "127.0.0.1";
            }
        }

        /// <summary>
        /// Validate an IP address string.
        /// </summary>
        public bool IsValidIP(string ip)
        {
            return IPAddress.TryParse(ip, out _);
        }
    }
}
