using HolyShitServer.Src.Network.Packets;

namespace HolyShitServer.Src.Services.Results;

public class ServiceResult
{
    public bool Success { get; private set; }
    public string Message { get; private set; }
    public GlobalFailCode FailCode { get; private set; }

    protected ServiceResult(bool success, string message = "", GlobalFailCode failCode = GlobalFailCode.NoneFailcode)
    {
        Success = success;
        Message = message;
        FailCode = failCode;
    }

    public static ServiceResult Ok(string message = "") => new ServiceResult(true, message);
    public static ServiceResult Error(GlobalFailCode failCode, string message = "") => new ServiceResult(false, message, failCode);
}

public class ServiceResult<T> : ServiceResult where T : class
{
    public T? Data { get; private set; }

    private ServiceResult(bool success, T? data = null, string message = "", GlobalFailCode failCode = GlobalFailCode.NoneFailcode)
        : base(success, message, failCode)
    {
        Data = data;
    }

    public static ServiceResult<T> Ok(T data, string message = "") => new ServiceResult<T>(true, data, message);
    public new static ServiceResult<T> Error(GlobalFailCode failCode, string message = "") => new ServiceResult<T>(false, null, message, failCode);
}