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
    RoomSettings,
    GameSettings,
    UpdateHost,
    KickPlayer,
    Turn
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
    GameSettings,
    UpdateHost,
    KickPlayer,
    Turn,
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
    public List<Client> InactiveClients;

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

        try
        {
            using var _stream = tcpClient.GetStream();
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
                    if (_clients.ContainsKey(tcpClient))
                    {
                        Console.WriteLine($"Received: {data} from {_clients[tcpClient].GetXivName()}");
                        _lastActiveTime[_clients[tcpClient]] = DateTime.Now;
                        string commandResponse = ExecuteCommand(data, tcpClient);
                        SendMessageToClient(_stream,commandResponse);
                    }
                }
            }
            else
            {
                Console.WriteLine($"Invalid login attempt. Closing connection. Message: {data}");
                tcpClient.Close();
            }
        }
        catch (IOException ex) when (ex.InnerException is SocketException se && se.SocketErrorCode == SocketError.ConnectionReset)
        {
            
            Console.WriteLine("Connection reset by peer. The client may have crashed.");
        }
        catch (Exception ex)
        {
            
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
        finally
        {
            tcpClient.Close();
        }
        
       
    }

    //  Checks if any client in clients hasn't sent a ping in > 5mins. If so, removes them from server.
    private void MonitorClients()
    {
        while (_isRunning)
        {
            //  Sleep for 5mins.
            Thread.Sleep(AfkTimer * 1000);
            var now = DateTime.Now;
            
            if (_clients.Count < 1)
            {
                return;
            }
            
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
                Logout(client.GetClient(), ResponseType(MessageTypeSend.Logout, 
                    $"Disconnecting from server due to inactivity."));
                RemoveClientTcp(client.GetClient());
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
    
    //  Gets Client from clients with TCPClient ref.
    public Client? GetClient(TcpClient client)
    {
        if (_clients.TryGetValue(client, out var value)) return value;
        
        Console.WriteLine($"No client exists. Requested Client {client}");
        return null;

    }

    //  Checks if client is in clients, removes client if true
    public void RemoveClientTcp(TcpClient client)
    {
        var clientFound = false;
        foreach (var c in _clients.Where(c => c.Key == client))
        {
            clientFound = true;
            c.Key.Close();
            _clients.Remove(c.Key);
        }
        
        //  This should never be ran....if this is ran then shit is fucked.
        if (!clientFound)
        {
            Console.WriteLine($"RemoteServer::RemoveClient: No Client could be found...Requested client...closing stream");
            client.Close();
        }
    }

    //  Sends message to client. Converts string to byte array, sends bytes to client.
    public void SendMessageToClient(NetworkStream? stream, string message)
    {
        if (stream == null)
        {
            Console.WriteLine($"Stream closed, cant send message. Message: {message}");
            return;
        }
        
        var commandResponseBytes = Encoding.ASCII.GetBytes(message);
        Console.WriteLine($"Sending: {message}");
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

        var commands = message.Split("\n");

        foreach (var command in commands)
        {
            var commandByte = int.Parse(command.Substring(0, 2));
            var commandArgument = command[2..];
            
            if (!_clients.ContainsKey(client) && commandByte != 1)
            {
                Console.WriteLine("Attempted to run command without being a valid Client...");
                return ResponseType(MessageTypeSend.Error, 
                    "Attempted to run command without being a valid Client...");
            }
            
            var route = (MessageTypeReceive)(commandByte);
            
            switch (route)
            {
                //  Ping = 00
                case MessageTypeReceive.Ping:
                    return Ping(_clients[client], commandArgument);
                //  Login = 01,
                case MessageTypeReceive.Login:
                    return Login(client, commandArgument);
                //  Logout = 02,
                case MessageTypeReceive.Logout:
                    return Logout(client, commandArgument);
                //  StartGame = 03,
                case MessageTypeReceive.StartGame:
                    return StartGame(_clients[client], commandArgument);
                //  EndGame = 04,
                case MessageTypeReceive.EndGame:
                    return ForceEndGame(_clients[client], commandArgument);
                //  CreateRoom = 05,
                case MessageTypeReceive.CreateRoom:
                    return CreateRoom(_clients[client], commandArgument);
                //  JoinRoom = 06,
                case MessageTypeReceive.JoinRoom:
                    return JoinRoom(_clients[client], commandArgument);
                //  LeaveRoom = 07
                case MessageTypeReceive.LeaveRoom:
                    return LeaveRoom(_clients[client], commandArgument);
                //  LeaveRoom = 08
                case MessageTypeReceive.UpdateRoom:
                    return UpdateCurrentPlayersInRoom(_clients[client], commandArgument);
                //  RoomSettings = 09
                case MessageTypeReceive.RoomSettings:
                    return RoomSettings(_clients[client], commandArgument);
                //  GameSettings = 10
                case MessageTypeReceive.GameSettings:
                    return GameSettings(_clients[client], commandArgument);
                //  UpdateHost = 11
                case MessageTypeReceive.UpdateHost:
                    return UpdateHost(_clients[client], commandArgument);
                //  KickPlayer = 12
                case MessageTypeReceive.KickPlayer:
                   return KickPlayer(_clients[client], commandArgument);
                //  Turn = 13
                case MessageTypeReceive.Turn:
                    return HandleTurn(_clients[client], commandArgument);
                default:
                    Console.WriteLine($"Unknown command. commandByte: {commandByte}. commandArgument: {commandArgument}");
                    return ResponseType(MessageTypeSend.Error,
                        $"Unknown command. commandByte: {commandByte}. commandArgument: {commandArgument}");
            }
        }
        Console.WriteLine($"Issue executing command, message: {message}");
        return ResponseType(MessageTypeSend.Error,
            $"Issue executing command, Error: Command failed to execute correctly.");
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
            return RemoveClient(client);
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

        if (GetClient(client)!.GetCurrentRoom() == null)
        {
            return ResponseType(MessageTypeSend.Logout, $"Disconnected from Server...Goodbye...");
        }

        if (GetClient(client)!.GetCurrentRoom()!.GetHost() != GetClient(client))
        {
            GetClient(client)!.GetCurrentRoom()!.RemoveClientFromRoom(GetClient(client)!);
            return ResponseType(MessageTypeSend.Logout, $"Disconnected from Server...Goodbye...");
        }
            
        GetClient(client)!.GetCurrentRoom()!.RemoveHost();

        return ResponseType(MessageTypeSend.Logout, $"Disconnected from Server...Goodbye...");
    }

    //  Starts game for all clients.
    public string StartGame(Client client, string command)
    {
        //  Gets ref to room.
        var room = client.GetCurrentRoom();

        //  Checks if room ref is null
        if (room == null)
        {
            Console.WriteLine("Failed to start a game, Client currently not in a room...");
            return ResponseType(MessageTypeSend.Error, 
                "Failed to start a game, Client currently not in a room.");
        }

        //  Checks if Client who sent the command is actually Host.
        if (room.GetHost().GetXivName() != client.GetXivName())
        {
            Console.WriteLine("Star game called by non-host client");
            return ResponseType(MessageTypeSend.Error, 
                "Failed to start a game, Star game called by non-host client.");
        }
        
        //  Gets Ref to clients.
        var players = room.GetClientsInRoom();
        
        //  Sets GameState
        room.Game.StartGame(players);
        
        //  Sends start game to all clients.
        foreach (var player in players)
        {
            if (player == client)
            {
                continue;
            }
            
            SendMessageToClient(player.GetClient().GetStream(), 
                ResponseType(MessageTypeSend.StartGame, $"{room.Game.CurrentTurn!.GetXivName()}"));
        }
        
        return ResponseType(MessageTypeSend.StartGame, $"{room.Game.CurrentTurn!.GetXivName()}");
    }

    //  Game won by a player, Ends game for all clients. (Not called by player)
    public void EndGame(List<Client> clients, string winner)
    {
        
        var room = clients.First().GetCurrentRoom();

        if (room == null)
        {
            Console.WriteLine("Issue with ending game, room is null");
            return;
        }

        foreach (var player in clients)
        {
            SendMessageToClient(player.GetClient().GetStream(), ResponseType(MessageTypeSend.EndGame, 
                $"Game Over. Match won by {winner}. Game lasted for {room.Game.Turn} Turns"));
        }
    }
    
    //  Host canceled game, Ends Game for all clients. (Called by player)
    public string ForceEndGame(Client client, string command)
    {
        
        var room = client.GetCurrentRoom();

        if (room == null)
        {
            Console.WriteLine("EndGame command received from client not in a valid room.");
            return ResponseType(MessageTypeSend.Error, $"{client.GetXivName()} Isn't in a valid room");
        }
        
        room!.Game.EndGame();

        foreach (var player in room.GetClientsInRoom())
        {
            if (player == client)
            {
                continue;
            }
            
            SendMessageToClient(player.GetClient().GetStream(), ResponseType(MessageTypeSend.EndGame, 
                "Game was ended by host."));
        }
        
        return ResponseType(MessageTypeSend.EndGame, $"{true};Successfully ended game.");
    }
    
    //  Add Password logic
    public string JoinRoom(Client client, string command)
    {
        //  int cast the command.
        var parts = command.Split(";");
        var givenId = int.Parse(parts[0]);
        var givenPassword = parts[1];
        
        //  Get client's current room ID. If no room, returns null.
        var oldRoomId = client.GetRoomId();
        
        //  If Client's RoomID is null
        if (oldRoomId != null)
        {
            //  Client's OldRoom.
            var oldRoom = GetRoomFromId((int)oldRoomId);

            //  Leave Room server side
            oldRoom!.RemoveClientFromRoom(client);
            
            //  Leave Room client side
            client.SetCurrentRoom(null);
            SendMessageToClient(client.GetClient().GetStream()
                ,ResponseType(MessageTypeSend.JoinRoom, $"{oldRoomId}"));
        }
        
        //  Checks if given Room exists in Rooms.
        if (GetRoomFromId(givenId) == null)
        {
            return ResponseType(MessageTypeSend.Error, $"Room: {givenId}. Doesn't exist...");
        }
        
        //  Client's new room.
        var newRoom = GetRoomFromId(givenId);

        if (givenPassword == newRoom!.Password)
        {
            //  Adds client to room server side.
            Console.WriteLine($"{client.GetXivName()} joined room: {givenId}");
            client.SetRoomId(givenId);
        }
        else
        {
            return ResponseType(MessageTypeSend.Error, $"Incorrect password. Please try again.");
        }
        
        //  Add thing to make check if Client was added to Room.
        
        if (!newRoom!.CheckPlayerPresent(client))
        {
            newRoom.AddClientToRoom(client, givenId);
        }
        
        //  Tells client it joined room.
        return ResponseType(MessageTypeSend.JoinRoom, $"{givenId}");
    }
    
    public string CreateRoom(Client client, string command)
    {
        var maxPlayers = int.Parse(command[..1]);
        var password = command[1..];
        
        var room = new Room(client, this, maxPlayers, password);
        
        //  Logic to parse message to set MaxPlayers.
        
        AddRoomToRooms(room);
        
        //  Rewrite this to be an if statement. If Room.AddClientToRoom returns true, SetCurrentRoom is run.
        Console.WriteLine(room.AddClientToRoom(client, room.GetRoomId()) < 1
            ? "Didnt add client to room."
            : $"{client.GetXivName()} joined Room{room.GetRoomId()}.");
        
        client.SetCurrentRoom(room);
        room.SetHost(client);
        return ResponseType(MessageTypeSend.JoinRoom, $"{room.GetRoomId()};{room.GetHost().GetXivName()}");
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

        return "yes";
    }

    public string GameSettings(Client client, string command)
    {
        var parts = command.Split(";");
        var host = parts[0];
        var startingHand = int.Parse(parts[1]);
        var zero = bool.Parse(parts[2]);
        var action = bool.Parse(parts[3]);
        var special = bool.Parse(parts[4]);
        var wild = bool.Parse(parts[5]);
        
        //  Checks if client who sent the command is in a room.
        if (client.GetCurrentRoom() == null)
        {
            Console.WriteLine("Client isn't in room.");
            return ResponseType(MessageTypeSend.Error, $"Not Current in a Room");
        }

        //  Creates a local var of the room.
        var currentRoom = GetRoomFromId((int)client.GetRoomId()!);

        //  Checks if the Room is null.
        if (currentRoom == null)
        {
            Console.WriteLine("Room is null...weird.");
            return ResponseType(MessageTypeSend.Error, $"Room wasn't found.");
        }

        //  Checks if client who sent command is room's host.
        if (currentRoom!.GetHost().GetXivName() != host)
        {
            Console.WriteLine("Client wasn't Host");
            return ResponseType(MessageTypeSend.Error, $"Attempted to change game settings while not being room host.");
        }

        //  Checks if Room's Game is active.
        if (currentRoom.Game.Active)
        {
            Console.WriteLine("Game is currently active");
            return ResponseType(MessageTypeSend.Error, $"Attempted to change game settings while not being room host.");
        }

        currentRoom.Game.SetSettings(startingHand, zero, special, action, wild);

        var newSettings = currentRoom.Game.UnoSettings;

        foreach (var player in currentRoom.CurrentPlayers)
        {
            SendMessageToClient(player.GetClient().GetStream(), ResponseType(MessageTypeSend.GameSettings, 
                $"{newSettings.StartingHand};{newSettings.IncludeZero};{newSettings.IncludeActionCards};{newSettings.IncludeSpecialCards};{newSettings.IncludeWildCards}"));
        }
        
        return ResponseType(MessageTypeSend.GameSettings, $"Game Settings updated.");
    }

    public string UpdateHost(Client client, string command)
    {
        if (client.GetCurrentRoom() == null)
        {
            return ResponseType(MessageTypeSend.Error, $"Can't update host (Not currently in a room).");
        }

        var room = client.GetCurrentRoom();

        if (room!.GetHost() != client)
        {
            return ResponseType(MessageTypeSend.Error, $"Can't update host (Not current host of room {client.GetCurrentRoom()!.GetRoomId()}).");
        }

        if (room!.GetHost().GetXivName() == command)
        {
            return ResponseType(MessageTypeSend.Error, $"Can't update host (Selected player is already host).");
        }

        var players = room.GetClientsInRoom();

        foreach (var player in players)
        {
            if (player.GetXivName() != command) continue;
            
            room.SetHost(player);
            room.UpdateClients(player);
            return ResponseType(MessageTypeSend.UpdateHost, $"Successfully updated Host");
        }

        return ResponseType(MessageTypeSend.Error, $"Couldn't find {command} in room {room.GetRoomId()}");
    }
    
    public string KickPlayer(Client client, string command)
    {
        if (client.GetCurrentRoom() == null)
        {
            Console.WriteLine($"{client.GetXivName()} isn't in a room.");
            return ResponseType(MessageTypeSend.Error, "Unable to Kick Player (Not Currently in a room).");
        }
        
        var room = GetRoomFromId((int)client.GetRoomId()!);

        if (client.GetXivName() != room!.GetHost().GetXivName())
        {
            Console.WriteLine($"{client.GetXivName()} isn't host of room {room.GetRoomId()}.");
            return ResponseType(MessageTypeSend.Error, "Unable to Kick Player (Not host of room).");
        }
        
        foreach (var player in room!.CurrentPlayers)
        {
            if (player.GetXivName() != command) continue;
            
            SendMessageToClient(player.GetClient().GetStream(), ResponseType(MessageTypeSend.LeaveRoom, $"{room.GetRoomId()}"));
            return ResponseType(MessageTypeSend.KickPlayer, $"Successfully kicked {player.GetXivName()}");
        }

        return ResponseType(MessageTypeSend.Error, $"Unable to Kick player: {command}, Player not found in room.");
    }

    public string HandleTurn(Client client, string command)
    {
        //  Parse command
        var parts = command.Split(";");
        var turnType = parts[0];
        var type = int.Parse(parts[1]);
        var color = int.Parse(parts[2]);
        var number = int.Parse(parts[3]);
        
        var room = client.GetCurrentRoom();

        //  If client isn't in a room
        if (room == null)
        {
            Console.WriteLine($"{client.GetXivName()} sent a Turn command but isn't in a room.");
            return ResponseType(MessageTypeSend.Error, $"Can't handle Turn (Not currently in a room).");
        }

        //  If Room's game isn't active
        if (!room.Game.Active)
        {
            Console.WriteLine($"{client.GetXivName()} sent a Turn command but game isn't currently active.");
            return ResponseType(MessageTypeSend.Error, $"Can't handle Turn (Not currently in a live game).");
        }
        
        //  If It's not Client's turn (Should never be called....but just in case.)
        if (client != room.Game.CurrentTurn)
        {
            return ResponseType(MessageTypeSend.Error, $"Can't handle Turn (Not currently your turn).");
        }
        
        switch (turnType)
        {
            case "Draw":
                room.Game.HandleTurn((TurnType)0);

                foreach (var player in room.GetClientsInRoom())
                {
                    if (player == client)
                    {
                        continue;
                    }
                    
                    SendMessageToClient(player.GetClient().GetStream(), 
                        ResponseType(MessageTypeSend.Turn, 
                            $"Draw;{type};{color};{number};{room.Game.CurrentTurn.GetXivName()}"));
                }
                
                return ResponseType(MessageTypeSend.Turn, 
                    $"Draw;{type};{color};{number};{room.Game.CurrentTurn.GetXivName()}");
            
            case "Play":
                room.Game.HandleTurn((TurnType)1);
                
                foreach (var player in room.GetClientsInRoom())
                {
                    if (player == client)
                    {
                        continue;
                    }

                    SendMessageToClient(player.GetClient().GetStream(),
                        ResponseType(MessageTypeSend.Turn,
                            $"Play;{type};{color};{number};{room.Game.CurrentTurn.GetXivName()}"));
                }

                //  This will prob cause problems.....we'll see
                if (room.Game.CheckIfGameOver())
                {
                    EndGame(room.GetClientsInRoom(), client.GetXivName());
                }
                
                return ResponseType(MessageTypeSend.Turn, 
                    $"Play;{type};{color};{number};{room.Game.CurrentTurn.GetXivName()}");
            
            default:
                Console.WriteLine($"Invalid Turn Command: {command} sent by {client.GetXivName()}");
                return ResponseType(MessageTypeSend.Error, 
                    $"Invalid Turn Command");
        }
    }
    
    public string RemoveClient(Client client)
    {
        RemoveClientTcp(client.GetClient());
        Console.WriteLine($"Removed: {client.GetClient()} from client list. Client Disconnected...");

        return ResponseType(MessageTypeSend.Logout, $"Goodbye {client.GetXivName()}...");
    }
    
    public static string ResponseType(MessageTypeSend r, string message)
    {
        var response = $"{(int)r:D2}" + message + "\n";
        return response;
    }
    
    
    
    public static void Main(string[] args)
    {
        var remoteServer = new RemoteServer(6347);
    }
}