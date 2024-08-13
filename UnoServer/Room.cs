namespace UnoServer;

public delegate void OnClientConnected(Client client);

public class Room
{
    private int RoomId { get; set; }
    private int MaxPlayers { get; set; }
    private Client Host { get; set; }
    public List<Client> CurrentPlayers { get; set; }
    private readonly RemoteServer _server;
    public event OnClientConnected OnClientConnected;
    
    public Room(Client client, RemoteServer server, int maxPlayers)
    {
        _server = server;
        
        CreateRoomId();
        CurrentPlayers = new List<Client> { client };
        SetHost(client);
        SetMaxPlayers(maxPlayers);
        AddClientToRoom(client, RoomId);
        
        //  Setup Delegate for UpdateCurrentPlayersInRoom() on Player Join/Leave.
        OnClientConnected += UpdateCurrentPlayersInRoom;
        
        Console.WriteLine($"Room{RoomId} created by {client.GetXivName()}");
        
        UpdateCurrentPlayersInRoom(client);
    }
    
    public int GetRoomId()
    {
        return RoomId;
    }

    public void SetRoomId(int id)
    {
        RoomId = id;
    }
    
    public void CreateRoomId()
    {
        
        var random = new Random();
        var rand = random.Next(1111, 97898);

        RoomId = rand;
    }

    private void DeleteRoom()
    {
        if (CurrentPlayers.Count > 0) return;
        
        Console.WriteLine($"Room{RoomId}'s host is {Host.GetXivName()} but room has no members. Deleting room...");

        //  If Client is an active client, RemoveHost
        if (_server.GetClients().ContainsValue(Host))
        {
            RemoveHost();
        }

        //  Checks if room is in Rooms.
        if (!_server.GetRooms().ContainsValue(this)) return;
        
        var currentRoom = _server.GetRoomfromRef(this);

        if (currentRoom == null)
        {
            Console.WriteLine($"Room{RoomId} isn't in Rooms...How was this even run?");
        }

        _server.GetRooms().Remove(RoomId);
    }
    
    public int GetMaxPlayers()
    {
        return MaxPlayers;
    }

    public void SetMaxPlayers(int newValue)
    {
        MaxPlayers = newValue;
    }

    public List<Client> GetClientsInRoom()
    {
        return CurrentPlayers;
    }
    
    //  This prob doesn't need a return statement...
    public void UpdateCurrentPlayersInRoom(Client client)
    {
        
        //  If Room has no members. Delete room.
        if (CurrentPlayers.Count < 1)
        {
            Console.WriteLine("CurrentPlayers < 1. Deleting room...");
            DeleteRoom();
            return;
        }
        
        var players = new string[CurrentPlayers.Count];
        var playerNames = "";
        
        
        //  Loop through all players, Stitch String of names.
        foreach (var player in CurrentPlayers)
        {
            //  Gonna use ; as a string separator
            playerNames += player.GetXivName() + ";";
        }
        
        //  Using CurrentPlayers, send message telling everyone Whose in the room.
        foreach (var player in CurrentPlayers)
        {
            //  msg format: commandByte, amount of players, player names[].
            _server.SendMessageToClient($"{8.ToString()}{CurrentPlayers.Count}{playerNames}");
            
            return;
        }
    }

    public bool RemoveHost()
    {
        var hostClient = _server.GetClient(Host.GetClient());

        //  Checks if Host Client's null, return.
        if (hostClient == null)
        {
            return false;
        }

        //  If Host Client's roomID matches room. Set Host's Room to null.
        if (hostClient!.GetRoomId() == RoomId)
        {
            hostClient.SetCurrentRoom(null!);
        }
        
        RemoveClientFromRoom(hostClient);
        
        return true;
    }
    
    public int AddClientToRoom(Client client, long givenId)
    {
        if (givenId != RoomId)
        {
            Console.WriteLine($"{client.GetXivName()} attempted to join Room{givenId} but instead tried to join Room{RoomId}.");
            return 0;
        }

        if (CurrentPlayers.Count >= MaxPlayers)
        {
            Console.WriteLine($"{client.GetXivName()} attempted to join Room{givenId} but room is full.");
            return -1;
        }
        
        CurrentPlayers.Add(client);
        Console.WriteLine($"{client.GetXivName()} joined Room{givenId}.");
        
        OnOnClientConnected(client);
        
        return 1;
    }

    public bool RemoveClientFromRoom(Client client)
    {
        if (!CurrentPlayers.Contains(client))
        {
            Console.WriteLine($"Client: {client.GetXivName()} isn't present in Room{RoomId}");
            return false;
        }
        
        Console.WriteLine($"Client: {client.GetXivName()} Left Room{RoomId}");
        CurrentPlayers.Remove(client);
        return true;
    }
    
    public Client GetHost()
    {
        return Host;
    }
    public void SetHost(Client client)
    {
        Host = client;
    }
    
    protected virtual void OnOnClientConnected(Client client)
    {
        OnClientConnected?.Invoke(client);
    }
}