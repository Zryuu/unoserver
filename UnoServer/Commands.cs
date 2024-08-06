using System.Net.Sockets;
using System.Text;

namespace UnoServer;

public class Commands(RemoteServer Server)
{

    public string Ping(TcpClient client, string command)
    {
        Console.WriteLine($"Ping received from {Server.GetClientId(client)}");

        return $"Received your ping, client {Server.GetClientId(client)}";
    }

    public string RemoveClient(TcpClient client, string command)
    {
        client.Close();
        Server.RemoveClient(client);
        Console.WriteLine($"Removed: {Server.GetClientId(client)} from client list. Client Disconnected...");

        return "Server: Goodbye...";
    }
    

    public string Time(TcpClient client, string command)
    {
        
        return DateTime.Now.ToString();
    }
    
}