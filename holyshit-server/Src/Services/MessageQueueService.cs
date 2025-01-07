using Google.Protobuf;
using HolyShitServer.Src.Core.Client;
using HolyShitServer.Src.Network.Packets;
using HolyShitServer.Src.Network.Protocol;

public class MessageQueueService
{
  private readonly ClientManager _clientManager;

  public MessageQueueService(ClientManager clientManager)
  {
    _clientManager = clientManager;
  }

  public async Task BroadcastMessage(PacketId packetId, uint sequence, IMessage message, List<int> targetUserIds)
  {
    var serializedData = PacketSerializer.Serialize(packetId, message, sequence);
    if (serializedData == null) return;

    foreach (var userId in targetUserIds)
    {
      var targetSession = _clientManager.GetSessionByUserId(userId);
      if (targetSession != null)
      {
        await targetSession.SendDataAsync(serializedData);
      }
    }
  }
}