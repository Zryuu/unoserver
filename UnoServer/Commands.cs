﻿using System.Net.Sockets;

namespace UnoServer;


public class Commands(RemoteServer server)
{

    public string Ping(Client client, string command)
    {
        if (!server.GetClients().ContainsValue(client))
        {
            Console.WriteLine($"Ping received from non-current client: {client}. command: {command}");
            return $"{99.ToString()}Ping rejected, not a current Client. Please reconnect to the server.";
        }
        
        Console.WriteLine($"Ping received from {client.GetXivName()}");
        client.SetLastActive(DateTime.Now);
        return 01.ToString();
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
        
        var response = $"{06.ToString()}{part}";
        
        return response;
    }
    
    public string CreateRoom(Client client, string command)
    {
        var part = int.Parse(command);
        
        Room room = new Room(client, server, part);
        
        
        //  Logic to parse message to set MaxPlayers.
        
        server.AddRoomToRooms(room);
        Console.WriteLine("Added Room to Rooms");
        
        //  Rewrite this to be an if statement. If Room.AddClientToRoom returns true, SetCurrentRoom is run.
        room.AddClientToRoom(client, room.GetRoomId());
        Console.WriteLine($"{client.GetXivName()} joined Room{room.GetRoomId()}.");
        
        room.SetHost(client);
        Console.WriteLine("Set Host");
        
        var response = $"{06.ToString()}{room.GetRoomId()}";

        return response;
    }
    
    public string LeaveRoom(Client client, string command)
    {
        var givenId = int.Parse(command);

        //  If Current Room is null, return
        if (client.GetCurrentRoom() == null)
        {
            return $"{07.ToString()}";
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

        return $"{07.ToString()}";
    }
    
    public string RemoveClient(Client client, string command)
    {
        client.GetClient().Close();
        server.RemoveClient(client.GetClient());
        Console.WriteLine($"Removed: {client.GetClient()} from client list. Client Disconnected...");

        return "Server: Goodbye...";
    }

    
}