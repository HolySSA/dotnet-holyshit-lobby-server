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

  // 메시지 역직렬화 (C2S - Little Endian)
  public static (PacketId id, uint sequence, IMessage? message)? Deserialize(byte[] buffer)
  {
    try
    {
      if (buffer.Length < HEADER_SIZE)
      {
        Console.WriteLine($"버퍼 크기 부족: {buffer.Length} < {HEADER_SIZE}");
        return null;
      }

      var offset = 0;
      Console.WriteLine($"Received buffer: {BitConverter.ToString(buffer)}");

      // 패킷 타입 (2 bytes)
      var packetId = (PacketId)(buffer[offset + 1] | (buffer[offset] << 8));
      Console.WriteLine($"PacketId: {packetId}");
      offset += 2;

      // Version Length (1 byte)
      var versionLength = buffer[offset];
      Console.WriteLine($"Version Length: {versionLength}");
      offset += 1;

      // Version (String)
      if (offset + versionLength > buffer.Length)
      {
        Console.WriteLine("버전 문자열 읽기 실패");
        return null;
      }
      var version = Encoding.UTF8.GetString(buffer, offset, versionLength);
      Console.WriteLine($"Version: {version}");
      if (version != VERSION)
      {
        Console.WriteLine($"버전 불일치: 예상={VERSION}, 실제={version}");
        return null;
      }
      offset += versionLength;

      // 시퀀스 (4 bytes)
      var sequence = (uint)(buffer[offset + 3] | 
                            (buffer[offset + 2] << 8) | 
                            (buffer[offset + 1] << 16) | 
                            (buffer[offset] << 24));
      Console.WriteLine($"Sequence: {sequence}");
      offset += 4;

      // Payload Length (4 bytes)
      // 페이로드 길이 바이트 출력
      Console.WriteLine($"Payload Length bytes: {BitConverter.ToString(buffer, offset, 4)}");
      var payloadLength = buffer[offset + 3] | 
                          (buffer[offset + 2] << 8) | 
                          (buffer[offset + 1] << 16) | 
                          (buffer[offset] << 24);
      Console.WriteLine($"Calculated Payload Length: {payloadLength}");
      offset += 4;

      // 페이로드 길이 유효성 검사
      if (payloadLength <= 0 || offset + payloadLength > buffer.Length)
      {
          Console.WriteLine($"페이로드 읽기 실패: 필요={payloadLength}, 가능={buffer.Length - offset}");
          return null;
      }

      // 페이로드 읽기
      var payload = new byte[payloadLength];
      Array.Copy(buffer, offset, payload, 0, payloadLength);
      
      // 페이로드 내용 출력
      Console.WriteLine($"페이로드 데이터: {BitConverter.ToString(payload)}");

      // 메시지 파싱
      var message = PacketManager.ParseMessage(packetId, payload);
      if (message == null)
      {
          Console.WriteLine($"메시지 파싱 실패: ID={packetId}");
          return null;
      }

      Console.WriteLine($"Deserialize 성공: ID={packetId}, Sequence={sequence}, Type={message.GetType().Name}");
      return (packetId, sequence, message);
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Deserialization 실패: {ex.Message}");
      return null;
    }
  }
} 