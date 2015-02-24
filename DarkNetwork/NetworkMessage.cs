using System;

namespace DarkNetwork
{
    public class NetworkMessage
    {
        public readonly int type;
        public readonly byte[] data;

        public NetworkMessage(int type, byte[] data)
        {
            this.type = type;
            this.data = data;
        }

        public byte[] GetBytes(bool littleEndian)
        {
            byte[] returnBytes = data != null ? new byte[8 + data.Length] : new byte[8];
            byte[] typeBytes = new byte[4];
            byte[] lengthBytes = new byte[4];
            BitConverter.GetBytes(type).CopyTo(typeBytes, 0);
            if (data != null)
            {
                BitConverter.GetBytes(data.Length).CopyTo(lengthBytes, 0);
                data.CopyTo(returnBytes, 8);
            }
            if (littleEndian != BitConverter.IsLittleEndian)
            {
                Array.Reverse(typeBytes);
                Array.Reverse(lengthBytes);
            }
            typeBytes.CopyTo(returnBytes, 0);
            lengthBytes.CopyTo(returnBytes, 4);
            return returnBytes;
        }
    }
}

