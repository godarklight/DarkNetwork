using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using DarkNetwork;

namespace DarkNetworkTester
{
    public class ServerMessages
    {
        public void ConnectCallback(NetworkClient<TrackingObject> client, TcpClient tcpClient)
        {
            client.stateObject = new TrackingObject();
            client.stateObject.thisID = Interlocked.Increment(ref TrackingObject.serverFreeID);
            client.stateObject.remoteEndpoint = (IPEndPoint)tcpClient.Client.RemoteEndPoint;
            Console.WriteLine("Server: ID " + client.stateObject.thisID + " connected from " + client.stateObject.remoteEndpoint);
        }

        public void ReceiveHeartbeatMessage(NetworkClient<TrackingObject> client, byte[] data)
        {
            Console.WriteLine("Server: Receive Heartbeat");
        }

        public void ReceiveCalculateMessage(NetworkClient<TrackingObject> client, byte[] data)
        {
            int var1 = BitConverter.ToInt32(data, 0);
            int var2 = BitConverter.ToInt32(data, 4);
            Console.WriteLine("Server: ID " + client.stateObject.thisID + " requested " + var1 + " + " + var2);
            NetworkMessage sendMessage = new NetworkMessage(1, BitConverter.GetBytes(var1 + var2));
            client.QueueNetworkMessage(sendMessage);
        }

        public NetworkMessage HeartbeatCallback(NetworkClient<TrackingObject> client)
        {
            Console.WriteLine("Server: Send Heartbeat");
            return new NetworkMessage(0, null);
        }

        public void DisconnectCallback(NetworkClient<TrackingObject> client, Exception disconnectException)
        {
            if (disconnectException == null)
            {
                Console.WriteLine("Server: ID " + client.stateObject.thisID + " disconnected from " + client.stateObject.remoteEndpoint);
            }
            else
            {
                Console.WriteLine("Server: ID " + client.stateObject.thisID + " disconnected from " + client.stateObject.remoteEndpoint + ", error: " + disconnectException.Message);
            }
        }
    }
}

