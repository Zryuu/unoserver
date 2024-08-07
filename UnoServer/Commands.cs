using System.Net.Sockets;
using System.Text;

namespace UnoServer;


public class Commands(RemoteServer Server)
{

    public string Ping(TcpClient client, string command)
    {
        if (!Server.GetClients().ContainsKey(client))
        {
            Console.WriteLine($"Ping received from non-current client: {client}. command: {command}");
            return 0.ToString();
        }
        
        Console.WriteLine($"Ping received from {Server.GetClientId(client)}");
        return 1.ToString();
    }

    public string StartGame(TcpClient client, string command)
    {
        return "StartGame was entered";
    }
    
    public string EndGame(TcpClient client, string command)
    {
        return "EndGame was entered";
    }
    
    public string JoinRoom(TcpClient client, string command)
    {
        return "Joined Room";
    }
    
    public string LeaveRoom(TcpClient client, string command)
    {
        return "Leave Room";
    }
    
    public string RemoveClient(TcpClient client, string command)
    {
        client.Close();
        Server.RemoveClient(client);
        Console.WriteLine($"Removed: {Server.GetClientId(client)} from client list. Client Disconnected...");

        return "Server: Goodbye...";
    }

    
}