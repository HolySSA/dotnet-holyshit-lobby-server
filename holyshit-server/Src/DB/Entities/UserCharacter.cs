using HolyShitServer.Src.Network.Packets;

namespace HolyShitServer.Src.DB.Entities;

public class UserCharacter
{
  public long Id { get; set; }
  public long UserId { get; set; }  // FK
  public CharacterType CharacterType { get; set; }
  public int PlayCount { get; set; }
  public int WinCount { get; set; }
  public DateTime PurchasedAt { get; set; }
  public DateTime UpdatedAt { get; set; }

  // Navigation property (User 엔티티와 연결)
  public User User { get; set; } = null!;
}