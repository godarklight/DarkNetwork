using System;
using System.Threading;
using System.Net.Sockets;
using DarkNetwork;

namespace DarkNetworkTester
{
    public class ClientMessages
    {
        public void ConnectCallback(NetworkClient<TrackingObject> client, TcpClient tcpClient)
        {
            client.stateObject = new TrackingObject();
            client.stateObject.thisID =  Interlocked.Increment(ref TrackingObject.clientFreeID);
        }

        public void ReceiveHeartbeatMessage(NetworkClient<TrackingObject> client, byte[] data)
        {
            Console.WriteLine("Client " + client.stateObject.thisID + ": Receive Heartbeat");
        }

        public void ReceiveAnswerMessage(NetworkClient<TrackingObject> client, byte[] data)
        {
            Console.WriteLine("Client " + client.stateObject.thisID + ": Got answer: " + BitConverter.ToInt32(data, 0));
        }

        public NetworkMessage HeartbeatCallback(NetworkClient<TrackingObject> client)
        {
            Console.WriteLine("Client " + client.stateObject.thisID + ": Send Heartbeat");
            return new NetworkMessage(0, null);
        }

        public void DisconnectCallback(NetworkClient<TrackingObject> client, Exception disconnectException)
        {
            if (disconnectException != null)
            {
                Console.WriteLine("Client " + client.stateObject.thisID + " disconnected");

            }
            else
            {
                Console.WriteLine("Client " + client.stateObject.thisID + " disconnected, error: " + disconnectException);
            }
        }
    }
}

