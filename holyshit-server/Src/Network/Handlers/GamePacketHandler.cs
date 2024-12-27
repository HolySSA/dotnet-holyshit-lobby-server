using HolyShitServer.Src.Models;
using HolyShitServer.Src.Network.Packets;
using HolyShitServer.Src.Network.Socket;
using HolyShitServer.Src.Services;
using HolyShitServer.Src.Services.Interfaces;
using HolyShitServer.Src.Utils.Decode;

namespace HolyShitServer.Src.Network.Handlers;

public static class GamePacketHandler
{
  private static readonly IGameService _gameService = new GameService();

  public static async Task<GamePacketMessage> HandlePositionUpdateRequest(ClientSession client, uint sequence, C2SPositionUpdateRequest request)
  {
    // 위치 업데이트 요청 처리
    var result = await _gameService.UpdatePosition(client.UserId, request.X, request.Y);

    // 성공 시에만 다른 유저들에게 알림
    if (result.Success && result.Data != null)
    {
      var currentRoom = RoomModel.Instance.GetUserRoom(client.UserId);
      if (currentRoom != null)
      {
        // 게임 내 모든 유저에게 알림
        var targetSessionIds = RoomModel.Instance.GetRoomTargetSessionIds(currentRoom.Id, 0);
        if (targetSessionIds.Any())
        {
          var notification = NotificationHelper.CreatePositionUpdateNotification(
            result.Data,  // 모든 유저의 현재 위치 정보
            targetSessionIds
          );

          await client.MessageQueue.EnqueueSend(
            notification.PacketId,
            notification.Sequence,
            notification.Message,
            notification.TargetSessionIds
          );
        }
      }
    }
    else
    {
      // 실패 시 로그만
      Console.WriteLine($"[GamePacketHandler] UpdatePosition 실패: {result.FailCode}");
    }

    // 빈 응답 반환 (클라이언트는 무시)
    return GamePacketMessage.CreateEmpty(PacketId.PositionUpdateRequest, sequence);
  }
}
