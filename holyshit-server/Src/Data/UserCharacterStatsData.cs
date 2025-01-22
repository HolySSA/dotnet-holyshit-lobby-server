using HolyShitServer.Src.Network.Packets;

namespace HolyShitServer.Src.Data;

public class UserCharacterStatsData
{
  public CharacterType CharacterType { get; set; }
  public int PlayCount { get; set; }
  public int WinCount { get; set; }
}