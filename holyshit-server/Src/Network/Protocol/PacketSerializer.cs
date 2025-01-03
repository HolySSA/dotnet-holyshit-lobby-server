using System.Text;
using Google.Protobuf;
using HolyShitServer.Src.Network.Packets;

namespace HolyShitServer.Src.Network.Protocol;

public class PacketSerializer
{
  public const int HEADER_SIZE = 11; // 2(type) + 1(versionLength) + 4(sequence) + 4(payloadLength)
  private static readonly string VERSION = "1.0.0"; // 버전 정보

  // 메시지 직렬화 (S2C - Big Endian)
  public static byte[]? Serialize<T>(PacketId packetId, T message, uint sequence) where T : IMessage
  {
    try
    {
      var messageBytes = message.ToByteArray();
      var versionBytes = Encoding.UTF8.GetBytes(VERSION);
      var totalLength = HEADER_SIZE + versionBytes.Length + messageBytes.Length;
      var result = new byte[totalLength];
      var offset = 0;

      // 패킷 타입 (2 bytes)
      WriteBytes(result, ref offset, BitConverter.GetBytes((ushort)packetId));

      // 버전 길이 (1 byte) + 버전 문자열
      result[offset++] = (byte)versionBytes.Length;
      versionBytes.CopyTo(result, offset);
      offset += versionBytes.Length;

      // 시퀀스 (4 bytes)
      WriteBytes(result, ref offset, BitConverter.GetBytes(sequence));

      // 페이로드 길이 (4 bytes)
      WriteBytes(result, ref offset, BitConverter.GetBytes(messageBytes.Length));

      // 페이로드
      messageBytes.CopyTo(result, offset);

      return result;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[PacketSerializer] Serialization 실패: {ex.Message}");
      return null;
    }
  }

  // 메시지 역직렬화 (C2S - Little Endian)
  public static (PacketId id, uint sequence, IMessage? message)? Deserialize(byte[] buffer)
  {
    try
    {
      if (buffer.Length < HEADER_SIZE) return null;

      var offset = 0;

      // 패킷 타입 (2 bytes)
      var packetId = (PacketId)ReadUInt16(buffer, ref offset);

      // 버전 길이 (1 byte) + 버전 문자열
      var versionLength = buffer[offset++];
      if (offset + versionLength > buffer.Length) return null;
      offset += versionLength;  // 버전 검증이 필요하다면 여기서 수행

      // 시퀀스 (4 bytes)
      var sequence = ReadUInt32(buffer, ref offset);

      // 페이로드 길이 (4 bytes)
      var payloadLength = ReadInt32(buffer, ref offset);
      if (payloadLength <= 0 || payloadLength > 1024 * 1024) return null;

      // 페이로드
      if (buffer.Length < offset + payloadLength) return null;
      var payload = new byte[payloadLength];
      Array.Copy(buffer, offset, payload, 0, payloadLength);

      var message = PacketManager.ParseMessage(payload);
      return message == null ? null : (packetId, sequence, message);
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[PacketSerializer] Deserialization 실패: {ex.Message}");
      return null;
    }
  }

  public static (bool isValid, int totalSize) GetExpectedPacketSize(byte[] buffer)
  {
    try
    {
      if (buffer.Length < HEADER_SIZE)
        return (true, 0);

      var offset = 0;

      // 패킷 타입 검증
      var packetType = ReadUInt16(buffer, ref offset);
      if (packetType > 1000) return (false, 0);

      // 버전 길이 검증
      var versionLength = buffer[offset++];
      if (versionLength != 5) return (false, 0);

      // 시퀀스 건너뛰기
      offset += versionLength + 4;

      // 페이로드 길이
      var payloadLength = ReadInt32(buffer, ref offset);
      if (payloadLength <= 0 || payloadLength > 1024 * 1024)
        return (false, 0);

      return (true, HEADER_SIZE + versionLength + payloadLength);
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[PacketSerializer] GetExpectedPacketSize 실패: {ex.Message}");
      return (false, 0);
    }
  }

  private static void WriteBytes(byte[] buffer, ref int offset, byte[] data)
  {
    if (BitConverter.IsLittleEndian)
      Array.Reverse(data);
    data.CopyTo(buffer, offset);
    offset += data.Length;
  }

  private static ushort ReadUInt16(byte[] buffer, ref int offset)
  {
    var bytes = new byte[2];
    Array.Copy(buffer, offset, bytes, 0, 2);
    if (BitConverter.IsLittleEndian)
      Array.Reverse(bytes);
    offset += 2;
    return BitConverter.ToUInt16(bytes, 0);
  }

  private static uint ReadUInt32(byte[] buffer, ref int offset)
  {
    var bytes = new byte[4];
    Array.Copy(buffer, offset, bytes, 0, 4);
    if (BitConverter.IsLittleEndian)
      Array.Reverse(bytes);
    offset += 4;
    return BitConverter.ToUInt32(bytes, 0);
  }

  private static int ReadInt32(byte[] buffer, ref int offset)
  {
    var bytes = new byte[4];
    Array.Copy(buffer, offset, bytes, 0, 4);
    if (BitConverter.IsLittleEndian)
      Array.Reverse(bytes);
    offset += 4;
    return BitConverter.ToInt32(bytes, 0);
  }
}