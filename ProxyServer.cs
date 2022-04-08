using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Indus_Proxy_Server_Library
{
    public class ProxyServer : Client
    {
        public static readonly IPEndPoint address = Utility.GetIP("hps.luluv.me", 21023);

        Dictionary<Node, ServerData> servers = new Dictionary<Node, ServerData>();

        public ProxyServer(UdpClient client) : base(client)
        {
            _ClientDisconnected += delegate (Node node)
            {
                if(servers.ContainsKey(node))
                    servers.Remove(node);
            };
            _PacketReceived += Server_PacketReceived;
        }

        void Server_PacketReceived(Packet packet, Node node)
        {
            switch (packet.type)
            {
                case Packet.Type.RequestCreateServer:
                    ServerData serverData = packet.data.FromJson<ServerData>();
                    serverData.externalAddress = node;
                    servers.Add(node, serverData);
                    break;
                case Packet.Type.RequestServerList:
                    SendPacket(new Packet(Packet.Type.SendServerList, servers.Values.ToJson()), node);
                    break;
            }
        }
    }
}
