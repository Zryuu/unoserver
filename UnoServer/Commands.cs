using System.Net.Sockets;

namespace UnoServer;


public class Commands(RemoteServer server)
{

    public string Ping(Client client, string command)
    {
        if (!server.GetClients().ContainsValue(client))
        {
            Console.WriteLine($"Ping received from non-current client: {client}. command: {command}");
            return 0.ToString();
        }
        
        Console.WriteLine($"Ping received from {client.GetXivName()}");
        client.SetLastActive(DateTime.Now);
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

        if (!int.TryParse(command, out var part))
        {
            part = Convert.ToInt32(command);
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
        
        Room room = new Room(client, server, part);
                
        //  Check if duplicate ID, reroll if true.
        while (true)
        {
            if (server.GetRoomFromId(room.GetRoomId()) != null)
            {
                room.CreateRoomId();
                continue;
            }

            break;
        }

        //  Logic to parse message to set MaxPlayers.
        
        server.AddRoomToRooms(room);
        Console.WriteLine("Added Room to Rooms");
        client.SetCurrentRoom(room);
        Console.WriteLine("Set Current Room");
        client.SetRoomId(room.GetRoomId());
        Console.WriteLine("Set Room Id");
        room.SetMaxPlayers(part);
        Console.WriteLine("Set Max Players");
        
        var response = $"{1.ToString()}{room.GetRoomId()}";

        return response;
    }
    
    public string LeaveRoom(Client client, string command)
    {
        var givenId = int.Parse(command);

        //  If Current Room is null, return
        if (client.GetCurrentRoom() == null)
        {
            return $"{0.ToString()}";
        }
        
        //  If currentroom's ID doesnt equal given ID
        if (client.GetRoomId() != givenId)
        {
            if (server.GetRooms().ContainsKey((int)client.GetRoomId()!) == false)
            {
                client.SetCurrentRoom(null!);
            }
            
            server.GetRoomFromId((int)client.GetRoomId()!)!.RemoveClientFromRoom(client);
            client.SetCurrentRoom(null!);
        }

        server.GetRoomFromId(givenId)!.RemoveClientFromRoom(client);
        client.SetCurrentRoom(null!);

        return $"{1.ToString()}";
    }
    
    public string RemoveClient(Client client, string command)
    {
        client.GetClient().Close();
        server.RemoveClient(client.GetClient());
        Console.WriteLine($"Removed: {client.GetClient()} from client list. Client Disconnected...");

        return "Server: Goodbye...";
    }

    
}