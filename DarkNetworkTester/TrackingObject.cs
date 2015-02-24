using System;
using System.Net;

namespace DarkNetworkTester
{
    public class TrackingObject
    {
        public static int clientFreeID = 0;
        public static int serverFreeID = 0;
        public int thisID = 0;
        public IPEndPoint remoteEndpoint;
    }
}

