namespace PlayerTradingReforged;

internal static class ZNetUtils
{
    public static Player? GetPlayer(long peer)
    {
        foreach (Player player in Player.GetAllPlayers())
            if (player && player.GetOwner() == peer) return player;
        return null;
    }

    public static Player? GetCharacter(long character)
    {
        foreach (Player player in Player.GetAllPlayers())
            if (player && player.GetPlayerID() == character) return player;
        return null;
    }
}
