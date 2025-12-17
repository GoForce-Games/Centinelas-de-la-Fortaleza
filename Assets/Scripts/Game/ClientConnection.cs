using System;
using System.Collections.Generic;
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
        public CancellationTokenSource connectionToken =  new CancellationTokenSource();
        public float lastSent;
        public float lastReceived;
        public short consecutiveTimeouts = 0;
        
        // UDP reliability - Sent to client
        public List<PendingMessage> ackPending = new List<PendingMessage>();
        public int lastIdReceived;
        public float lastAckTimeReceived;
        
        // UDP reliability - Received from client
        public List<int> ackedIds = new List<int>();
        
        public ClientConnection(IPEndPoint endPoint)
        {
            this.endPoint = endPoint;
        }
    }
}