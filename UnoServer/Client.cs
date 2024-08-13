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
        if (CurrentRoom != null)
        {
            CurrentRoom.RemoveClientFromRoom(this);
        }

        //  If Room is null, return.
        if (room == null) return;
        
        //  If Room isn't, set currentroom to room and add client to room, via room.
        CurrentRoom = room;
        CurrentRoom!.AddClientToRoom(this, room.GetRoomId());

    }
    
}