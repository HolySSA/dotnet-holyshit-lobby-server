using HolyShitServer.Src.Network.Packets;

namespace HolyShitServer.Src.Services.Results;

public class ServiceResult
{
  public bool Success { get; set; }
  public GlobalFailCode FailCode { get; set; }

  public static ServiceResult Error(GlobalFailCode code) =>
    new() { Success = false, FailCode = code };

  public static ServiceResult Ok() =>
    new() { Success = true, FailCode = GlobalFailCode.NoneFailcode };
}

public class ServiceResult<T> : ServiceResult
{
  public T? Data { get; set; }

  public static ServiceResult<T> Error(GlobalFailCode code) =>
    new() { Success = false, FailCode = code };

  public static ServiceResult<T> Ok(T data) =>
    new() { Success = true, Data = data, FailCode = GlobalFailCode.NoneFailcode };
}