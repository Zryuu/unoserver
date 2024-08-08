using System.Net.Sockets;

namespace UnoServer;


public class Commands(RemoteServer Server)
{

    public string Ping(Client client, string command)
    {
        if (!Server.GetClients().ContainsValue(client))
        {
            Console.WriteLine($"Ping received from non-current client: {client}. command: {command}");
            return 0.ToString();
        }
        
        Console.WriteLine($"Ping received from {client.GetXivName()}");
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
    
    public string JoinRoom(Client client, string command)
    {

        if (client.GetRoomId() != null)
        {
            //  Lease Room
        }

        if (!long.TryParse(command, out var part))
        {
            part = (long)Convert.ToInt64(command);
        }
         
        
        //  Check if Room exists 
        
        client.SetRoomId(part);

        //  Add thing to make check if Client was added to Room.
        
        var response = $"{1.ToString()}{part}";
        
        return response;
    }
    
    public string CreateRoom(Client client, string command)
    {
        var part = int.Parse(command);
        
        Room room = new Room(client,part);
                
        //  Check if duplicate ID, reroll if true.
        while (true)
        {
            if (Server.GetRoomFromId(room.GetRoomId()) == null)
            {
                room.CreateRoomId();
                continue;
            }

            break;
        }

        //  Logic to parse message to set MaxPlayers.
        
        Server.AddRoomToRooms(room);
        
        client.SetRoomId(room.GetRoomId());
        var response = $"{1.ToString()}{room.GetRoomId()}";

        return response;
    }
    
    public string LeaveRoom(Client client, string command)
    {
        var givenId = long.Parse(command);

        if (client.GetCurrentRoom() == null)
        {
            return $"{0.ToString()}";
        }
        
        if (client.GetRoomId() != givenId)
        {
            if (Server.GetRooms().ContainsKey((long)client.GetRoomId()!) == null)
            {
                client.SetCurrentRoom(null!);
            }
            
            Server.GetRoomFromId((long)client.GetRoomId()!)!.RemoveClientFromRoom(client);
            client.SetCurrentRoom(null!);
        }

        Server.GetRoomFromId(givenId)!.RemoveClientFromRoom(client);
        client.SetCurrentRoom(null!);

        return $"{1.ToString()}";
    }
    
    public string RemoveClient(Client client, string command)
    {
        client.GetClient().Close();
        Server.RemoveClient(client.GetClient());
        Console.WriteLine($"Removed: {client.GetClient()} from client list. Client Disconnected...");

        return "Server: Goodbye...";
    }

    
}