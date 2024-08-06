using System.Net.Sockets;
using System.Text;

namespace UnoServer;

public class Commands(RemoteServer Server)
{

    public string Ping(TcpClient client)
    {
        Console.WriteLine($"Ping received from {Server.GetClientId(client)}");
        WriteResponse(client, $"Received your ping, client {Server.GetClientId(client)}");

        return $"Received your ping, client {Server.GetClientId(client)}";
    }

    public string RemoveClient(TcpClient client)
    {
        client.Close();
        Server.RemoveClient(client);
        Console.WriteLine($"Removed: {Server.GetClientId(client)} from client list. Client Disconnected...");

        return $"Server: Goodbye...";
    }

    private static void WriteResponse(TcpClient client,string message)
    {
        var stream = client.GetStream();

        if (stream == null)
        {
            Console.WriteLine("Commands::writeResponse:: client.GetStream() returned false");
            return;
        }
        
        var messageBytes = Encoding.ASCII.GetBytes(message);

        stream.Write(messageBytes, 0, messageBytes.Length);
    }
    
}