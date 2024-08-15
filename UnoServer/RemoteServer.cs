using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace UnoServer;

//  CommandBytes received from Server
internal enum MessageTypeReceive
{
    Ping = 0,
    Login,
    Logout,
    StartGame,
    EndGame,
    CreateRoom,
    JoinRoom,
    LeaveRoom,
    UpdateRoom,
    RoomSettings
}

//  CommandBytes sent to Server
public enum MessageTypeSend
{
    Ping = 0,
    Login,
    Logout,
    StartGame,
    EndGame,
    JoinRoom,
    LeaveRoom,
    UpdateRoom,
    RoomSettings,
    Error = 99
}



public class RemoteServer
{
    private readonly TcpListener _server;
    private readonly bool _isRunning;
    private const int AfkTimer = 300;
    private Dictionary<TcpClient, Client> _clients = new Dictionary<TcpClient, Client>();
    private Dictionary<Client, DateTime> _lastActiveTime = new Dictionary<Client, DateTime>();
    private Dictionary<int, Room> _rooms = new Dictionary<int, Room>();
    public List<Client> InactiveClients = new List<Client>();

    private NetworkStream _stream;

    private RemoteServer(int port)
    {
        _server = new TcpListener(IPAddress.Any, port);
        _server.Start();
        _isRunning = true;
        Console.WriteLine($"Server is running on port: {port}");
        
        var monitorThread = new Thread(MonitorClients)
        {
            IsBackground = true
        };
        monitorThread.Start();
        
        LoopClients();
    }

    //  Creates thread to handle clients and accepts new tcp streams.
    private void LoopClients()
    {
        while (_isRunning)
        {
            var newClient = _server.AcceptTcpClient();
            var clientThread = new Thread(HandleClient!)
            {
                IsBackground = true
            };
            clientThread.Start(newClient);
        }
    }

    //  Handles sending, receiving and adding TCPClient's to clients.
    private void HandleClient(object obj)
    {
        TcpClient tcpClient = (TcpClient)obj;
        _stream = tcpClient.GetStream();
        var buffer = new byte[1024];
        int bytesRead;

        bytesRead = _stream.Read(buffer, 0, buffer.Length);
        var data = Encoding.ASCII.GetString(buffer, 0, bytesRead);
        Console.WriteLine($"Received: {data} for login attempt");

        //  Adds client to Server
        var response = ExecuteCommand(data, tcpClient);
        SendMessageToClient(_stream, response);

        if (response.StartsWith("01"))
        {
            Client client = _clients[tcpClient];
            client.SetLastActive(DateTime.Now);
            Console.WriteLine($"Client connected with ID: {_clients[tcpClient].GetXivName()}");
            
            while ((bytesRead = _stream.Read(buffer, 0, buffer.Length)) != 0)
            {
                data = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"Received: {data} from {_clients[tcpClient].GetXivName()}");
                _lastActiveTime[_clients[tcpClient]] = DateTime.Now;
                string commandResponse = ExecuteCommand(data, tcpClient);
                SendMessageToClient(_stream,commandResponse);
            
            }
        }
        else
        {
            Console.WriteLine("Invalid login attempt. Closing connection.");
            tcpClient.Close();
        }
        
    }

    //  Checks if any client in clients hasn't sent a ping in > 5mins. If so, removes them from server.
    private void MonitorClients()
    {
        while (_isRunning)
        {
            if (_clients.Count < 1)
            {
                continue;
            }
            
            //  Sleep for 5mins.
            Thread.Sleep(AfkTimer * 1000);
            var now = DateTime.Now;
            
            foreach (var client in _lastActiveTime)
            {
                if ((now - client.Value).TotalSeconds > AfkTimer)
                {
                    InactiveClients.Add(client.Key);
                }
            }

            foreach (var client in InactiveClients)
            {
                Console.WriteLine($"removing inactive client with ID: {_clients[client.GetClient()].GetXivName()}");
                RemoveClient(client.GetClient());
            }
        }
    }

    //  Adds new room to rooms list.
    public void AddRoomToRooms(Room room)
    {
        _rooms.Add(room.GetRoomId(), room);
    }

    //  Returns Clients dictionary.
    public Dictionary<TcpClient, Client> GetClients()
    {
        return _clients;
    }
    
    //  Get's Client from clients with TCPClient ref.
    public Client? GetClient(TcpClient client)
    {
        if (_clients.TryGetValue(client, out var value)) return value;
        
        Console.WriteLine($"No client exists. Requested Client {client}");
        return null;

    }

    //  Checks if client is in clients, removes client if true
    public void RemoveClient(TcpClient client)
    {
        var clientFound = false;
        foreach (var c in _clients)
        {
            if (c.Key != client) { continue; }
            
            _clients.Remove(c.Key);
            clientFound = true;
            Logout(client, "Disconnected from Server...");
            _clients.Remove(client);
        }
        
        //  This should never be ran....if this is ran then shit is fucked.
        if (!clientFound)
        {
            Console.WriteLine($"RemoteServer::RemoveClient: No Client could be found...Requested client...closing stream");
            client.Close();
        }
    }

    //  Sends message to client. Converts string to byte array, sends bytes to client.
    public void SendMessageToClient(NetworkStream stream, string message)
    {
        var commandResponseBytes = Encoding.ASCII.GetBytes(message);
        stream.Write(commandResponseBytes, 0, commandResponseBytes.Length);
    }
    
    //  Return's Rooms dictionary
    public Dictionary<int, Room> GetRooms()
    {
        return _rooms;
    }

    //  Check if Room is in Room's with a reference.
    public bool CheckRoomInRooms(Room room)
    {
        
        if (_rooms.ContainsValue(room)) return true;
        
        Console.WriteLine($"RemoteServer::GetRoomFromRef:: No room exists. Requested room: Room{room.GetRoomId()}");
        return false;

    }

    //  Get a reference to Room from Room's ID.
    public Room? GetRoomFromId(int roomId)
    {
        if (_rooms.TryGetValue(roomId, out var room)) return room;
        
        Console.WriteLine($"RemoteServer::GetRoomFromId:: No room exists. Requested room: Room{roomId}");
        return null;
    }
    
    /***************************
     *          Route          *
     *         Commands        *
     ***************************/
    
    //  Executes Commands on the server.
    private string ExecuteCommand(string message, TcpClient client)
    {
        if (string.IsNullOrEmpty(message) || message.Length < 2)
        {
            Console.WriteLine("Invalid command format.");
            return "Invalid command format.";
        }
        
        var commandByte = int.Parse(message.Substring(0, 2));
        var commandArgument = message[2..];
        
        if (!_clients.ContainsKey(client) && commandByte != 1)
        {
            Console.WriteLine("Attempted to run command without being a valid Client...");
            return ResponseType(MessageTypeSend.Error, 
                "Attempted to run command without being a valid Client...");
        }
        
        var route = (MessageTypeReceive)(commandByte);
        
        switch (route)
        {
            //  Ping = 01
            case MessageTypeReceive.Ping:
                return Ping(_clients[client], commandArgument);
            //  Login = 02,
            case MessageTypeReceive.Login:
                return Login(client, commandArgument);
            //  Logout = 03,
            case MessageTypeReceive.Logout:
                return Logout(client, commandArgument);
            //  StartGame = 04,
            case MessageTypeReceive.StartGame:
                return StartGame(client, commandArgument);
            //  EndGame = 05,
            case MessageTypeReceive.EndGame:
                return EndGame(client, commandArgument);
            //  CreateRoom = 06,
            case MessageTypeReceive.CreateRoom:
                return CreateRoom(_clients[client], commandArgument);
            //  JoinRoom = 07,
            case MessageTypeReceive.JoinRoom:
                return JoinRoom(_clients[client], commandArgument);
            //  LeaveRoom = 08
            case MessageTypeReceive.LeaveRoom:
                return LeaveRoom(_clients[client], commandArgument);
            //  LeaveRoom = 09
            case MessageTypeReceive.UpdateRoom:
                return UpdateCurrentPlayersInRoom(_clients[client], commandArgument);
            //  LeaveRoom = 10
            case MessageTypeReceive.RoomSettings:
                return RoomSettings(_clients[client], commandArgument);
            default:
                Console.WriteLine($"Unknown command. commandByte: {commandByte}. commandArgument: {commandArgument}");
                return ResponseType(MessageTypeSend.Error,
                    $"Unknown command. commandByte: {commandByte}. commandArgument: {commandArgument}");
        }
    }
    

    /***************************
     *         Commands        *
     ***************************/
    
     public string Ping(Client client, string command)
    {
        if (!GetClients().ContainsValue(client))
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
    
     //  Checks if client is in clients. If not, add new client.
    public string Login(TcpClient tcpClient, string command)
    {
        if (_clients.ContainsKey(tcpClient))
        {
            Console.WriteLine($"{command} is already a client...");
            return ResponseType(MessageTypeSend.Login, $"Already connected to server.");
        }

        var newClient = new Client(tcpClient, command, this);
        newClient.SetClient(tcpClient);
        newClient.SetXivName(command);
        newClient.SetBInGame(false);
        newClient.SetLastActive(DateTime.Now);
        _clients.Add(tcpClient, newClient);
        
        newClient.SetCurrentRoom(null);
        
        return ResponseType(MessageTypeSend.Login, $"[UNO]: Successfully connected to Server. Welcome {newClient.GetXivName()}!");
    }
    
    public string Logout(TcpClient client, string command)
    {
        if (!GetClients().ContainsKey(client))
        {
            Console.WriteLine("Failed to disconnect a clint...");
            return ResponseType(MessageTypeSend.Error, $"Failed to disconnect");    //This prob wont ever be ran.....
        }
        
        InactiveClients.Add(GetClient(client)!);
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
            var oldRoom = GetRoomFromId((int)id);

            //  Leave Room server side
            oldRoom!.RemoveClientFromRoom(client);
            
            //  Leave Room client side
            client.SetCurrentRoom(null);
            SendMessageToClient(client.GetClient().GetStream()
                ,ResponseType(MessageTypeSend.JoinRoom, $"{id}"));
        }
        
        //  Checks if given Room exists in Rooms.
        if (GetRoomFromId(part) == null)
        {
            return ResponseType(MessageTypeSend.Error, $"Room: {part}. Doesn't exist...");
        }
        
        //  Add's client to room server side.
        Console.WriteLine($"{client.GetXivName()} joined room: {part}");
        client.SetRoomId(part);

        //  Add thing to make check if Client was added to Room.

        //  Client's new room.
        var newRoom = GetRoomFromId(part);

        if (!newRoom!.CheckPlayerPresent(client))
        {
            newRoom.AddClientToRoom(client, part);
        }
        
        var playerNames = string.Join(";", newRoom.CurrentPlayers.Select(player => player.GetXivName()));
        
        //  Tells client it joined room.
        return ResponseType(MessageTypeSend.JoinRoom, $"{part}");
    }
    
    public string CreateRoom(Client client, string command)
    {
        var part = int.Parse(command);
        
        Room room = new Room(client, this, part);
        
        //  Logic to parse message to set MaxPlayers.
        
        AddRoomToRooms(room);
        
        //  Rewrite this to be an if statement. If Room.AddClientToRoom returns true, SetCurrentRoom is run.
        if (room.AddClientToRoom(client, room.GetRoomId()) < 1)
        {
            Console.WriteLine("Didnt add client to room.");
            
            
        }
        else
        {
            Console.WriteLine($"{client.GetXivName()} joined Room{room.GetRoomId()}.");
        }
        
        
        room.SetHost(client);
        
        //  This needs to be a macro
        var playerNames = string.Join(";", room.CurrentPlayers.Select(player => player.GetXivName()));
        
        return ResponseType(MessageTypeSend.JoinRoom, $"{room.GetRoomId()}");
    }
    
    //  Removes client from Room.
    public string LeaveRoom(Client client, string command)
    {
        var givenId = int.Parse(command);

        //  If Current Room is null, return
        if (client.GetCurrentRoom() == null)
        {
            return ResponseType(MessageTypeSend.Error, $"Not currently in a room.");
        }
        
        //  If current room's ID doesnt equal given ID
        if (client.GetRoomId() != givenId)
        {
            if (GetRooms().ContainsKey((int)client.GetRoomId()!) == false)
            {
                client.SetCurrentRoom(null!);
            }
            
            GetRoomFromId((int)client.GetRoomId()!)!.RemoveClientFromRoom(client);
            client.SetCurrentRoom(null!);
        }

        GetRoomFromId(givenId)!.RemoveClientFromRoom(client);
        client.SetCurrentRoom(null!);

        return ResponseType(MessageTypeSend.LeaveRoom, $"Left Room {client.GetRoomId()}");
    }
    
    public string UpdateCurrentPlayersInRoom(Client client, string command)
    {
        
        var parts = command.Split("|");
        
        var part = int.Parse(parts[0]);
        var playerNames = parts[1];
        
        //  Get Room from Rooms.
        var room = GetRoomFromId(part);

        //  Check if Room is null. If so, return with error message.
        return room == null 
            ? ResponseType(MessageTypeSend.Error,$"Room {part} doesn't exists. Aborting updating players.") 
            : ResponseType(MessageTypeSend.UpdateRoom,$"{playerNames}");
    }

    public string RoomSettings(Client client, string command)
    {
       // var


        //return ResponseType(MessageTypeSend.RoomSettings,);
        return "yes";
    }
    
    public string RemoveClient(Client client)
    {
        RemoveClient(client.GetClient());
        Console.WriteLine($"Removed: {client.GetClient()} from client list. Client Disconnected...");

        return ResponseType(MessageTypeSend.Logout, $"Goodbye {client.GetXivName()}...");
    }
    
    public string ResponseType(MessageTypeSend r, string message)
    {
        var response = $"{(int)r:D2}" + message;
        return response;
    }
    
    
    
    public static void Main(string[] args)
    {
        var remoteServer = new RemoteServer(6347);
    }
}