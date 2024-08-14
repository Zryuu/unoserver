using System.Net.Sockets;

namespace UnoServer;


public class Commands(RemoteServer server)
{

    public string Ping(Client client, string command)
    {
        if (!server.GetClients().ContainsValue(client))
        {
            Console.WriteLine($"Ping received from non-current client: {client}. command: {command}");
            return ResponseType(MessageTypeSend.Error, "Ping rejected, not a current Client. Please reconnect to the server.");
        }

        if (command != client.GetXivName())
        {
            Console.WriteLine($"Ping received from {client.GetXivName()} but name sent was {command}...Removing");
            RemoveClient(client);
        }
        
        Console.WriteLine($"Ping received from {client.GetXivName()}");
        client.SetLastActive(DateTime.Now);
        return ResponseType(MessageTypeSend.Ping,$"Received Ping at {DateTime.Now}");
    }
    
    public string Login(TcpClient client, string command)
    {
        return "StartGame was entered";
    }
    
    public string Logout(TcpClient client, string command)
    {
        if (!server.GetClients().ContainsKey(client))
        {
            Console.WriteLine("Failed to disconnect a clint...");
            return ResponseType(MessageTypeSend.Error, $"Failed to disconnect");    //This prob wont ever be ran.....
        }
        
        server.InactiveClients.Add(server.GetClient(client)!);
        return ResponseType(MessageTypeSend.Logout, $"Disconnected from Server...Goodbye...");
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
        //  int cast the command.
        var part = int.Parse(command);
        
        //  Get client's current room ID. If no room, returns null.
        var id = client.GetRoomId();
        
        //  If Client's RoomID is null
        if (id != null)
        {
            //  Client's OldRoom.
            var oldRoom = server.GetRoomFromId((int)id);

            //  Leave Room server side
            oldRoom!.RemoveClientFromRoom(client);
            
            //  Leave Room client side
            client.SetCurrentRoom(null);
            server.SendMessageToClient(ResponseType(MessageTypeSend.LeaveRoom, $"{id}"));
        }
        
        //  Checks if given Room exists in Rooms.
        if (server.GetRoomFromId(part) == null)
        {
            return ResponseType(MessageTypeSend.Error, $"Room: {part}. Doesn't exist...");
        }
        

        
        //  Add's client to room server side.
        Console.WriteLine($"{client.GetXivName()} joined room: {part}");
        client.SetRoomId(part);

        //  Add thing to make check if Client was added to Room.

        //  Client's new room.
        var newRoom = server.GetRoomFromId(part);

        if (!newRoom!.CheckPlayerPresent(client))
        {
            newRoom.AddClientToRoom(client, part);
        }
        
        //  Tells client it joined room.
        return ResponseType(MessageTypeSend.JoinRoom, $"{part}");
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
        
        return ResponseType(MessageTypeSend.JoinRoom, $"{room.GetRoomId()}");
    }
    
    //  Removes client from Room.
    public string LeaveRoom(Client client, string command)
    {
        var givenId = int.Parse(command);

        //  If Current Room is null, return
        if (client.GetCurrentRoom() == null)
        {
            return ResponseType(MessageTypeSend.LeaveRoom, $"Not currently in a room.");
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

        return ResponseType(MessageTypeSend.LeaveRoom, $"Left Room{client.GetRoomId()}");
    }
    

    public string UpdateCurrentPlayersInRoom(Client client, string command)
    {
        
        var part = int.Parse(command);

        //  Get Room from Rooms.
        var room = server.GetRoomFromId(part);

        //  Check if Room is null. If so, return with error message.
        if (room == null)
        {
            return ResponseType(MessageTypeSend.Error, $"Room {part} doesn't exists. Aborting updating players.");
        }
        
        //  Combine playerNames to one string with separator.
        var playerNames = string.Join(";", room.CurrentPlayers.Select(player => player.GetXivName()));

        return ResponseType(MessageTypeSend.UpdateRoom,$"{MessageTypeSend.UpdateRoom}{playerNames}");
    }
    
    public string RemoveClient(Client client)
    {
        server.RemoveClient(client.GetClient());
        Console.WriteLine($"Removed: {client.GetClient()} from client list. Client Disconnected...");

        return ResponseType(MessageTypeSend.Logout, $"Goodbye {client.GetXivName()}...");
    }


    public string ResponseType(MessageTypeSend r, string message)
    {
        var response = $"{(int)r:D2}" + message;
        return response;
    }
    
}