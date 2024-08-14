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
    UpdateRoom
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
    Error = 99
}



public class RemoteServer
{
    private readonly TcpListener _server;
    private readonly bool _isRunning;
    private readonly Commands _commands;
    private const int AfkTimer = 30;
    private Dictionary<TcpClient, Client> _clients = new Dictionary<TcpClient, Client>();
    private Dictionary<Client, DateTime> _lastActiveTime = new Dictionary<Client, DateTime>();
    private Dictionary<int, Room> _rooms = new Dictionary<int, Room>();
    public List<Client> InactiveClients = new List<Client>();

    public NetworkStream Stream;

    private RemoteServer(int port)
    {
        _server = new TcpListener(IPAddress.Any, port);
        _server.Start();
        _isRunning = true;
        _commands = new Commands(this);
        Console.WriteLine($"Server is running on port: {port}");
        
        var monitorThread = new Thread(MonitorClients)
        {
            IsBackground = true
        };
        monitorThread.Start();
        
        LoopClients();
    }

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

    private void HandleClient(object obj)
    {
        TcpClient tcpClient = (TcpClient)obj;
        Stream = tcpClient.GetStream();
        var buffer = new byte[1024];
        int bytesRead;

        bytesRead = Stream.Read(buffer, 0, buffer.Length);
        var data = Encoding.ASCII.GetString(buffer, 0, bytesRead);
        Console.WriteLine($"Received: {data} for login attempt");

        //  Adds client to Server
        var response = ExecuteCommand(data, tcpClient);
        SendMessageToClient(response);

        if (response.StartsWith("01"))
        {
            Client client = _clients[tcpClient];
            client.SetLastActive(DateTime.Now);
            Console.WriteLine($"Client connected with ID: {_clients[tcpClient].GetXivName()}");
            
            while ((bytesRead = Stream.Read(buffer, 0, buffer.Length)) != 0)
            {
                data = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"Received: {data} from {_clients[tcpClient].GetXivName()}");
                _lastActiveTime[_clients[tcpClient]] = DateTime.Now;
                string commandResponse = ExecuteCommand(data, tcpClient);
                SendMessageToClient(commandResponse);
            
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

    //  Checks if client is in clients. If not, add new client.
    private string AddNewClients(TcpClient client, string clientId)
    {
        if (_clients.ContainsKey(client))
        {
            Console.WriteLine($"{clientId} is already a client...");
            return _commands.ResponseType(MessageTypeSend.Login, $"Already connected to server.");
        }

        var newClient = new Client(client, clientId, this);
            newClient.SetClient(client);
            newClient.SetXivName(clientId);
            newClient.SetBInGame(false);
            newClient.SetLastActive(DateTime.Now);
        _clients.Add(client, newClient);
        
        newClient.SetCurrentRoom(null);
        
        return _commands.ResponseType(MessageTypeSend.Login, $"[UNO]: Successfully connected to Server. Welcome {newClient.GetXivName()}!");
    }

    public void AddRoomToRooms(Room room)
    {
        _rooms.Add(room.GetRoomId(), room);
    }

    //  Returns Clients
    public Dictionary<TcpClient, Client> GetClients()
    {
        return _clients;
    }
    
    public Client? GetClient(TcpClient client)
    {
        if (!_clients.ContainsKey(client))
        {
            Console.WriteLine($"No client exists. Requested Client {client}");
            return null;
        }

        return _clients[client];
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
            _commands.Logout(client, "Disconnected from Server...");
            _clients.Remove(client);
        }
        
        //  This should never be ran....if this is ran then shit is fucked.
        if (!clientFound)
        {
            Console.WriteLine($"RemoteServer::RemoveClient: No Client could be found...Requested client...closing stream");
            client.Close();
        }
    }

    public void SendMessageToClient(string message)
    {
        byte[] commandResponseBytes = Encoding.ASCII.GetBytes(message);
        Stream.Write(commandResponseBytes, 0, commandResponseBytes.Length);
    }
    
    public Dictionary<int, Room> GetRooms()
    {
        return _rooms;
    }

    public Room? GetRoomFromRef(Room room)
    {
        var id = room.GetRoomId();
        if (!_rooms.TryGetValue(id, out var foundRoom))
        {
            Console.WriteLine($"RemoteServer::GetRoomFromRef:: No room exists. Requested room: Room{id}");
            return null;
        }

        return _rooms[id];
        
    }

    public Room? GetRoomFromId(int roomId)
    {
        
        if (!_rooms.TryGetValue(roomId, out var room))
        {
            Console.WriteLine($"RemoteServer::GetRoomFromId:: No room exists. Requested room: Room{roomId}");
            return null;
        }

        return _rooms[roomId];
    }
    
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
            return _commands.ResponseType(MessageTypeSend.Error, "Attempted to run command without being a valid Client...");
        }
        
        var route = (MessageTypeReceive)(commandByte);
        
        switch (route)
        {
            //  Ping = 01
            case MessageTypeReceive.Ping:
                return _commands.Ping(_clients[client], commandArgument);
            //  Login = 02,
            case MessageTypeReceive.Login:
                return AddNewClients(client, commandArgument);
            //  Logout = 03,
            case MessageTypeReceive.Logout:
                return _commands.Logout(client, commandArgument);
            //  StartGame = 04,
            case MessageTypeReceive.StartGame:
                return _commands.StartGame(client, commandArgument);
            //  EndGame = 05,
            case MessageTypeReceive.EndGame:
                return "EndGame";
            //  CreateRoom = 06,
            case MessageTypeReceive.CreateRoom:
                return _commands.CreateRoom(_clients[client], commandArgument);
            //  JoinRoom = 07,
            case MessageTypeReceive.JoinRoom:
                return _commands.JoinRoom(_clients[client], commandArgument);
            //  LeaveRoom = 08
            case MessageTypeReceive.LeaveRoom:
                return "LeaveRoom";
            //case MessageTypeReceive.UpdateRoom:
                //return _rooms.UpdateCurrentPlayersInRoom();
            default:
                Console.WriteLine("Unknown command");
                Console.WriteLine($"commandByte: {commandByte}");
                Console.WriteLine($"commandArgument: {commandArgument}");
                return _commands.ResponseType(MessageTypeSend.Error, "Unknown command");
                
        }
    }
    

    
    public static void Main(string[] args)
    {
        var remoteServer = new RemoteServer(6347);
    }
}