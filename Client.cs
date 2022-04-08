using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Indus_Proxy_Server_Library
{
    public delegate void PacketHandler(Packet packet, Node node);
    public delegate void NodeHandler(Node node);

    public class Client
    {
        protected event PacketHandler _PacketReceived;
        protected event NodeHandler _ClientConnected;
        protected event NodeHandler _ClientDisconnected;

        public event PacketHandler PacketReceived;
        public event NodeHandler ClientConnected;
        public event NodeHandler ClientDisconnected;

        protected UdpClient client;

        static readonly int heartbeat = 5 * 1000;

        List<Node> nodes = new List<Node>();
        List<Node> waitForDisconnects = new List<Node>();

        public Client(UdpClient client)
        {
            this.client = client;
            new Thread(delegate ()
            {
                while (true)
                    Receive();
            }).Start();

            new Thread(delegate ()
            {
                while (true)
                {
                    foreach (Node node in waitForDisconnects)
                    {
                        nodes.Remove(node);
                        _ClientDisconnected?.Invoke(node);
                        ClientDisconnected?.Invoke(node);
                    }
                    
                    waitForDisconnects = new List<Node>();

                    foreach (Node node in nodes)
                    {
                        SendPacket(Packet.heartbeat, node);
                        waitForDisconnects.Add(node);
                    }
                    Thread.Sleep(heartbeat);
                }
            }).Start();
        }

        void Receive()
        {
            IPEndPoint remoteEndPoint = null;
            byte[] receiveBytes = null;
            try
            {
                receiveBytes = client.Receive(ref remoteEndPoint);
            }
            catch (Exception)
            {

            }

            if (receiveBytes == null)
                return;

            if (!nodes.Contains(remoteEndPoint))
            {
                nodes.Add(remoteEndPoint);
                _ClientConnected?.Invoke(remoteEndPoint);
                ClientConnected?.Invoke(remoteEndPoint);
            }

            waitForDisconnects.Remove(remoteEndPoint);

            Packet packet = Encoding.UTF8.GetString(receiveBytes).FromJson<Packet>();

            _PacketReceived?.Invoke(packet, remoteEndPoint);
            PacketReceived?.Invoke(packet, remoteEndPoint);
        }

        public void SendPacket(Packet packet, IPEndPoint endPoint)
        {
            byte[] data = Encoding.UTF8.GetBytes(packet.ToJson());
            client.Send(data, data.Length, endPoint);
        }
    }
}
