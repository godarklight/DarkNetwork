using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace DarkNetwork
{
    public class NetworkServer<T>
    {
        private TcpListener tcpListener;
        private List<NetworkClient<T>> clients = new List<NetworkClient<T>>();
        private NetworkHandler<T> networkHandler;
        private bool littleEndian;
        public int ConnectCount
        {
            get
            {
                return clients.Count;
            }
        }

        public NetworkServer(NetworkHandler<T> networkHandler, bool littleEndian)
        {
            this.networkHandler = networkHandler;
            this.littleEndian = littleEndian;
        }

        public void Start(IPEndPoint bindAddress)
        {
            tcpListener = new TcpListener(bindAddress);
            tcpListener.Start();
            tcpListener.BeginAcceptTcpClient(ConnectionCallback, null);
        }

        public void ConnectionCallback(IAsyncResult ar)
        {
            TcpClient newClient = null;
            try
            {
                newClient = tcpListener.EndAcceptTcpClient(ar);
                NetworkClient<T> newNetworkClient = new NetworkClient<T>(networkHandler, newClient, littleEndian);
                AddNetworkClient(newNetworkClient);
            }
            catch
            {
                //I don't think we care.
                newClient.Close();
            }
            tcpListener.BeginAcceptTcpClient(ConnectionCallback, null);
        }

        public void AddNetworkClient(NetworkClient<T> client)
        {
            lock (clients)
            {
                if (!clients.Contains(client))
                {
                    clients.Add(client);
                }
            }
        }

        public void RemoveNetworkClient(NetworkClient<T> client)
        {
            lock (clients)
            {
                if (clients.Contains(client))
                {
                    clients.Remove(client);
                }
            }
        }

        public void QueueToAll(NetworkMessage networkMessage)
        {
            lock (clients)
            {
                foreach (NetworkClient<T> client in clients)
                {
                    client.QueueNetworkMessage(networkMessage);
                }
            }
        }
    }
}

