using System.Net.Sockets;

namespace UnoServer;


public class Commands(RemoteServer server)
{

    public string Ping(Client client, string command)
    {
        if (!server.GetClients().ContainsValue(client))
        {
            Console.WriteLine($"Ping received from non-current client: {client}. command: {command}");
            return ResponseType(ResponseByte.Error, "Ping rejected, not a current Client. Please reconnect to the server.");
        }
        
        Console.WriteLine($"Ping received from {client.GetXivName()}");
        client.SetLastActive(DateTime.Now);
        return ResponseType(ResponseByte.Ping,$"Received Ping at {DateTime.Now}");
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
            //  Leave Room
            client.SetCurrentRoom(null);
            server.SendMessageToClient(ResponseType(ResponseByte.LeaveRoom, ""));
        }

        var part = int.Parse(command);
        
        //  Check if Room exists 

        if (server.GetRoomFromId(part) == null)
        {
            return ResponseType(ResponseByte.Error, $"Room: {part}. Doesn't exist...");
        }
        
        Console.WriteLine($"{client.GetXivName()} joined room: {part}");
        client.SetRoomId(part);
        

        //  Add thing to make check if Client was added to Room.

        return ResponseType(ResponseByte.JoinRoom, $"{part}");
    }
    
    public string CreateRoom(Client client, string command)
    {
        var part = int.Parse(command);
        
        Room room = new Room(client, server, part);
        
        //  Logic to parse message to set MaxPlayers.
        
        server.AddRoomToRooms(room);
        
        //  Rewrite this to be an if statement. If Room.AddClientToRoom returns true, SetCurrentRoom is run.
        room.AddClientToRoom(client, room.GetRoomId());
        Console.WriteLine($"{client.GetXivName()} joined Room{room.GetRoomId()}.");
        
        room.SetHost(client);
        
        return ResponseType(ResponseByte.JoinRoom, $"{room.GetRoomId()}");
    }
    
    public string LeaveRoom(Client client, string command)
    {
        var givenId = int.Parse(command);

        //  If Current Room is null, return
        if (client.GetCurrentRoom() == null)
        {
            return ResponseType(ResponseByte.LeaveRoom, $"Not currently in a room.");
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

        return ResponseType(ResponseByte.LeaveRoom, $"Left Room{client.GetRoomId()}");
    }
    
    public string RemoveClient(Client client, string command)
    {
        server.RemoveClient(client.GetClient());
        Console.WriteLine($"Removed: {client.GetClient()} from client list. Client Disconnected...");

        return ResponseType(ResponseByte.Logout, $"Goodbye {client.GetXivName()}...");
    }


    public string ResponseType(ResponseByte r, string message)
    {
        var response = $"{(int)r:D2}" + message;
        return response;
    }
    
}