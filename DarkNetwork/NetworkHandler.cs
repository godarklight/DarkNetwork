using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;

namespace DarkNetwork
{
    public class NetworkHandler<T>
    {
        private Dictionary<int, Action<NetworkClient<T>, byte[]>> messageCallbacks = new Dictionary<int, Action<NetworkClient<T>, byte[]>>();
        private Action<NetworkClient<T>, TcpClient> connectCallback = null;
        private Action<NetworkClient<T>, int, byte[]> unhandledMessage = null;
        private Action<NetworkClient<T>, Exception> disconnectCallback = null;
        private Func<NetworkClient<T>, NetworkMessage> heartbeatCallback = null;
        private Action<NetworkClient<T>, NetworkMessage> sentCallback = null;
        internal int heartbeatInterval
        {
            get;
            private set;
        }
        internal int timeoutInterval
        {
            get;
            private set;
        }

        public bool IsRegistered(int messageType)
        {
            return messageCallbacks.ContainsKey(messageType);
        }

        public void SetMessageCallback(int messageType, Action<NetworkClient<T>, byte[]> callback)
        {
            if (callback != null)
            {
                messageCallbacks[messageType] = callback;
            }
            else
            {
                if (IsRegistered(messageType))
                {
                    messageCallbacks.Remove(messageType);
                }
            }
        }

        public void SetConnectCallback(Action<NetworkClient<T>, TcpClient> connectCallback)
        {
            this.connectCallback = connectCallback;
        }

        public void SetUnhandledCallback(Action<NetworkClient<T>, int, byte[]> unhandledCallback)
        {
            this.unhandledMessage = unhandledCallback;
        }

        public void SetDisconnectCallback(Action<NetworkClient<T>, Exception> disconnectCallback)
        {
            this.disconnectCallback = disconnectCallback;
        }

        public void SetHeartbeatCallback(Func<NetworkClient<T>, NetworkMessage> heartbeatCallback, int heartbeatIntervalMs, int timeoutIntervalMs)
        {
            this.heartbeatInterval = heartbeatIntervalMs * 10000;
            this.timeoutInterval = timeoutIntervalMs * 10000;
            this.heartbeatCallback = heartbeatCallback;
        }

        public void SetSentCallback(Action<NetworkClient<T>, NetworkMessage> sentCallback)
        {
            this.sentCallback = sentCallback;
        }

        internal void FireConnectCallback(NetworkClient<T> client, TcpClient networkConnection)
        {
            if (connectCallback != null)
            {
                connectCallback(client, networkConnection);
            }
        }

        internal void FireMessageCallback(NetworkClient<T> client, int messageType, byte[] data)
        {
            if (messageCallbacks.ContainsKey(messageType))
            {
                if (messageCallbacks[messageType] != null)
                {
                    messageCallbacks[messageType](client, data);
                }
            }
            else
            {
                FireUnhandledCallback(client, messageType, data);
            }
        }

        private void FireUnhandledCallback(NetworkClient<T> client, int messageType, byte[] data)
        {
            if (unhandledMessage != null)
            {
                unhandledMessage(client, messageType, data);
            }
        }

        internal void FireDisconnectCallback(NetworkClient<T> client, Exception disconnectException)
        {
            if (disconnectCallback != null)
            {
                disconnectCallback(client, disconnectException);
            }
        }

        internal NetworkMessage FireHeartbeatCallback(NetworkClient<T> client)
        {
            if (heartbeatCallback != null)
            {
                return heartbeatCallback(client);
            }
            return null;
        }

        internal void FireSentCallback(NetworkClient<T> client, NetworkMessage networkMessage)
        {
            if (sentCallback != null)
            {
                sentCallback(client, networkMessage);
            }
        }
    }
}

