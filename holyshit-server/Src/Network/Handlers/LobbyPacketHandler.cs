using HolyShitServer.Src.Models;
using HolyShitServer.Src.Network.Packets;
using HolyShitServer.Src.Utils.Decode;

namespace HolyShitServer.Src.Network.Handlers;

public static class LobbyPacketHandler
{
  public static async Task HandleGetRoomListRequest(TcpClientHandler client, uint sequence, C2SGetRoomListRequest request)
  {
    try
    {
      var roomList = GetRoomList();
      await ResponseHelper.SendGetRoomListResponse(client, sequence, roomList);
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[Lobby] GetRoomList Request 처리 중 오류: {ex.Message}");
    }
  }

  // 임시 테스트용 방 목록 생성 메서드
  private static List<RoomData> GetRoomList()
  {
    var rooms = new List<RoomData>();
    
    // 테스트용 방 데이터 생성
    var testRoom1 = new RoomData
    {
      Id = 1,
      OwnerId = 1001,
      Name = "테스트 방 1",
      MaxUserNum = 4,
      State = RoomStateType.Wait,
      Users = { new UserData 
      {
        Id = 1001,
        Nickname = "테스트유저1",
        Character = new CharacterData
        {
          CharacterType = CharacterType.Red,
          RoleType = RoleType.Target,
          Hp = 100
        }
      }}
    };
    
    var testRoom2 = new RoomData
    {
      Id = 2,
      OwnerId = 1002,
      Name = "테스트 방 2",
      MaxUserNum = 6,
      State = RoomStateType.Wait,
      Users = { new UserData 
      {
        Id = 1002,
        Nickname = "테스트유저2",
        Character = new CharacterData
        {
          CharacterType = CharacterType.Shark,
          RoleType = RoleType.Hitman,
          Hp = 100
        }
      }}
    };
  
    rooms.Add(testRoom1);
    rooms.Add(testRoom2);

    return rooms;
  }

  public static async Task HandleCreateRoomRequest(TcpClientHandler client, uint sequence, C2SCreateRoomRequest request)
  {
    try
    {
      var userInfo = UserModel.Instance.GetAllUsers().FirstOrDefault(u => u.Client == client);
      if (userInfo == null)
      {
        Console.WriteLine("[Lobby] CreateRoom 실패: 인증되지 않은 사용자");
        await ResponseHelper.SendCreateRoomResponse(
          client, 
          sequence, 
          false, 
          null, 
          GlobalFailCode.AuthenticationFailed
        );

        return;
      }

      // 2. 이미 방에 있는지 확인
      var existingRoom = RoomModel.Instance.GetUserRoom(userInfo.UserId);
      if (existingRoom != null)
      {
        Console.WriteLine($"[Lobby] CreateRoom 실패: 이미 방에 있는 사용자 - UserId={userInfo.UserId}, RoomId={existingRoom.Id}");
        await ResponseHelper.SendCreateRoomResponse(
          client, 
          sequence, 
          false, 
          null, 
          GlobalFailCode.CreateRoomFailed
        );
        return; 
      }

      // 3. 방 생성 요청 유효성 검사
      if (string.IsNullOrEmpty(request.Name) || request.MaxUserNum < 2 || request.MaxUserNum > 8)
      {
        Console.WriteLine($"[Lobby] CreateRoom 실패: 잘못된 요청 - Name='{request.Name}', MaxUserNum={request.MaxUserNum}");
        await ResponseHelper.SendCreateRoomResponse(
          client, 
          sequence, 
          false, 
          null, 
          GlobalFailCode.InvalidRequest
        );
        return;
      }

      // 4. 방 생성
      var room = RoomModel.Instance.CreateRoom(
        request.Name,
        request.MaxUserNum,
        userInfo.UserId,
        userInfo.UserData
      );

      if (room != null)
      {
        Console.WriteLine($"[Lobby] CreateRoom 성공: RoomId={room.Id}, Name='{room.Name}', Owner={room.OwnerId}");
        await ResponseHelper.SendCreateRoomResponse(
          client,
          sequence,
          true,
          room,
          GlobalFailCode.NoneFailcode
        );
      }
      else
      {
        Console.WriteLine("[Lobby] CreateRoom 실패: 방 생성 실패");
        await ResponseHelper.SendCreateRoomResponse(
          client,
          sequence,
          false,
          null,
          GlobalFailCode.CreateRoomFailed
        );
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[Lobby] CreateRoom Request 처리 중 오류: {ex.Message}");
    }
  }
}