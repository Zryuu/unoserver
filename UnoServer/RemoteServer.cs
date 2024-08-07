using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace UnoServer;

internal enum CommandRoute
{
    Ping = 0,
    LogIn = 1,
    LogOut = 2,
    StartGame = 3,
    EndGame = 4,
    JoinRoom = 5,
    LeaveRoom = 6
}

public struct ClientInfo
{
    public TcpClient Client;
    public string XivName;
    public bool BInGame;
    public int? GameSeed;
}

public class RemoteServer
{
    private readonly TcpListener _server;
    private readonly bool _isRunning;
    private readonly Commands _commands;
    private Dictionary<TcpClient, ClientInfo> _clients = new Dictionary<TcpClient, ClientInfo>();
    private Dictionary<TcpClient, DateTime> _lastActiveTime = new Dictionary<TcpClient, DateTime>();

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
        TcpClient client = (TcpClient)obj;
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];
        int bytesRead;

        bytesRead = stream.Read(buffer, 0, buffer.Length);
        string data = Encoding.ASCII.GetString(buffer, 0, bytesRead);
        Console.WriteLine($"Received: {data} for login attempt");

        //  Adds client to Server
        string response = ExecuteCommand(data, client);
        byte[] responseBytes = Encoding.ASCII.GetBytes(response);
        stream.Write(responseBytes, 0, responseBytes.Length);

        if (response.StartsWith("UNO: Successfully"))
        {
            _lastActiveTime[client] = DateTime.Now;
            Console.WriteLine($"Client connected with ID: {_clients[client]}");
            
            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
            {
                data = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"Received: {data} from {_clients[client]}");
                _lastActiveTime[client] = DateTime.Now;
                string commandResponse = ExecuteCommand(data, client);
                byte[] commandResponseBytes = Encoding.ASCII.GetBytes(commandResponse);
                stream.Write(commandResponseBytes, 0, commandResponseBytes.Length);
            
            }
        }
        else
        {
            Console.WriteLine("Invalid login attempt. Closing connection.");
            client.Close();
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
            Thread.Sleep(3000000);
            var now = DateTime.Now;

            var inactiveClients = new List<TcpClient>();
            
            foreach (var client in _lastActiveTime)
            {
                if ((now - client.Value).TotalSeconds > 30)
                {
                    inactiveClients.Add(client.Key);
                }
            }

            foreach (var client in inactiveClients)
            {
                Console.WriteLine($"removing inactive client with ID: {_clients[client]}");
                _clients.Remove(client);
                RemoveClient(client);
            }
        }
    }

    //  Checks if client is in clients. If not, add new client.
    private string AddNewClients(TcpClient client, string clientId)
    {
        if (_clients.ContainsKey(client))
        {
            Console.WriteLine($"{clientId} is already a client...");
        }

        var newInfo = new ClientInfo
        {
            Client = client,
            XivName = clientId,
            BInGame = false,
            GameSeed = null
        };

        _clients.Add(client, newInfo);
        Console.WriteLine($"Added new client: {clientId}");
        return $"UNO: Successfully connected to Server. Welcome {clientId}!";
    }
    
    //  Returns ClientId (aka XivName)
    public string GetClientId(TcpClient client)
    {
        if (!_clients.ContainsKey(client))
        {
            Console.WriteLine($"RemoteServer::GetClientId: failed, no client exists in clients. Requested client: {client}");
        }
        return _clients[client].XivName;
    }

    //  Returns Clients
    public Dictionary<TcpClient, ClientInfo> GetClients()
    {
        return _clients;
    }

    //  Checks if client is in clients, removes client if true
    public void RemoveClient(TcpClient client)
    {
        if (_clients[client].Client == null!)
        {
            Console.WriteLine($"RemoteServer::RemoveClient: No Client could be found...Requested client: {client}");
        }
        _clients.Remove(client);
    }
    
    //  Executes Commands on the server.
    private string ExecuteCommand(string message, TcpClient client)
    {
        if (string.IsNullOrEmpty(message) || message.Length < 2)
        {
            return "Invalid command format.";
        }
        
        var commandByte = (int)message[0];
        var commandArgument = message[1..];
        
        
        var route = (CommandRoute)(commandByte - '0');
        
        switch (route)
        {
            //  Ping = 0
            case CommandRoute.Ping:
                return _commands.Ping(client, commandArgument);
            //  LogIn = 1,
            case CommandRoute.LogIn:
                return AddNewClients(client, commandArgument);
            //  LogOut = 2,
            case CommandRoute.LogOut:
                return _commands.RemoveClient(client, commandArgument);
            //  StartGame = 3,
            case CommandRoute.StartGame:
                return _commands.StartGame(client, commandArgument);
            //  EndGame = 4,
            case CommandRoute.EndGame:
                return "EndGame";
            //  JoinRoom = 5,
            case CommandRoute.JoinRoom:
                return "JoinRoom";
            //  LeaveRoom = 6
            case CommandRoute.LeaveRoom:
                return "LeaveRoom";
            default:
                return "Unknown command";
                
        }
    }
    

    
    public static void Main(string[] args)
    {
        var remoteServer = new RemoteServer(6347);
    }
}