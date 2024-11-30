using Google.Protobuf;
using HolyShitServer.Src.Network.Packets;

namespace HolyShitServer.Src.Network.Protocol;

public class PacketSerializer
{
  private const int HEADER_SIZE = 11; // 2(type) + 1(versionLength) + 4(sequence) + 4(payloadLength)
  private static readonly string VERSION = "1.0.0"; // 버전 정보

  // 메시지를 바이트 배열로 직렬화 (S2C - Big Endian)
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
      var typeBytes = BitConverter.GetBytes((ushort)packetId);
      if (BitConverter.IsLittleEndian)
      {
        Array.Reverse(typeBytes);
      }
      typeBytes.CopyTo(result, offset);
      offset += 2;

      // Version Length (1 byte)
      result[offset] = (byte)versionBytes.Length;
      offset += 1;

      // Version (String
      versionBytes.CopyTo(result, offset);
      offset += versionBytes.Length;

      // 시퀀스 (4 bytes)
      var sequenceBytes = BitConverter.GetBytes(sequence);
      if (BitConverter.IsLittleEndian)
      {
        Array.Reverse(sequenceBytes);
      }
      sequenceBytes.CopyTo(result, offset);
      offset += 4;

      // Payload Length (4 bytes)
      var lengthBytes = BitConverter.GetBytes(messageBytes.Length);
      if (BitConverter.IsLittleEndian)
      {
        Array.Reverse(lengthBytes);
      }
      lengthBytes.CopyTo(result, offset);
      offset += 4;

      // Payload
      messageBytes.CopyTo(result, offset);

      return result;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Serialization 실패: {ex.Message}");
      return null;
    }
  }

  // 바이트 배열을 메시지로 역직렬화 (C2S - Little Endian)
  public static (PacketId id, uint sequence, T? message)? Deserialize<T>(byte[] buffer) where T : IMessage, new()
  {
    try
    {
      if (buffer.Length < HEADER_SIZE) return null;

      var offset = 0;

      // 패킷 타입 (2 bytes)
      var packetId = (PacketId)BitConverter.ToUInt16(buffer, offset);
      offset += 2;

      // Version Length (1 byte)
      var versionLength = buffer[offset];
      offset += 1;

      // Version (String)
      var version = Encoding.UTF8.GetString(buffer, offset, versionLength);
      if (version != VERSION)
      {
        Console.WriteLine($"버전 불일치: 예상={VERSION}, 실제={version}");
        return null;
      }
      offset += versionLength;

      // 시퀀스 (4 bytes)
      var sequence = BitConverter.ToUInt32(buffer.AsSpan(offset, 4));
      offset += 4;

      // Payload Length (4 bytes)
      var payloadLength = BitConverter.ToInt32(buffer.AsSpan(offset, 4));
      offset += 4;

      // Payload
      var message = new T();
      message.MergeFrom(new ReadOnlySpan<byte>(buffer, offset, payloadLength));

      return (packetId, sequence, message);
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Deserialization 실패: {ex.Message}");
      return null;
    }
  }
} 