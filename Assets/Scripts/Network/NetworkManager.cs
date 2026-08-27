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

        // Connection state
        public enum ConnectionState { Disconnected, Connecting, Connected }
        public ConnectionState State { get; private set; } = ConnectionState.Disconnected;
        public bool IsHost { get; private set; }
        public int PingMs { get; private set; }

        // Events
        public event Action OnConnected;
        public event Action OnDisconnected;

        // Networking
        private UdpClient udpClient;
        private IPEndPoint remoteEndPoint;
        private Thread receiveThread;
        private volatile bool running;

        // Input sync - ring buffers
        private const int BUFFER_SIZE = 256;
        private InputData[] localInputBuffer = new InputData[BUFFER_SIZE];
        private InputData[] remoteInputBuffer = new InputData[BUFFER_SIZE];
        private bool[] remoteInputReceived = new bool[BUFFER_SIZE];
        private int latestRemoteFrame = -1;

        public InputData[] LocalInputBuffer => localInputBuffer;
        public InputData[] RemoteInputBuffer => remoteInputBuffer;

        // Checksum sync
        private uint[] localChecksumBuffer = new uint[BUFFER_SIZE];
        private bool[] localChecksumSet = new bool[BUFFER_SIZE];
        private uint lastLocalChecksum;
        private uint lastRemoteChecksum;
        private int lastRemoteChecksumFrame = -1;
        public uint LastLocalChecksum => lastLocalChecksum;
        public uint LastRemoteChecksum => lastRemoteChecksum;
        public bool ChecksumMismatch { get; private set; }

        // Ping measurement
        private long lastPingSendTime;
        private int pingInterval = 60; // frames (~1 second)

        // Disconnect detection
        private long lastReceiveTimeMs;
        private const int DISCONNECT_TIMEOUT_MS = 5000; // 5 seconds

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

        public void Host()
        {
            try
            {
                IsHost = true;
                udpClient = new UdpClient(port);
                udpClient.Client.ReceiveTimeout = 1000;
                remoteEndPoint = null;
                running = true;
                lastReceiveTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                StartReceiveThread();
                State = ConnectionState.Connecting;
                Debug.Log($"Hosting on port {port}");
            }
            catch (System.Net.Sockets.SocketException e)
            {
                Debug.LogError($"Failed to host on port {port}: {e.Message}");
                State = ConnectionState.Disconnected;
            }
        }

        public void Join(string hostIp)
        {
            IsHost = false;
            udpClient = new UdpClient(0);
            udpClient.Client.ReceiveTimeout = 1000;
            IPAddress[] addresses = Dns.GetHostAddresses(hostIp);
            if (addresses.Length == 0)
            {
                Debug.LogError("Could not resolve host: " + hostIp);
                return;
            }
            remoteEndPoint = new IPEndPoint(addresses[0], port);
            running = true;
            lastReceiveTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            StartReceiveThread();

            byte[] connectMsg = new byte[] { 0xFF };
            udpClient.Send(connectMsg, connectMsg.Length, remoteEndPoint);

            State = ConnectionState.Connecting;
            Debug.Log($"Connecting to {hostIp}:{port}");
        }

        public void Disconnect()
        {
            if (State == ConnectionState.Disconnected) return;

            running = false;
            State = ConnectionState.Disconnected;

            // Notify peer
            if (udpClient != null && remoteEndPoint != null)
            {
                try { udpClient.Send(new byte[] { 0xFC }, 1, remoteEndPoint); }
                catch { }
            }

            if (receiveThread != null && receiveThread.IsAlive)
            {
                receiveThread.Join(500);
            }

            if (udpClient != null)
            {
                try { udpClient.Close(); }
                catch { }
                udpClient = null;
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
            IPEndPoint senderEp = new IPEndPoint(IPAddress.Any, 0);

            while (running)
            {
                try
                {
                    byte[] data = udpClient.Receive(ref senderEp);
                    lastReceiveTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                    // Connection accepted (0xFF)
                    if (data.Length == 1 && data[0] == 0xFF && State == ConnectionState.Connecting)
                    {
                        if (remoteEndPoint == null)
                            remoteEndPoint = senderEp;
                        State = ConnectionState.Connected;
                        UnityMainThread.Enqueue(() => OnConnected?.Invoke());
                        Debug.Log("Connected!");
                        continue;
                    }

                    // Disconnect notification (0xFC)
                    if (data.Length == 1 && data[0] == 0xFC)
                    {
                        Debug.Log("Peer disconnected");
                        UnityMainThread.Enqueue(() => Disconnect());
                        continue;
                    }

                    // Ping response (0xFE)
                    if (data.Length == 1 && data[0] == 0xFE)
                    {
                        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        PingMs = (int)(now - lastPingSendTime);
                        continue;
                    }

                    // Ping request (0xFD) - respond
                    if (data.Length == 1 && data[0] == 0xFD)
                    {
                        udpClient.Send(new byte[] { 0xFE }, 1, senderEp);
                        continue;
                    }

                    // Input packet: [frame(4), buttons(1), checksum(4)] = 9 bytes
                    if (data.Length >= 5)
                    {
                        int frame = BitConverter.ToInt32(data, 0);
                        byte buttons = data[4];

                        remoteInputBuffer[frame % BUFFER_SIZE] = new InputData
                        {
                            frame = frame,
                            buttons = buttons
                        };
                        remoteInputReceived[frame % BUFFER_SIZE] = true;

                        if (frame > latestRemoteFrame)
                            latestRemoteFrame = frame;

                        if (data.Length >= 9)
                        {
                            uint remoteChecksum = BitConverter.ToUInt32(data, 5);
                            if (remoteChecksum != 0 && frame > lastRemoteChecksumFrame)
                            {
                                lastRemoteChecksum = remoteChecksum;
                                lastRemoteChecksumFrame = frame;
                                if (localChecksumSet[frame % BUFFER_SIZE])
                                    ChecksumMismatch = localChecksumBuffer[frame % BUFFER_SIZE] != remoteChecksum;
                            }
                        }
                    }
                }
                catch (SocketException)
                {
                    // ReceiveTimeout fires as SocketException — just loop
                    if (!running) break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
            }
        }

        private void Update()
        {
            if (State == ConnectionState.Disconnected) return;

            // Disconnect detection: no data for 5 seconds
            if (State == ConnectionState.Connected && remoteEndPoint != null)
            {
                long elapsed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - lastReceiveTimeMs;
                if (elapsed > DISCONNECT_TIMEOUT_MS)
                {
                    Debug.LogWarning($"No data received for {elapsed}ms — disconnecting");
                    Disconnect();
                    return;
                }
            }

            // Send ping periodically
            if (State == ConnectionState.Connected && Time.frameCount % pingInterval == 0 && remoteEndPoint != null)
            {
                lastPingSendTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                try { udpClient.Send(new byte[] { 0xFD }, 1, remoteEndPoint); }
                catch { }
            }
        }

        public void RecordLocalInput(int frame, InputData input)
        {
            localInputBuffer[frame % BUFFER_SIZE] = input;
        }

        public void RecordLocalChecksum(int frame, uint checksum)
        {
            localChecksumBuffer[frame % BUFFER_SIZE] = checksum;
            localChecksumSet[frame % BUFFER_SIZE] = true;
            lastLocalChecksum = checksum;

            if (frame == lastRemoteChecksumFrame)
                ChecksumMismatch = checksum != lastRemoteChecksum;
        }

        /// <summary>
        /// Send inputs to peer. Sends current frame + redundancy every 4 frames.
        /// </summary>
        public void SendInputs(int currentFrame)
        {
            if (remoteEndPoint == null || udpClient == null) return;

            // Always send current frame
            SendSingleInput(currentFrame);

            // Every 4 frames, also send the last 3 for redundancy (handles packet loss)
            if (currentFrame % 4 == 0)
            {
                for (int f = currentFrame - 3; f < currentFrame; f++)
                {
                    if (f >= 0)
                        SendSingleInput(f);
                }
            }
        }

        private void SendSingleInput(int frame)
        {
            InputData input = localInputBuffer[frame % BUFFER_SIZE];
            byte[] data = new byte[9];
            Buffer.BlockCopy(BitConverter.GetBytes(input.frame), 0, data, 0, 4);
            data[4] = input.buttons;

            // Only attach checksum if we have one for this frame
            if (localChecksumSet[frame % BUFFER_SIZE])
                Buffer.BlockCopy(BitConverter.GetBytes(localChecksumBuffer[frame % BUFFER_SIZE]), 0, data, 5, 4);

            try { udpClient.Send(data, data.Length, remoteEndPoint); }
            catch { }
        }

        public bool GetRemoteInput(int frame, out InputData input)
        {
            if (remoteInputReceived[frame % BUFFER_SIZE])
            {
                input = remoteInputBuffer[frame % BUFFER_SIZE];
                return true;
            }

            // Predict using last known input
            if (latestRemoteFrame >= 0)
            {
                input = remoteInputBuffer[latestRemoteFrame % BUFFER_SIZE];
                return true;
            }

            input = InputData.Create(frame);
            return false;
        }

        public int CheckForRollback(int currentFrame)
        {
            for (int f = currentFrame - rollbackFrames; f < currentFrame; f++)
            {
                if (f >= 0 && remoteInputReceived[f % BUFFER_SIZE])
                {
                    return f;
                }
            }
            return -1;
        }

        public void SaveGameState(int frame, GameState state)
        {
            rollbackManager.SaveState(frame, state);
        }

        public bool TryLoadGameState(int frame, out GameState state)
        {
            return rollbackManager.TryLoadState(frame, out state);
        }

        public void NotifyRollback(int frames)
        {
            Debug.Log($"Rollback: {frames} frames");
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
