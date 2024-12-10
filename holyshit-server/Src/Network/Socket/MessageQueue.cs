using System.Collections.Concurrent;
using Google.Protobuf;
using HolyShitServer.Src.Core.Client;
using HolyShitServer.Src.Network.Packets;
using HolyShitServer.Src.Network.Protocol;

namespace HolyShitServer.Src.Network.Socket;

public class MessageQueue
{
  private readonly ConcurrentQueue<GamePacketMessage> _receiveQueue = new();
  private readonly ConcurrentQueue<GamePacketMessage> _sendQueue = new();
  private volatile bool _processingReceive;
  private volatile bool _processingSend;
  private readonly ClientSession _session;

  public MessageQueue(ClientSession session)
  {
    _session = session;
  }

  public async Task EnqueueReceive(PacketId packetId, uint sequence, IMessage message)
  {
    _receiveQueue.Enqueue(new GamePacketMessage(packetId, sequence, message));
    await ProcessReceiveQueue();
  }

  public async Task EnqueueSend(PacketId packetId, uint sequence, IMessage message, List<string>? targetSessionIds = null)
  {
    _sendQueue.Enqueue(new GamePacketMessage(packetId, sequence, message, targetSessionIds));
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
          // ... 패킷 타입에 따른 메시지 할당 ...
          var serializedData = PacketSerializer.Serialize(
              message.PacketId, 
              message.Message, 
              message.Sequence);

          if (serializedData != null)
          {
            if (message.TargetSessionIds.Count > 0)
            {
              // 브로드 캐스트
              foreach (var sessionId in message.TargetSessionIds)
              {
                var targetSession = ClientManager.GetSession(sessionId);
                if (targetSession != null)
                {
                  await targetSession.SendDataAsync(serializedData);
                }
              }
            }
            else
            {
              // 단일 대상
              await _session.SendDataAsync(serializedData);
            }
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