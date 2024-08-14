using System.Net.Sockets;
using System.Runtime.InteropServices.JavaScript;

namespace UnoServer;

public class Client(TcpClient client, string xivName, RemoteServer Server)
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

    public long? GetRoomId()
    {
        if (CurrentRoom == null)
        {
            return null;
        }
        
        return CurrentRoom.GetRoomId();
    }

    public void SetRoomId(int newValue)
    {
        if (CurrentRoom == null)
        {
            if (!Server.GetRooms().ContainsKey(newValue))
            {
                Console.WriteLine("SetRoomId: No valid room to set ID.");
                return;
            }

            CurrentRoom = Server.GetRoomFromId(newValue);
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
        if (room == null) return;
        
        //  If passed room isn't null, set currentRoom to room and add client to room.
        CurrentRoom = room;
        CurrentRoom!.AddClientToRoom(this, room.GetRoomId());

        //  If previousRoom is valid, remove client from room.
        previousRoom?.RemoveClientFromRoom(this);
        Console.WriteLine("SetCurrentRoom RemoveClientFromRoom func ran");

    }
    
}