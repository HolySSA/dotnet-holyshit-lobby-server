using System.Collections.Concurrent;
using Google.Protobuf;
using HolyShitServer.Src.Core;
using HolyShitServer.Src.Network;
using HolyShitServer.Src.Network.Packets;
using HolyShitServer.Src.Network.Protocol;

namespace HolyShitServer.Src.Utils.Socket;

public class MessageQueue
{
  private readonly ConcurrentQueue<GamePacketMessage> _receiveQueue = new();
  private readonly ConcurrentQueue<GamePacketMessage> _sendQueue = new();

  private volatile bool _processingReceive;
  private volatile bool _processingSend;
  private readonly TcpClientHandler _client;

  public MessageQueue(TcpClientHandler client)
  {
    _client = client;
  }

  public async Task EnqueueReceive(PacketId packetId, uint sequence, IMessage message)
  {
    _receiveQueue.Enqueue(new GamePacketMessage(packetId, sequence, message));
    await ProcessReceiveQueue();
  }

  public async Task EnqueueSend(PacketId packetId, uint sequence, IMessage message)
  {
    _sendQueue.Enqueue(new GamePacketMessage(packetId, sequence, message));
    await ProcessSendQueue();
  }

  private async Task ProcessReceiveQueue()
  {
    if (_processingReceive) return;
    _processingReceive = true;

    try
    {
      while (_receiveQueue.TryDequeue(out var message))
      {
        try
        {
          await PacketManager.ProcessMessageAsync(_client, message.PacketId, message.Sequence, message.Message);
        }
        catch (Exception ex)
        {
          Console.WriteLine($"메시지 처리 중 오류: {ex.Message}");
        } 
      }
    }
    finally
    {
      _processingReceive = false;
    }
  }

  private async Task ProcessSendQueue()
  {
    if (_processingSend) return;
    _processingSend = true;

    try
    {
      while (_sendQueue.TryDequeue(out var message))
      {
        try
        {
          var gamePacket = new GamePacket();
          // ... 패킷 타입에 따른 메시지 할당 ...
          var serializedData = PacketSerializer.Serialize(
              message.PacketId, 
              gamePacket, 
              message.Sequence);

          if (serializedData != null)
          {
            if (message.TargetUUIDs.Count > 0)
            {
              foreach (var uuid in message.TargetUUIDs)
              {
                var targetClient = ClientManager.GetClientByUUID(uuid);
                if (targetClient != null)
                {
                  await targetClient.SendDataAsync(serializedData);
                }
              }
            }
            else
            {
              await _client.SendDataAsync(serializedData);
            }
          }
        }
        catch (Exception ex)
        {
          Console.WriteLine($"메시지 전송 중 오류: {ex.Message}");
        }
      }
    }
    finally
    {
      _processingSend = false;
    }
  }
}