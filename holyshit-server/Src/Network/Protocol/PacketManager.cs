
using System.Collections.Immutable;
using Google.Protobuf;
using HolyShitServer.Src.Network.Packets;

namespace HolyShitServer.Src.Network.Protocol;

public class PacketManager
{
  private static readonly ImmutableDictionary<PacketId, MessageParser> _parser;
  /////////////// 작성중///////////////
}