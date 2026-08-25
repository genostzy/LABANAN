using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace LABANAN
{
    /// <summary>
    /// Manages UDP peer-to-peer connection and input exchange.
    /// Implements rollback netcode with 2-frame input delay.
    /// </summary>
    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }

        [Header("Settings")]
        public int port = 7777;
        public int rollbackFrames = 2;
        public int inputBufferFrames = 8;

        // Connection state
        public enum ConnectionState { Disconnected, Connecting, Connected }
        public ConnectionState State { get; private set; } = ConnectionState.Disconnected;
        public bool IsHost { get; private set; }
        public int PingMs { get; private set; }
        public bool IsRollingBack { get; private set; }
        public int RollbackFrames => rollbackFrames;

        // Events
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<int> OnRollback;

        // Networking
        private UdpClient udpClient;
        private IPEndPoint remoteEndPoint;
        private Thread receiveThread;
        private bool running;

        // Input sync
        private InputData[] localInputBuffer = new InputData[256];
        private InputData[] remoteInputBuffer = new InputData[256];
        private bool[] remoteInputReceived = new bool[256];
        private int latestRemoteFrame = -1;

        public InputData[] LocalInputBuffer => localInputBuffer;
        public InputData[] RemoteInputBuffer => remoteInputBuffer;

        // Checksum sync
        private uint lastLocalChecksum;
        private uint lastRemoteChecksum;
        public uint LastLocalChecksum => lastLocalChecksum;
        public uint LastRemoteChecksum => lastRemoteChecksum;
        public bool ChecksumMismatch { get; private set; }

        // Ping measurement
        private long lastPingSendTime;
        private int pingInterval = 30; // frames

        // Rollback
        private RollbackManager rollbackManager = new RollbackManager();
        private GameManager gameManager;

        public int LatestRemoteFrame => latestRemoteFrame;
        public RollbackManager Rollback => rollbackManager;

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
        }

        public void SetGameManager(GameManager gm)
        {
            gameManager = gm;
        }

        /// <summary>
        /// Host a game (listen for connections).
        /// </summary>
        public void Host()
        {
            IsHost = true;
            udpClient = new UdpClient(port);
            remoteEndPoint = null;
            running = true;
            StartReceiveThread();
            State = ConnectionState.Connecting;
            Debug.Log($"Hosting on port {port}");
        }

        /// <summary>
        /// Join a game by connecting to host IP.
        /// </summary>
        public void Join(string hostIp)
        {
            IsHost = false;
            udpClient = new UdpClient(0); // Any available port
            IPAddress[] addresses = Dns.GetHostAddresses(hostIp);
            if (addresses.Length == 0)
            {
                Debug.LogError("Could not resolve host: " + hostIp);
                return;
            }
            remoteEndPoint = new IPEndPoint(addresses[0], port);
            running = true;
            StartReceiveThread();

            // Send connection request
            byte[] connectMsg = new byte[] { 0xFF }; // Magic connect byte
            udpClient.Send(connectMsg, connectMsg.Length, remoteEndPoint);

            State = ConnectionState.Connecting;
            Debug.Log($"Connecting to {hostIp}:{port}");
        }

        /// <summary>
        /// Disconnect from the game.
        /// </summary>
        public void Disconnect()
        {
            running = false;
            State = ConnectionState.Disconnected;

            if (receiveThread != null && receiveThread.IsAlive)
            {
                receiveThread.Join(1000);
            }

            if (udpClient != null)
            {
                try { udpClient.Close(); }
                catch { }
            }

            rollbackManager.Clear();
            OnDisconnected?.Invoke();
            Debug.Log("Disconnected");
        }

        private void StartReceiveThread()
        {
            receiveThread = new Thread(ReceiveLoop);
            receiveThread.IsBackground = true;
            receiveThread.Start();
        }

        private void ReceiveLoop()
        {
            while (running)
            {
                try
                {
                    if (udpClient.Available > 0)
                    {
                        IPEndPoint senderEp = new IPEndPoint(IPAddress.Any, 0);
                        byte[] data = udpClient.Receive(ref senderEp);

                        if (data.Length == 1 && data[0] == 0xFF && State == ConnectionState.Connecting)
                        {
                            // Connection accepted
                            if (remoteEndPoint == null)
                                remoteEndPoint = senderEp;
                            State = ConnectionState.Connected;
                            UnityMainThread.Enqueue(() => OnConnected?.Invoke());
                            Debug.Log("Connected!");
                            continue;
                        }

                        if (data.Length == 1 && data[0] == 0xFE)
                        {
                            // Ping response
                            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                            PingMs = (int)(now - lastPingSendTime);
                            continue;
                        }

                        if (data.Length == 8 && data[0] == 0xFD)
                        {
                            // Ping request - respond
                            byte[] pong = new byte[] { 0xFE };
                            udpClient.Send(pong, pong.Length, senderEp);
                            continue;
                        }

                        // Input data: [frame(4 bytes), buttons(1 byte), checksum(4 bytes)]
                        if (data.Length >= 5)
                        {
                            int frame = BitConverter.ToInt32(data, 0);
                            byte buttons = data[4];

                            remoteInputBuffer[frame % 256] = new InputData
                            {
                                frame = frame,
                                buttons = buttons
                            };
                            remoteInputReceived[frame % 256] = true;

                            if (frame > latestRemoteFrame)
                                latestRemoteFrame = frame;

                            if (data.Length >= 9)
                            {
                                lastRemoteChecksum = BitConverter.ToUInt32(data, 5);
                            }
                        }
                    }
                    else
                    {
                        Thread.Sleep(1); // Avoid busy waiting
                    }
                }
                catch (SocketException)
                {
                    if (running) Debug.Log("Socket error - connection lost");
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
            }
        }

        private void Update()
        {
            if (State != ConnectionState.Connected) return;

            // Send ping periodically
            if (Time.frameCount % pingInterval == 0 && remoteEndPoint != null)
            {
                lastPingSendTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                byte[] ping = new byte[] { 0xFD };
                try { udpClient.Send(ping, ping.Length, remoteEndPoint); }
                catch { }
            }
        }

        /// <summary>
        /// Record local input for the current frame.
        /// </summary>
        public void RecordLocalInput(int frame, InputData input)
        {
            localInputBuffer[frame % 256] = input;
        }

        /// <summary>
        /// Send all un-acknowledged inputs to peer.
        /// </summary>
        public void SendInputs(int currentFrame, uint checksum)
        {
            if (remoteEndPoint == null || udpClient == null) return;

            lastLocalChecksum = checksum;

            // Send last N frames of input for reliability
            int startFrame = Math.Max(0, currentFrame - inputBufferFrames);

            for (int f = startFrame; f <= currentFrame; f++)
            {
                InputData input = localInputBuffer[f % 256];
                byte[] data = new byte[9];
                Buffer.BlockCopy(BitConverter.GetBytes(input.frame), 0, data, 0, 4);
                data[4] = input.buttons;
                Buffer.BlockCopy(BitConverter.GetBytes(checksum), 0, data, 5, 4);

                try { udpClient.Send(data, data.Length, remoteEndPoint); }
                catch { }
            }
        }

        /// <summary>
        /// Get the remote player's input for a specific frame.
        /// Returns true if input is available (received or predicted).
        /// </summary>
        public bool GetRemoteInput(int frame, out InputData input)
        {
            if (remoteInputReceived[frame % 256])
            {
                input = remoteInputBuffer[frame % 256];
                return true;
            }

            // Predict using last known input
            if (latestRemoteFrame >= 0)
            {
                input = remoteInputBuffer[latestRemoteFrame % 256];
                return true;
            }

            input = InputData.Create(frame);
            return false;
        }

        /// <summary>
        /// Check if we should rollback (remote input arrived that differs from prediction).
        /// Returns the earliest frame that needs re-simulation.
        /// </summary>
        public int CheckForRollback(int currentFrame)
        {
            // Check if any recent remote inputs arrived that differ from what we predicted
            for (int f = currentFrame - rollbackFrames; f <= currentFrame; f++)
            {
                if (f >= 0 && remoteInputReceived[f % 256])
                {
                    // Input arrived - need to check if it matches prediction
                    // This will be handled by GameManager comparing with rollback state
                    return f;
                }
            }
            return -1;
        }

        /// <summary>
        /// Save game state for potential rollback.
        /// </summary>
        public void SaveGameState(int frame, GameState state)
        {
            rollbackManager.SaveState(frame, state);
        }

        /// <summary>
        /// Load a previous game state for rollback.
        /// </summary>
        public bool TryLoadGameState(int frame, out GameState state)
        {
            return rollbackManager.TryLoadState(frame, out state);
        }

        /// <summary>
        /// Notify listeners that a rollback occurred.
        /// </summary>
        public void NotifyRollback(int frames)
        {
            OnRollback?.Invoke(frames);
        }

        private void OnDestroy()
        {
            Disconnect();
        }

        private void OnApplicationQuit()
        {
            Disconnect();
        }
    }

    /// <summary>
    /// Helper to enqueue actions to the main thread from background threads.
    /// </summary>
    public static class UnityMainThread
    {
        private static readonly System.Collections.Generic.Queue<Action> queue =
            new System.Collections.Generic.Queue<Action>();
        private static readonly object lockObj = new object();

        public static void Enqueue(Action action)
        {
            lock (lockObj)
            {
                queue.Enqueue(action);
            }
        }

        public static void Update()
        {
            lock (lockObj)
            {
                while (queue.Count > 0)
                {
                    queue.Dequeue()?.Invoke();
                }
            }
        }
    }
}
