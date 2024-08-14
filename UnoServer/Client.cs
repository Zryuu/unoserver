using System.Net.Sockets;
using System.Runtime.InteropServices.JavaScript;

namespace UnoServer;

public class Client(TcpClient client, string xivName, RemoteServer server)
{
    private TcpClient TcpClient { get; set; } = client;
    private string XivName { get; set; } = xivName;
    private bool BInUnoGame { get; set; } = false;
    private DateTime LastActive { get; set; }
    private Room? CurrentRoom { get; set; }
    
    
    public void SetClient(TcpClient client)
    {
        TcpClient = client;
    }
    
    public TcpClient GetClient()
    {
        return TcpClient;
    }

    public void SetClient(Client passedClient)
    {
        TcpClient = passedClient.TcpClient;
    }

    public string GetXivName()
    {
        return XivName;
    }
    
    public void SetXivName(string name)
    {
        XivName = name;
    }

    public bool GetBInGame(Client passedClient)
    {
        return passedClient.BInUnoGame;
    }
    
    public void SetBInGame(bool newValue)
    {
        BInUnoGame = newValue;
    }

    public int? GetRoomId()
    {
        return CurrentRoom?.GetRoomId();
    }

    public void SetRoomId(int newValue)
    {
        if (CurrentRoom == null)
        {
            if (!server.GetRooms().ContainsKey(newValue))
            {
                Console.WriteLine("SetRoomId: No valid room to set ID.");
                return;
            }

            CurrentRoom = server.GetRoomFromId(newValue);
            Console.WriteLine($"Set currentRoom's ID");
        }
        
        CurrentRoom!.SetRoomId(newValue);
    }

    public DateTime GetLastActive()
    {
        return LastActive;
    }
    
    public void SetLastActive(DateTime newValue)
    {
        LastActive = newValue;
    }

    public Room? GetCurrentRoom()
    {
        return CurrentRoom;
    }

    public void SetCurrentRoom(Room? room)
    {
        
        //  Saves copy of currentRoom.
        var previousRoom = CurrentRoom;
        
        //  If passed room is null, return.
        if (room == null)
        {
            return;
        }
        
        //  If passed room isn't null, set currentRoom to room and add client to room.
        CurrentRoom = room;
        CurrentRoom!.AddClientToRoom(this, room.GetRoomId());

        //  If previousRoom is valid, remove client from room.
        previousRoom?.RemoveClientFromRoom(this);

        Console.WriteLine(previousRoom != null
            ? $"{xivName} left room {previousRoom.GetRoomId()} and joined room {CurrentRoom.GetRoomId()}"
            : $"{xivName} joined room {CurrentRoom.GetRoomId()}");
    }
    
}