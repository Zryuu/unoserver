namespace UnoServer;

public delegate void OnClientConnected(Client client);

public class Room
{
    private int RoomId { get; set; }
    private int MaxPlayers { get; set; }
    private Client Host { get; set; }
    public List<Client> CurrentPlayers { get; set; }
    public string Password { get; set; }
    private readonly RemoteServer _server;
    public event OnClientConnected OnClientConnected;
    
    public Room(Client client, RemoteServer server, int maxPlayers, string password)
    {
        _server = server;
        
        CreateRoomId();
        
        SetMaxPlayers(maxPlayers);

        SetPassword(password);
        
        CurrentPlayers = new List<Client>();
        
        Console.WriteLine($"Room{RoomId} created by {client.GetXivName()}");

        OnClientConnected += UpdateClients;
        OnClientConnected += SetHost;

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

        //  Check if duplicate ID, reroll if true.
        while (true)
        {
            if (_server.GetRoomFromId(rand) != null)
            {
                Console.WriteLine($"Room {rand} already exists. ReRolling ID");
                continue;
            }
            break;
        }
        
        RoomId = rand;
    }

    public bool CheckPlayerPresent(Client client)
    {
        return CurrentPlayers.Contains(client);
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
        
        var currentRoom = _server.CheckRoomInRooms(this);

        if (!currentRoom)
        {
            Console.WriteLine($"Room{RoomId} isn't in Rooms...How was this even run?");
            return;
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
    
    public int AddClientToRoom(Client client, int givenId)
    {
        //  Checks if the roomID given matches this room instance's ID.
        if (givenId != RoomId)
        {
            Console.WriteLine($"{client.GetXivName()} attempted to join Room{givenId} but instead tried to join Room{RoomId}.");
            return 0;
        }

        //  Checks if Room is full
        if (CurrentPlayers.Count >= MaxPlayers)
        {

            for (var i = 0; i < CurrentPlayers.Count; i++)
            {
                Console.WriteLine($"Player{i}: {CurrentPlayers[i].GetXivName()}");
            }
            
            Console.WriteLine($"Room: {client.GetXivName()} attempted to join Room{givenId} but room is full.");
            return -1;
        }
        
        //  Adds client.
        CurrentPlayers.Add(client);
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
        
        //  If Room has no members. Delete room.
        if (CurrentPlayers.Count < 1)
        {
            Console.WriteLine("CurrentPlayers < 1. Deleting room...");
            DeleteRoom();
            return true;
        }
        
        OnOnClientConnected(client);
        return true;
    }

    //  Delegate func. Called anytime a player joins or leaves room.
    public void UpdateClients(Client client)
    {
        //  Combine playerNames to one string with separator.
        var playerNames = string.Join(";", CurrentPlayers.Select(player => player.GetXivName()));

        //  Using | as the separator
        var command = RoomId + "|" + playerNames;

        //  Sends the message to all players in room.
        foreach (var player in CurrentPlayers)
        {
            _server.SendMessageToClient(player.GetClient().GetStream(),
                _server.UpdateCurrentPlayersInRoom(client, command));
        }
    }
    
    public Client GetHost()
    {
        return Host;
    }
    public void SetHost(Client client)
    {
        Host = client;
        UpdateClients(client);
    }

    public string GetPassward()
    {
        return Password;
    }

    public bool SetPassword(string newPassword)
    {
        if (newPassword.Length is > 4 or < 4)
        {
            return false;
        }

        Password = newPassword;
        return true;
    }
    
    protected virtual void OnOnClientConnected(Client client)
    {
        OnClientConnected?.Invoke(client);
    }
}