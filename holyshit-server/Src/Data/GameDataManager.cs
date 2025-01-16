using HolyShitServer.Src.Constants;
using HolyShitServer.Src.Models;
using HolyShitServer.Src.Network.Packets;
using HolyShitServer.Src.Utils.Loader;

namespace HolyShitServer.Src.Data;

public class GameDataManager
{
  private readonly JsonFileLoader _jsonLoader;
  private CharacterInfo? _characterInfo;

  public GameDataManager()
  {
    _jsonLoader = new JsonFileLoader();
  }

  public async Task InitializeDataAsync()
  {
    await Task.WhenAll(
      LoadCharacterInfoAsync()
    );
  }

  private async Task LoadCharacterInfoAsync()
  {
    try
    {
      _characterInfo = await Task.Run(() =>
        _jsonLoader.LoadFromAssets<CharacterInfo>(PathConstants.Assets.DataFiles.CHARACTER_INFO)
      );

      Console.WriteLine($"캐릭터 데이터 로드 성공: {_characterInfo.Data.Count}개");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"캐릭터 데이터 로드 실패: {ex.Message}");
      throw;
    }
  }

  public CharacterStaticData? GetCharacterByType(CharacterType type)
  {
    return _characterInfo?.Data.FirstOrDefault(c =>
      string.Equals(c.Type, type.ToString(), StringComparison.OrdinalIgnoreCase)); // 대소문자 구분 X
  }

  public List<CharacterStaticData> GetAllCharacters()
  {
    return _characterInfo?.Data ?? new List<CharacterStaticData>();
  }
}