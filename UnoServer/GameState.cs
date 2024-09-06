
namespace UnoServer;

public struct UnoSettings
{
    public int StartingHand;
    public bool IncludeZero;
    public bool IncludeActionCards;
    public bool IncludeSpecialCards;
    public bool IncludeWildCards;

    public UnoSettings()
    {
        StartingHand = 6;
        IncludeZero = true;
        IncludeActionCards = true;
        IncludeSpecialCards = true;
        IncludeWildCards = true;
    }
}

public enum TurnType
{
    Draw = 0,
    Play
}

public class GameState
{
    public bool Active { get; set; }
    public bool Clockwise { get; set; } = true;
    public List<Client> Players;
    public Client? CurrentTurn;
    public int? CurrentTurnIndex;
    public int[] PlayersHeldCards;
    public TurnType Turntype;
    public int Turn = 0;
    public UnoSettings UnoSettings = new();

    public GameState()
    {
        SetSettings(UnoSettings.StartingHand, UnoSettings.IncludeZero, 
                    UnoSettings.IncludeActionCards, UnoSettings.IncludeSpecialCards, 
                    UnoSettings.IncludeWildCards);

    }

    public void SetSettings(int startingHand, bool Zero, bool Action, bool Special, bool Wild)
    {
        UnoSettings.StartingHand = startingHand;
        UnoSettings.IncludeZero = Zero;
        UnoSettings.IncludeActionCards = Action;
        UnoSettings.IncludeSpecialCards = Special;
        UnoSettings.IncludeWildCards = Wild;
    }

    //  Sets Game to Active, Sets Players and Chooses First Player.
    public void StartGame(List<Client> players)
    {
        //  Sets players.
        Players = players;

        Active = true;
        
        //  Reset's Turn counter.
        Turn = 0;

        //  Chooses random Player to start.
        var random = new Random();
        CurrentTurn = Players[random.Next(Players.Count)];
        CurrentTurnIndex = Players.IndexOf(CurrentTurn!);
        
        PlayersHeldCards = new int[Players.Count];

        for (var i = 0; i < PlayersHeldCards.Length; i++)
        {
            PlayersHeldCards[i] = UnoSettings.StartingHand;
        }
        
    }

    //  
    public void EndGame()
    {
        Active = false;
        CurrentTurn = null;
        Clockwise = true;
    }

    public bool CheckIfGameOver()
    {
        return PlayersHeldCards[(int)CurrentTurnIndex!] == 0;
    }
    
    public void HandleTurn(TurnType turn)
    {
        
        switch (turn)
        {
            case TurnType.Draw:
                PlayersHeldCards[(int)CurrentTurnIndex!]++;
                break;
            case TurnType.Play:

                PlayersHeldCards[(int)CurrentTurnIndex!]--;
                
                if (Clockwise)
                {
                    //  CONTINUE
                    if (CurrentTurn == Players.Last())
                    {
                        CurrentTurnIndex = Players.IndexOf(Players.First());
                        CurrentTurn = Players.First();
                    }
                    
                    CurrentTurn = Players[(int)CurrentTurnIndex++!];
                }
                else
                {
                    CurrentTurn = Players[(int)CurrentTurnIndex--!];
                }
                
                break;
        }

        Turn++;
    }
}


