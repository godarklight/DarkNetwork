using System;
using System.Collections.Generic;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace DarkNetwork
{
    public class NetworkClient<T>
    {
        public static MessageThrowBehaviour messageThrowBehaviour = MessageThrowBehaviour.DISCONNECT;
        //10MB default
        public static int MAX_MESSAGE_SIZE = 10000000;
        public T stateObject;
        //Server tracking
        internal NetworkServer<T> networkServer;
        //Connection state
        internal NetworkHandler<T> handler;
        private TcpClient tcpClient;
        private object disconnectLock = new object();
        private bool isLittleEndian;
        public bool Connected
        {
            get
            {
                return tcpClient != null;
            }
        }
        //Receive message state
        private bool receivingHeader;
        private int receivingType;
        private byte[] receivingBytes;
        private int receivingBytesLeft;
        //Send message state
        private Queue<NetworkMessage> outgoingQueue = new Queue<NetworkMessage>();
        private AutoResetEvent are = new AutoResetEvent(false);
        //Heartbeat / timeout tracking
        public long lastReceiveTime
        {
            private set;
            get;
        }

        public long lastSendTime
        {
            private set;
            get;
        }
        //Connection tracking
        public long sentBytes
        {
            private set;
            get;
        }

        public long receivedBytes
        {
            private set;
            get;
        }

        public long queuedBytes
        {
            private set;
            get;
        }

        public long sentMessages
        {
            private set;
            get;
        }

        public long receivedMessages
        {
            private set;
            get;
        }

        public long queuedMessages
        {
            private set;
            get;
        }

        public NetworkClient(NetworkHandler<T> handler, TcpClient tcpClient, bool littleEndian)
        {
            lastSendTime = DateTime.UtcNow.Ticks;
            lastReceiveTime = lastSendTime;
            this.isLittleEndian = littleEndian;
            this.handler = handler;
            this.tcpClient = tcpClient;
            ReceiveNewMessage();
            handler.FireConnectCallback(this, tcpClient);
            //Start
            Thread sendThread = new Thread(new ThreadStart(SendThread));
            sendThread.IsBackground = true;
            sendThread.Start();
            Thread receiveThread = new Thread(new ThreadStart(ReceiveThread));
            receiveThread.IsBackground = true;
            receiveThread.Start();
        }

        private void ReceiveNewMessage()
        {
            receivingHeader = true;
            receivingType = 0;
            receivingBytes = new byte[8];
            receivingBytesLeft = 8;
        }

        private void SendThread()
        {
            while (tcpClient != null)
            {
                if (are.WaitOne(1000))
                {
                    if (tcpClient != null)
                    {
                        bool messagesToSend = true;
                        while (messagesToSend)
                        {
                            NetworkMessage sendMessage = null;
                            lock (outgoingQueue)
                            {
                                if (outgoingQueue.Count > 0)
                                {
                                    sendMessage = outgoingQueue.Dequeue();
                                    if (sendMessage.data == null)
                                    {
                                        queuedBytes -= 8;
                                    }
                                    else
                                    {
                                        queuedBytes -= 8 + sendMessage.data.Length;
                                    }
                                    queuedMessages--;
                                }
                                else
                                {
                                    messagesToSend = false;
                                }
                            }
                            if (sendMessage != null)
                            {
                                SendNetworkMessage(sendMessage);
                            }
                        }
                    }
                }
                else
                {
                    CheckHeartbeat();
                }
            }
        }

        private void SendNetworkMessage(NetworkMessage sendMessage)
        {
            byte[] sendBytes = sendMessage.GetBytes(isLittleEndian);
            try
            {
                tcpClient.GetStream().Write(sendBytes, 0, sendBytes.Length);
                sentBytes += sendBytes.Length;
                sentMessages++;
                handler.FireSentCallback(this, sendMessage);
            }
            catch (Exception e)
            {
                Disconnect(e);
            }
            lastSendTime = DateTime.UtcNow.Ticks;
        }

        private void CheckHeartbeat()
        {
            if (tcpClient != null)
            {
                if (handler.heartbeatInterval > 0)
                {
                    if ((DateTime.UtcNow.Ticks - lastSendTime) > handler.heartbeatInterval)
                    {
                        NetworkMessage heartbeatMessage = handler.FireHeartbeatCallback(this);
                        if (heartbeatMessage != null)
                        {
                            SendNetworkMessage(heartbeatMessage);
                        }
                    }
                }
                if (handler.timeoutInterval > 0)
                {
                    if ((DateTime.UtcNow.Ticks - lastReceiveTime) > handler.timeoutInterval)
                    {
                        Exception errorException = new Exception("Receive timeout");
                        if (messageThrowBehaviour == MessageThrowBehaviour.CRASH)
                        {
                            throw errorException;
                        }
                        if (messageThrowBehaviour == MessageThrowBehaviour.DISCONNECT)
                        {
                            Disconnect(errorException);
                        }
                    }
                }
            }
        }

        private void ReceiveThread()
        {
            while (tcpClient != null)
            {
                int readBytes = 0;
                bool connectionError = false;
                try
                {
                    readBytes = tcpClient.GetStream().Read(receivingBytes, receivingBytes.Length - receivingBytesLeft, receivingBytesLeft);
                }
                catch (Exception e)
                {
                    Disconnect(e);
                    connectionError = true;
                }
                if (!connectionError)
                {
                    if (readBytes > 0)
                    {
                        lastReceiveTime = DateTime.UtcNow.Ticks;
                        receivedBytes += readBytes;
                        receivingBytesLeft -= readBytes;
                        if (receivingBytesLeft == 0)
                        {
                            if (receivingHeader)
                            {
                                int tempLength = 0;
                                if (isLittleEndian != BitConverter.IsLittleEndian)
                                {
                                    //Endian flip
                                    Array.Reverse(receivingBytes);
                                    tempLength = BitConverter.ToInt32(receivingBytes, 0);
                                    receivingType = BitConverter.ToInt32(receivingBytes, 4);
                                }
                                else
                                {
                                    //No endian flip
                                    receivingType = BitConverter.ToInt32(receivingBytes, 0);
                                    tempLength = BitConverter.ToInt32(receivingBytes, 4);
                                }
                                //Check that the size is within bounds
                                if (tempLength >= 0 && tempLength <= MAX_MESSAGE_SIZE)
                                {
                                    if (tempLength == 0)
                                    {
                                        HandleMessage(receivingType, null);
                                    }
                                    else
                                    {
                                        receivingHeader = false;
                                        receivingBytes = new byte[tempLength];
                                        receivingBytesLeft = tempLength;
                                    }
                                }
                                else
                                {
                                    Exception errorException = new Exception("Message size " + tempLength + " out of bounds");
                                    if (messageThrowBehaviour == MessageThrowBehaviour.CRASH)
                                    {
                                        throw errorException;
                                    }
                                    if (messageThrowBehaviour == MessageThrowBehaviour.DISCONNECT)
                                    {
                                        Disconnect(errorException);
                                    }
                                }
                            }
                            else
                            {
                                HandleMessage(receivingType, receivingBytes);
                            }
                        }
                    }
                    else
                    {
                        //I *think* read can whack out and put the CPU into a busy loop if we don't do this.
                        Thread.Sleep(10);
                    }
                }
            }
        }

        private void HandleMessage(int messageType, byte[] messageBytes)
        {
            if (messageThrowBehaviour == MessageThrowBehaviour.CRASH)
            {
                handler.FireMessageCallback(this, messageType, messageBytes);
                if (!handler.IsRegistered(messageType))
                {
                    throw new Exception("Message type " + messageType + " is not registered");
                }
            }
            else
            {
                try
                {
                    handler.FireMessageCallback(this, messageType, messageBytes);
                    if (!handler.IsRegistered(messageType))
                    {
                        throw new Exception("Message type " + messageType + " is not registered");
                    }
                }
                catch (Exception e)
                {
                    if (messageThrowBehaviour == MessageThrowBehaviour.DISCONNECT)
                    {
                        Disconnect(e);
                    }
                }
            }
            ReceiveNewMessage();
            receivedMessages++;
        }

        public void QueueNetworkMessage(NetworkMessage outgoingMessage)
        {
            if (tcpClient != null)
            {
                lock (outgoingQueue)
                {
                    outgoingQueue.Enqueue(outgoingMessage);
                    if (outgoingMessage.data == null)
                    {
                        queuedBytes += 8;
                    }
                    else
                    {
                        queuedBytes += 8 + outgoingMessage.data.Length;
                    }
                    queuedMessages++;
                }
                are.Set();
            }
        }

        public void TransferToServer(NetworkServer<T> networkServer)
        {
            lock (disconnectLock)
            {
                if (tcpClient != null)
                {
                    if (networkServer != null)
                    {
                        networkServer.RemoveNetworkClient(this);
                    }
                    networkServer.AddNetworkClient(this);
                }
            }
        }

        public void Disconnect()
        {
            Disconnect(null);
        }

        private void Disconnect(Exception disconnectException)
        {
            lock (disconnectLock)
            {
                if (tcpClient != null)
                {
                    handler.FireDisconnectCallback(this, disconnectException);
                    tcpClient.Close();
                    tcpClient = null;
                    are.Set();
                    if (networkServer != null)
                    {
                        networkServer.RemoveNetworkClient(this);
                    }
                }
            }
        }
    }

    public enum MessageThrowBehaviour
    {
        CRASH,
        IGNORE,
        DISCONNECT,
    }
}

