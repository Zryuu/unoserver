namespace UnoServer;

public class Room
{
    private long RoomId { get; set; }
    private int MaxPlayers { get; set; }
    private Client Host { get; set; }
    public List<Client> CurrentPlayers { get; set; }
    
    public RemoteServer Server;

    public Room(Client client, RemoteServer server, int maxPlayers)
    {
        this.Server = server;
        
        CreateRoomId();
        AddClientToRoom(client, RoomId);
        SetHost(client);
        
        Console.WriteLine($"Room{RoomId} created by {client.GetXivName()}");
        
    }
    
    public long GetRoomId()
    {
        return RoomId;
    }

    public void SetRoomId(long id)
    {
        RoomId = id;
    }
    
    public void CreateRoomId()
    {
        
        var random = new Random();
        var rand = random.NextInt64(1111, 97898);

        RoomId = rand;
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
    
    public string UpdateCurrentPlayersInRoom()
    {
        foreach (var player in CurrentPlayers)
        {
            //  Loop through all players, get name.
            //  User Server to send message with CurrentPlayers and all names but theirs.
            
            Server.
        }
        
        //  Using CurrentPlayers, send message telling everyone Whose in the room.
        //  msg format: amount of players, player names[].

        return $"{1.ToString()}";
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
    
}