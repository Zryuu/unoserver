using System.Net.Sockets;
using System.Runtime.InteropServices.JavaScript;

namespace UnoServer;

public class Client(TcpClient client, string XIVName, RemoteServer Server)
{
    private TcpClient client { get; set; }
    private string XivName { get; set; }
    private bool BInGame { get; set; }
    private DateTime LastActive { get; set; }
    
    private Room? CurrentRoom { get; set; }
    
    
    public TcpClient GetClient()
    {
        return client;
    }

    public void SetClient(Client passedClient)
    {
        this.client = passedClient.client;
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
        return passedClient.BInGame;
    }
    
    public void SetBInGame(bool newValue)
    {
        BInGame = newValue;
    }

    public long? GetRoomId()
    {
        if (CurrentRoom == null)
        {
            return null;
        }
        
        return CurrentRoom.GetRoomId();
    }
    
    public void SetRoomId(long newValue)
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

    public void SetCurrentRoom(Room room)
    {
        CurrentRoom?.RemoveClientFromRoom(this);
        
        CurrentRoom = room;
        CurrentRoom.AddClientToRoom(this, room.GetRoomId());
    }
    
}