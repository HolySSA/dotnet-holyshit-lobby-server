using HolyShitServer.Src.Network.Packets;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HolyShitServer.Src.DB.MongoEntities;

public class ChatMessages
{
  [BsonId]
  [BsonRepresentation(BsonType.ObjectId)]
  public string Id { get; set; } = string.Empty;

  public int UserId { get; set; }
  public string Nickname { get; set; } = string.Empty;
  public string Message { get; set; } = string.Empty;
  public ChatMessageType MessageType { get; set; }
  public DateTime CreatedAt { get; set; }
}