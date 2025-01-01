using HolyShitServer.Src.Data;

namespace HolyShitServer.Src.Models;

public class CharacterInfo
{
  public string Name { get; set; } = string.Empty;
  public List<CharacterStaticData> Data { get; set; } = new();
}