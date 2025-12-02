using System;
using System.Net;
using System.Threading;

namespace Game
{
    [Serializable] // For visualizing within Unity Inspector
    public class ClientConnection
    {
        public string name;
        public IPEndPoint endPoint;

        // Timeout-related variables
        public CancellationTokenSource connectionToken;
        public float lastPing;
        public short consecutiveTimeouts = 0;
        
        public ClientConnection(IPEndPoint endPoint)
        {
            this.endPoint = endPoint;
        }
    }
}