namespace HolyShitServer.Src.DB.Entities;

public class User
{
  public long Id { get; set; }
  public string Email { get; set; } = string.Empty;
  public string Nickname { get; set; } = string.Empty;
  public string PasswordHash { get; set; } = string.Empty;
  public bool IsActive { get; set; }
  public DateTime LastLoginAt { get; set; }
  public DateTime CreatedAt { get; set; }

  // Navigation property
  public ICollection<UserCharacter> Characters { get; set; } = new List<UserCharacter>();
}