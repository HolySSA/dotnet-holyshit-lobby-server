using System.Collections.Concurrent;
using Google.Protobuf;
using HolyShitServer.Src.Core.Client;
using HolyShitServer.Src.Network.Packets;
using HolyShitServer.Src.Network.Protocol;
using Microsoft.Extensions.DependencyInjection;

namespace HolyShitServer.Src.Network.Socket;

public class MessageQueue
{
  private readonly ConcurrentQueue<GamePacketMessage> _receiveQueue = new();
  private readonly ConcurrentQueue<(PacketId, uint, IMessage)> _sendQueue = new();
  private volatile bool _processingReceive;
  private volatile bool _processingSend;
  private readonly ClientSession _session;
  private readonly MessageQueueService _messageQueueService;

  public MessageQueue(ClientSession session)
  {
    _session = session;
    _messageQueueService = session.ServiceProvider.GetRequiredService<MessageQueueService>();
  }

  public async Task EnqueueReceive(PacketId packetId, uint sequence, IMessage message)
  {
    _receiveQueue.Enqueue(new GamePacketMessage(packetId, sequence, message));
    await ProcessReceiveQueue();
  }

  public async Task EnqueueSend(PacketId packetId, uint sequence, IMessage message)
  {
    _sendQueue.Enqueue((packetId, sequence, message));
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
          await HandlerManager.HandleMessageAsync(_session, message.PacketId, message.Sequence, message.Message);
        }
        catch (Exception ex)
        {
          Console.WriteLine($"[MessageQueue] 수신 처리 오류: {_session.SessionId}, {ex.Message}");
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
          var (packetId, sequence, data) = message;
          var serializedData = PacketSerializer.Serialize(packetId, data, sequence);
          if (serializedData != null)
          {
            await _session.SendDataAsync(serializedData);
          }
        }
        catch (Exception ex)
        {
          Console.WriteLine($"[MessageQueue] 송신 처리 오류: {_session.SessionId}, {ex.Message}");
        }
      }
    }
    finally
    {
      _processingSend = false;
    }
  }
}