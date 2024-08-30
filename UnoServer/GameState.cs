
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

public class GameState
{
    public bool Active { get; set; } = false;

    public UnoSettings UnoSettings = new();

    public void SetSettings(int startingHand, bool Zero, bool Action, bool Special, bool Wild)
    {
        UnoSettings.StartingHand = startingHand;
        UnoSettings.IncludeZero = Zero;
        UnoSettings.IncludeActionCards = Action;
        UnoSettings.IncludeSpecialCards = Special;
        UnoSettings.IncludeWildCards = Wild;
    }
    
}


