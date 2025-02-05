using HolyShitServer.Src.Network.Packets;

namespace HolyShitServer.Src.DB.Entities;

public class ChatMessage
{
  public int Id { get; set; }
  public int UserId { get; set; }
  public string Nickname { get; set; } = string.Empty;
  public string Message { get; set; } = string.Empty;
  public ChatMessageType MessageType { get; set; }
  public DateTime CreatedAt { get; set; }

  // 외래 키 관계
  public User User { get; set; } = null!;
}