using HolyShitServer.DB.Contexts;
using HolyShitServer.Src.Data;
using HolyShitServer.Src.Models;
using HolyShitServer.Src.Network.Packets;
using HolyShitServer.Src.Services.Interfaces;
using HolyShitServer.Src.Services.LoadBalancing;
using HolyShitServer.Src.Services.Results;
using Microsoft.Extensions.DependencyInjection;

namespace HolyShitServer.Src.Services;

public class RoomService : IRoomService
{
  private readonly RoomModel _roomModel;
  private readonly IServiceProvider _serviceProvider;
  private readonly GameDataManager _gameDataManager;

  public RoomService(IServiceProvider serviceProvider)
  {
    _roomModel = RoomModel.Instance;
    _serviceProvider = serviceProvider;
    _gameDataManager = serviceProvider.GetRequiredService<GameDataManager>();
  }

  /// <summary>
  /// 현재 존재하는 모든 방 목록을 반환
  /// </summary>
  public async Task<ServiceResult<List<RoomData>>> GetRoomList()
  {
    try
    {
      return await Task.Run(() =>
      {
        // 방 목록 조회
        var roomList = _roomModel.GetRoomList();
        return ServiceResult<List<RoomData>>.Ok(roomList ?? new List<RoomData>());
      });
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[RoomService] GetRoomList 실패: {ex.Message}");
      return ServiceResult<List<RoomData>>.Error(GlobalFailCode.UnknownError);
    }
  }

  /// <summary>
  /// 방 생성
  /// </summary>
  public async Task<ServiceResult<RoomData>> CreateRoom(int userId, string name, int maxUserNum)
  {
    try
    {
      // 입력값 검증
      if (string.IsNullOrEmpty(name) || maxUserNum < 2 || maxUserNum > 8)
        return ServiceResult<RoomData>.Error(GlobalFailCode.InvalidRequest);

      using var scope = _serviceProvider.CreateScope();
      var redisService = scope.ServiceProvider.GetRequiredService<RedisService>();
      var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

      // Redis에서 유저 정보 조회
      var userCharacterData = await redisService.GetUserCharacterTypeAsync(userId, dbContext);
      if (userCharacterData == null)
        return ServiceResult<RoomData>.Error(GlobalFailCode.AuthenticationFailed);

      // 이미 방에 있는지 체크
      var existingRoom = _roomModel.GetUserRoom(userId);
      if (existingRoom != null)
        return ServiceResult<RoomData>.Error(GlobalFailCode.CreateRoomFailed);

      // 로비용 기본 UserData 생성
      var userData = new UserData
      {
        Id = userId,
        Nickname = userCharacterData.Nickname,
        Character = new CharacterData
        {
          CharacterType = userCharacterData.LastSelectedCharacter,
          RoleType = RoleType.NoneRole,
          Hp = 0,
          Weapon = 0,
          StateInfo = new CharacterStateInfoData
          {
            State = CharacterStateType.NoneCharacterState,
            NextState = CharacterStateType.NoneCharacterState,
            NextStateAt = 0,
            StateTargetUserId = 0
          },
          BbangCount = 0,
          HandCardsCount = 0
        }
      };

      // 방 생성
      var room = _roomModel.CreateRoom(name, maxUserNum, userId, userData);
      if (room == null)
        return ServiceResult<RoomData>.Error(GlobalFailCode.CreateRoomFailed);

      return ServiceResult<RoomData>.Ok(room.ToProto());
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[RoomService] CreateRoom 실패: {ex.Message}");
      return ServiceResult<RoomData>.Error(GlobalFailCode.UnknownError);
    }
  }

  /// <summary>
  /// 방 입장
  /// </summary>
  public async Task<ServiceResult<RoomData>> JoinRoom(int userId, int roomId)
  {
    try
    {
      using var scope = _serviceProvider.CreateScope();
      var redisService = scope.ServiceProvider.GetRequiredService<RedisService>();
      var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

      // 유저 정보 조회
      var userCharacterData = await redisService.GetUserCharacterTypeAsync(userId, dbContext);
      if (userCharacterData == null)
        return ServiceResult<RoomData>.Error(GlobalFailCode.AuthenticationFailed);

      // 이미 방에 있는지 확인
      var existingRoom = _roomModel.GetUserRoom(userId);
      if (existingRoom != null)
        return ServiceResult<RoomData>.Error(GlobalFailCode.JoinRoomFailed);

      // 요청한 방이 존재하는지 확인
      var targetRoom = _roomModel.GetRoom(roomId);
      if (targetRoom == null)
        return ServiceResult<RoomData>.Error(GlobalFailCode.RoomNotFound);

      // 인원 수 체크
      var currentUsers = targetRoom.GetAllUsers();
      if (currentUsers.Count >= targetRoom.MaxUserNum)
        return ServiceResult<RoomData>.Error(GlobalFailCode.JoinRoomFailed);

      // 상태 체크
      if (targetRoom.State != RoomStateType.Wait)
        return ServiceResult<RoomData>.Error(GlobalFailCode.InvalidRoomState);

      // 로비용 기본 UserData 생성
      var userData = new UserData
      {
        Id = userId,
        Nickname = userCharacterData.Nickname,
        Character = new CharacterData
        {
          CharacterType = userCharacterData.LastSelectedCharacter,
          RoleType = RoleType.NoneRole,
          Hp = 0,
          Weapon = 0,
          StateInfo = new CharacterStateInfoData
          {
            State = CharacterStateType.NoneCharacterState,
            NextState = CharacterStateType.NoneCharacterState,
            NextStateAt = 0,
            StateTargetUserId = 0
          },
          BbangCount = 0,
          HandCardsCount = 0
        }
      };

      // 방 입장 처리
      if (!_roomModel.JoinRoom(roomId, userData))
        return ServiceResult<RoomData>.Error(GlobalFailCode.JoinRoomFailed);

      var updatedRoom = _roomModel.GetRoom(roomId);
      if (updatedRoom == null)
        return ServiceResult<RoomData>.Error(GlobalFailCode.RoomNotFound);

      return ServiceResult<RoomData>.Ok(updatedRoom.ToProto());
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[RoomService] JoinRoom 실패: {ex.Message}");
      return ServiceResult<RoomData>.Error(GlobalFailCode.UnknownError);
    }
  }

  /// <summary>
  /// 랜덤 방 입장
  /// </summary>
  public async Task<ServiceResult<RoomData>> JoinRandomRoom(int userId)
  {
    try
    {
      using var scope = _serviceProvider.CreateScope();
      var redisService = scope.ServiceProvider.GetRequiredService<RedisService>();
      var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

      // 유저 검증
      var userCharacterData = await redisService.GetUserCharacterTypeAsync(userId, dbContext);
      if (userCharacterData == null)
        return ServiceResult<RoomData>.Error(GlobalFailCode.AuthenticationFailed);

      // 이미 방에 있는지 확인
      var existingRoom = _roomModel.GetUserRoom(userId);
      if (existingRoom != null)
        return ServiceResult<RoomData>.Error(GlobalFailCode.JoinRoomFailed);

      // 입장 가능한 방 목록 조회
      var availableRooms = _roomModel.GetRoomList()
        .Where(r => r.Users.Count < r.MaxUserNum && r.State == RoomStateType.Wait)
        .ToList();
      if (!availableRooms.Any())
        return ServiceResult<RoomData>.Error(GlobalFailCode.RoomNotFound);

      // 랜덤 방 선택
      var random = new Random();
      var selectedRoom = availableRooms[random.Next(availableRooms.Count)];

      // 로비용 기본 UserData 생성
      var userData = new UserData
      {
        Id = userId,
        Nickname = userCharacterData.Nickname,
        Character = new CharacterData
        {
          CharacterType = userCharacterData.LastSelectedCharacter,
          RoleType = RoleType.NoneRole,
          Hp = 0,
          Weapon = 0,
          StateInfo = new CharacterStateInfoData
          {
            State = CharacterStateType.NoneCharacterState,
            NextState = CharacterStateType.NoneCharacterState,
            NextStateAt = 0,
            StateTargetUserId = 0
          },
          BbangCount = 0,
          HandCardsCount = 0
        }
      };

      // 선택된 방 입장
      if (!_roomModel.JoinRoom(selectedRoom.Id, userData))
        return ServiceResult<RoomData>.Error(GlobalFailCode.JoinRoomFailed);

      // 업데이트된 방 정보 조회
      var updatedRoom = _roomModel.GetRoom(selectedRoom.Id);
      if (updatedRoom == null)
        return ServiceResult<RoomData>.Error(GlobalFailCode.RoomNotFound);

      return ServiceResult<RoomData>.Ok(updatedRoom.ToProto());
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[RoomService] JoinRandomRoom 실패: {ex.Message}");
      return ServiceResult<RoomData>.Error(GlobalFailCode.UnknownError);
    }
  }

  /// <summary>
  /// 방 퇴장
  /// </summary>
  public async Task<ServiceResult> LeaveRoom(int userId)
  {
    try
    {
      using var scope = _serviceProvider.CreateScope();
      var redisService = scope.ServiceProvider.GetRequiredService<RedisService>();
      var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

      // 유저 검증
      var userCharacterData = await redisService.GetUserCharacterTypeAsync(userId, dbContext);
      if (userCharacterData == null)
        return ServiceResult.Error(GlobalFailCode.AuthenticationFailed);

      // 현재 방 확인
      var currentRoom = _roomModel.GetUserRoom(userId);
      if (currentRoom == null)
        return ServiceResult.Error(GlobalFailCode.RoomNotFound);

      // 게임 중인지 확인
      if (currentRoom.State == RoomStateType.Ingame)
        return ServiceResult.Error(GlobalFailCode.InvalidRoomState);

      // 방 퇴장 처리
      if (!_roomModel.LeaveRoom(userId))
        return ServiceResult.Error(GlobalFailCode.LeaveRoomFailed);

      return ServiceResult.Ok();
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[RoomService] LeaveRoom 실패: {ex.Message}");
      return ServiceResult.Error(GlobalFailCode.UnknownError);
    }
  }

  /// <summary>
  /// 게임 레디 토글
  /// </summary>
  public async Task<ServiceResult> RoomReady(int userId, bool isReady)
  {
    try
    {
      using var scope = _serviceProvider.CreateScope();
      var redisService = scope.ServiceProvider.GetRequiredService<RedisService>();
      var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

      // 유저 검증
      var userCharacterData = await redisService.GetUserCharacterTypeAsync(userId, dbContext);
      if (userCharacterData == null)
        return ServiceResult.Error(GlobalFailCode.AuthenticationFailed);

      // 현재 방 검증
      var currentRoom = _roomModel.GetUserRoom(userId);
      if (currentRoom == null)
        return ServiceResult.Error(GlobalFailCode.RoomNotFound);

      // 방 상태 검증
      if (currentRoom.State != RoomStateType.Wait)
        return ServiceResult.Error(GlobalFailCode.InvalidRoomState);

      // 레디 상태 설정
      if (!currentRoom.SetUserReady(userId, isReady))
        return ServiceResult.Error(GlobalFailCode.UnknownError);

      return ServiceResult.Ok();
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[RoomService] GameReady 실패: {ex.Message}");
      return ServiceResult.Error(GlobalFailCode.UnknownError);
    }
  }

  /// <summary>
  /// 방 레디 상태 조회
  /// </summary>
  public async Task<ServiceResult<List<RoomUserReadyData>>> GetRoomReadyState(int roomId)
  {
    try
    {
      // 방 존재 여부 확인
      var room = _roomModel.GetRoom(roomId);
      if (room == null)
        return ServiceResult<List<RoomUserReadyData>>.Error(GlobalFailCode.RoomNotFound);

      // 방 상태 검증
      if (room.State != RoomStateType.Wait)
        return ServiceResult<List<RoomUserReadyData>>.Error(GlobalFailCode.InvalidRoomState);

      // 모든 유저의 레디 상태 수집
      var readyStates = await Task.Run(() => _roomModel.GetRoomReadyStates(roomId));
      return ServiceResult<List<RoomUserReadyData>>.Ok(readyStates);
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[RoomService] GetRoomReadyState 실패: {ex.Message}");
      return ServiceResult<List<RoomUserReadyData>>.Error(GlobalFailCode.UnknownError);
    }
  }

  /// <summary>
  /// 채팅 메시지 전송
  /// </summary>
  public async Task<ServiceResult> SendChatMessage(int userId, string message, ChatMessageType messageType)
  {
    try
    {
      using var scope = _serviceProvider.CreateScope();
      var redisService = scope.ServiceProvider.GetRequiredService<RedisService>();
      var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

      // 유저 검증
      var userCharacterData = await redisService.GetUserCharacterTypeAsync(userId, dbContext);
      if (userCharacterData == null)
        return ServiceResult.Error(GlobalFailCode.AuthenticationFailed);

      // 메시지 유효성 검사
      if (string.IsNullOrEmpty(message))
        return ServiceResult.Error(GlobalFailCode.InvalidRequest);

      return ServiceResult.Ok();
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[RoomService] SendChatMessage 실패: {ex.Message}");
      return ServiceResult.Error(GlobalFailCode.UnknownError);
    }
  }

  /// <summary>
  /// 게임 준비 단계 시작
  /// </summary>
  public async Task<ServiceResult> GamePrepare(int userId)
  {
    try
    {
      using var scope = _serviceProvider.CreateScope();
      var redisService = scope.ServiceProvider.GetRequiredService<RedisService>();
      var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

      // 유저 검증
      var userCharacterData = await redisService.GetUserCharacterTypeAsync(userId, dbContext);
      if (userCharacterData == null)
        return ServiceResult.Error(GlobalFailCode.AuthenticationFailed);

      // 현재 방 확인
      var currentRoom = _roomModel.GetUserRoom(userId);
      if (currentRoom == null)
        return ServiceResult.Error(GlobalFailCode.RoomNotFound);

      // 방 상태 검증
      if (currentRoom.State != RoomStateType.Wait)
        return ServiceResult.Error(GlobalFailCode.InvalidRoomState, "이미 게임이 시작되었거나 준비 중인 방입니다.");

      // 방장 권한 검증
      if (currentRoom.OwnerId != userId)
        return ServiceResult.Error(GlobalFailCode.InvalidRequest, "방장만 게임을 시작할 수 있습니다.");

      // 역할 분배
      var roles = GetRoleDistribution(currentRoom.GetAllUsers().Count);
      if (!roles.Any())
        return ServiceResult.Error(GlobalFailCode.InvalidRequest, "유효하지 않은 인원수입니다.");

      // 역할 목록 생성 및 섞기
      var roleList = roles.SelectMany(pair => Enumerable.Repeat(pair.Key, pair.Value)).ToList();
      var random = new Random();
      var shuffledRoles = roleList.OrderBy(_ => random.Next()).ToList();

      // 각 유저에게 역할 할당
      var users = currentRoom.GetAllUsers();
      for (int i = 0; i < users.Count; i++)
      {
        var user = users[i];
        var assignedRole = shuffledRoles[i];
        var userCharacter = await redisService.GetUserCharacterTypeAsync(user.Id, dbContext);
        if (userCharacter != null)
          SetInitialStats(user.Character, userCharacter.LastSelectedCharacter, assignedRole);
      }

      // 게임 준비 상태로 변경
      if (!_roomModel.SetRoomState(currentRoom.Id, RoomStateType.Prepare))
        return ServiceResult.Error(GlobalFailCode.UnknownError);

      return ServiceResult.Ok();
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[RoomService] GamePrepare 실패: {ex.Message}");
      return ServiceResult.Error(GlobalFailCode.UnknownError);
    }
  }

  /// <summary>
  /// 게임 시작
  /// </summary>
  public async Task<ServiceResult<GameServerInfo>> GameStart(int userId)
  {
    try
    {
      using var scope = _serviceProvider.CreateScope();
      var redisService = scope.ServiceProvider.GetRequiredService<RedisService>();
      var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
      var loadBalancer = scope.ServiceProvider.GetRequiredService<LoadBalancer>();

      // 유저 검증
      var userCharacterData = await redisService.GetUserCharacterTypeAsync(userId, dbContext);
      if (userCharacterData == null)
        return ServiceResult<GameServerInfo>.Error(GlobalFailCode.AuthenticationFailed);

      // 방 검증
      var currentRoom = _roomModel.GetUserRoom(userId);
      if (currentRoom == null)
        return ServiceResult<GameServerInfo>.Error(GlobalFailCode.RoomNotFound);

      // 방장 권한 검증
      if (currentRoom.OwnerId != userId)
        return ServiceResult<GameServerInfo>.Error(GlobalFailCode.InvalidRequest, "방장만 게임을 시작할 수 있습니다.");

      // 방 상태 검증
      if (currentRoom.State != RoomStateType.Prepare)
        return ServiceResult<GameServerInfo>.Error(GlobalFailCode.InvalidRoomState, "게임 준비 상태가 아닙니다.");

      // 게임 서버 할당
      var playerCount = currentRoom.GetAllUsers().Count;
      var gameServer = await loadBalancer.GetServerForRoom(playerCount);
      if (gameServer == null)
        return ServiceResult<GameServerInfo>.Error(GlobalFailCode.UnknownError, "사용 가능한 게임 서버가 없습니다.");

      // 게임 서버 플레이어 수 업데이트
      await loadBalancer.UpdateServerStatus(gameServer.Host, gameServer.Port, gameServer.CurrentPlayers + playerCount);

      // 게임 시작 상태로 변경
      if (!_roomModel.SetRoomState(currentRoom.Id, RoomStateType.Ingame))
        return ServiceResult<GameServerInfo>.Error(GlobalFailCode.UnknownError);

      return ServiceResult<GameServerInfo>.Ok(gameServer);
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[RoomService] GameStart 실패: {ex.Message}");
      return ServiceResult<GameServerInfo>.Error(GlobalFailCode.UnknownError);
    }
  }

  /// <summary>
  /// 인원 수에 따른 역할 분배 규칙을 반환
  /// </summary>
  private Dictionary<RoleType, int> GetRoleDistribution(int playerCount)
  {
    return playerCount switch
    {
      2 => new Dictionary<RoleType, int>
        {
          { RoleType.Target, 1 },
          { RoleType.Hitman, 1 }
        },
      3 => new Dictionary<RoleType, int>
        {
          { RoleType.Target, 1 },
          { RoleType.Hitman, 1 },
          { RoleType.Psychopath, 1 }
        },
      4 => new Dictionary<RoleType, int>
        {
          { RoleType.Target, 1 },
          { RoleType.Hitman, 2 },
          { RoleType.Psychopath, 1 }
        },
      5 => new Dictionary<RoleType, int>
        {
          { RoleType.Target, 1 },
          { RoleType.Bodyguard, 1 },
          { RoleType.Hitman, 2 },
          { RoleType.Psychopath, 1 }
        },
      6 => new Dictionary<RoleType, int>
        {
          { RoleType.Target, 1 },
          { RoleType.Bodyguard, 1 },
          { RoleType.Hitman, 3 },
          { RoleType.Psychopath, 1 }
        },
      7 => new Dictionary<RoleType, int>
        {
          { RoleType.Target, 1 },
          { RoleType.Bodyguard, 2 },
          { RoleType.Hitman, 3 },
          { RoleType.Psychopath, 1 }
        },
      _ => new Dictionary<RoleType, int>()
    };
  }

  /// <summary>
  /// 초기 스탯 설정
  /// </summary>
  private void SetInitialStats(CharacterData character, CharacterType selectedCharacter, RoleType role)
  {
    // 기존 캐릭터 데이터 초기화 필요
    character.StateInfo = new CharacterStateInfoData
    {
      State = CharacterStateType.NoneCharacterState,
      NextState = CharacterStateType.NoneCharacterState,
      NextStateAt = 0,
      StateTargetUserId = 0
    };

    // 카드 초기화
    character.HandCards.Clear();
    character.Equips.Clear();
    character.Debuffs.Clear();

    // 캐릭터 타입, 역할 설정
    character.CharacterType = selectedCharacter;
    character.RoleType = role;

    var characterInfo = _gameDataManager.GetCharacterByType(selectedCharacter);
    // 기본 스탯
    character.Hp = characterInfo?.BaseHp ?? 3;
    character.BbangCount = 1;
    character.HandCardsCount = 4;
    character.Weapon = 0;
  }
}