using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace UnoServer;


public class RemoteServer
{
    private TcpListener _server;
    private bool _isRunning;
    private Commands commands;
    private Dictionary<TcpClient, string> _clients = new Dictionary<TcpClient, string>();
    private Dictionary<TcpClient, DateTime> _lastActiveTime = new Dictionary<TcpClient, DateTime>();

    public RemoteServer(int port)
    {
        _server = new TcpListener(IPAddress.Any, port);
        _server.Start();
        _isRunning = true;
        commands = new Commands(this);
        Console.WriteLine($"Server is running on port: {port}");
        LoopClients();


    }

    private void LoopClients()
    {
        while (_isRunning)
        {
            TcpClient newClient = _server.AcceptTcpClient();
            var clientThread = new Thread(HandleClient!);
            clientThread.Start(newClient);
            
            Thread monitorThread = new Thread(monitorClients);
            monitorThread.Start();
        }
    }

    private void HandleClient(object obj)
    {
        TcpClient client = (TcpClient)obj;
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];
        int bytesRead;

        bytesRead = stream.Read(buffer, 0, buffer.Length);
        string clientId = Encoding.ASCII.GetString(buffer, 0, bytesRead);

        //  Adds client to Server
        AddNewClients(client, clientId);
        
        _lastActiveTime[client] = DateTime.Now;
        Console.WriteLine($"Client connected with ID: {_clients[client]}");
        
        
        while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
        {
            string data = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            Console.WriteLine($"Received: {data} from {clientId}");
            _lastActiveTime[client] = DateTime.Now;
            string response = ExecuteCommand(data, client);
            byte[] responseBytes = Encoding.ASCII.GetBytes(response);
            stream.Write(responseBytes, 0, responseBytes.Length);
            
        }
    }

    private void monitorClients()
    {
        
        while (_isRunning)
        {
            Thread.Sleep(30000);
            DateTime now = DateTime.Now;

            List<TcpClient> inactiveClients = new List<TcpClient>();
            
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
                RemoveClient(client);
            }
        }
    }

    public void AddNewClients(TcpClient client, string clientId)
    {
        if (_clients.TryGetValue(client, out clientId!))
        {
            Console.WriteLine($"{clientId} is already a client...");
        }
        
        _clients[client] = clientId;
    }
    
    public string GetClientId(TcpClient client)
    {
        var id = _clients[client];

        return id;
    }

    public void RemoveClient(TcpClient client)
    {
        if (_clients[client] == null)
        {
            Console.WriteLine("RemoteServer::RemoveClient:: No Client could be found...");
        }

        _clients.Remove(client);
    }
    
    private string ExecuteCommand(string command, TcpClient client)
    {
        switch (command.Trim().ToLower())
        {
            case "ping":
                return commands.Ping(client);
            case "logout":
                return commands.RemoveClient(client);
            case "time":
                return commands.Time(client);
            default:
                return "Unknown command";
        }
    }
    

    
    public static void Main(string[] args)
    {
        var remoteServer = new RemoteServer(6347);
    }
}