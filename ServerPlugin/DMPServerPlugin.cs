using System;
using System.IO;
using DarkMultiPlayerServer;
using MessageStream2;

namespace DMPCkanPlugin
{
    public class DMPServerPlugin : DMPPlugin
    {
        public const int CKAN_PROTOCOL_VERSION = 1;
        private static string ckanDirectory = Path.Combine(Server.universeDirectory, "Plugins", "CKAN");
        private static string ckanFile = Path.Combine(ckanDirectory, "DMPServer.ckan");

        public override void OnServerStart()
        {
            if (!Directory.Exists(ckanDirectory))
            {
                DarkLog.Debug("Created CKAN directory.");
                Directory.CreateDirectory(ckanDirectory);
            }
            if (!File.Exists(ckanFile))
            {
                DarkLog.Error("To enable CKAN support please export your installed mods from the ckan file -> export mods option.");
                DarkLog.Error("The CKAN file must be named DMPServer.ckan and placed in the Plugins/CKAN/ folder");
            }
            DMPModInterface.RegisterModHandler("DMPCkanPlugin", OnCKANRequest);
        }

        public override void OnServerStop()
        {
            DMPModInterface.UnregisterModHandler("DMPCkanPlugin");
        }

        //Receive message data (PROTOCOL 1):
        //int ckan protocol version
        //int ckan request type:
        //1: CKAN metadata file

        //Send message data (PROTOCOL 1):
        //int ckan protocol version
        //bool ckan available (true/false)
        //if ckan available: UTF-8 string (ckan export data)
        private void OnCKANRequest(ClientObject client, byte[] messageData)
        {
            using (MessageReader mr = new MessageReader(messageData))
            {
                int clientProtocol = mr.Read<int>();
                if (clientProtocol != CKAN_PROTOCOL_VERSION)
                {
                    DarkLog.Error("Client " + client.playerName + " connected with CKAN protocol " + clientProtocol + ", server version: " + CKAN_PROTOCOL_VERSION);
                    return;
                }
                int requestType = mr.Read<int>();
                switch (requestType)
                {
                    case 1:
                        SendCKANFileToClient(client);
                        break;
                    default:
                        DarkLog.Error("Unknown CKAN request type: " + requestType);
                        break;
                }
            }

        }

        private void SendCKANFileToClient(ClientObject client)
        {
            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<int>(CKAN_PROTOCOL_VERSION);
                if (!File.Exists(ckanFile))
                {
                    mw.Write<bool>(false);
                    byte[] sendData = mw.GetMessageBytes();
                    DMPModInterface.SendDMPModMessageToClient(client, "DMPCkanPlugin", sendData, false);
                    return;
                }
                mw.Write<bool>(true);
                mw.Write<string>(File.ReadAllText(ckanFile));
            }
            DarkLog.Debug("Sent CKAN file to " + client.playerName);
        }
    }
}

