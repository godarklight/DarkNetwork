using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using DarkNetwork;

namespace DarkNetworkTester
{
    public class MainClass
    {
        int freeClientID = 0;
        public static void Main()
        {
            MainClass mainClass = new MainClass();
            NetworkServer<TrackingObject> networkServer = mainClass.RunServer();
            NetworkClient<TrackingObject> networkClient0 = mainClass.Run();
            Thread.Sleep(100);
            NetworkClient<TrackingObject> networkClient1 = mainClass.Run();
            Thread.Sleep(100);
            NetworkClient<TrackingObject> networkClient2 = mainClass.Run();
            Thread.Sleep(100);
            NetworkClient<TrackingObject> networkClient3 = mainClass.Run();
            Thread.Sleep(100);
            byte[] messageBytes = new byte[8];
            BitConverter.GetBytes(9000).CopyTo(messageBytes, 0);
            BitConverter.GetBytes(1).CopyTo(messageBytes, 4);
            NetworkMessage newMessage = new NetworkMessage(1, messageBytes);
            networkClient0.QueueNetworkMessage(newMessage);
            networkClient1.Disconnect();
            networkClient2.QueueNetworkMessage(new NetworkMessage(2, null));
            networkServer.QueueToAll(new NetworkMessage(1, BitConverter.GetBytes(8999)));
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }

        public NetworkClient<TrackingObject> Run()
        {
            ClientMessages cm = new ClientMessages();
            NetworkHandler<TrackingObject> networkHandler = new NetworkHandler<TrackingObject>();
            networkHandler.SetConnectCallback(cm.ConnectCallback);
            networkHandler.SetHeartbeatCallback(cm.HeartbeatCallback, 10000, 40000);
            networkHandler.SetMessageCallback(0, cm.ReceiveHeartbeatMessage);
            networkHandler.SetMessageCallback(1, cm.ReceiveAnswerMessage);
            networkHandler.SetDisconnectCallback(cm.DisconnectCallback);
            TcpClient tcpClient = new TcpClient("::1", 9005);
            NetworkClient<TrackingObject> newClient = new NetworkClient<TrackingObject>(networkHandler, tcpClient, false);
            return newClient;
        }

        public NetworkServer<TrackingObject> RunServer()
        {
            ServerMessages sm = new ServerMessages();
            NetworkHandler<TrackingObject> serverHandler = new NetworkHandler<TrackingObject>();
            serverHandler.SetConnectCallback(sm.ConnectCallback);
            serverHandler.SetHeartbeatCallback(sm.HeartbeatCallback, 10000, 40000);
            serverHandler.SetMessageCallback(0, sm.ReceiveHeartbeatMessage);
            serverHandler.SetMessageCallback(1, sm.ReceiveCalculateMessage);
            serverHandler.SetDisconnectCallback(sm.DisconnectCallback);
            NetworkServer<TrackingObject> networkServer = new NetworkServer<TrackingObject>(serverHandler, false);
            networkServer.Start(new IPEndPoint(IPAddress.IPv6Any, 9005));
            return networkServer;
        }
    }
}

