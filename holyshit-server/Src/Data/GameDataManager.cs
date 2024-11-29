using HolyShitServer.Src.Constants;
using HolyShitServer.Src.Models;
using HolyShitServer.Src.Utils.Loader;

namespace HolyShitServer.Src.Data;

public class GameDataManager
{
  private readonly JsonFileLoader _jsonLoader;
  private MonsterInfo? _monsterInfo;

  public GameDataManager()
  {
    _jsonLoader = new JsonFileLoader();
  }

  public async Task InitializeDataAsync()
  {
    await LoadMonsterInfoAsync();
  }

  private async Task LoadMonsterInfoAsync()
  {
    try
    {
      _monsterInfo = await Task.Run(() =>
        _jsonLoader.LoadFromAssets<MonsterInfo>(PathConstants.Assets.DataFiles.MONSTER_INFO)
      );
      
      Console.WriteLine($"몬스터 데이터 로드 성공: {_monsterInfo.Data.Count}개");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"몬스터 데이터 로드 실패: {ex.Message}");
      throw;
    }
  }

  public MonsterData? GetMonsterById(string id)
  {
    return _monsterInfo?.Data.FirstOrDefault(m => m.Id == id);
  }
}