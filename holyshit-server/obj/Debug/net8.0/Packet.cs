// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: Packet.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021, 8981
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace HolyShitServer.Src.Network.Packets {

  /// <summary>Holder for reflection information generated from Packet.proto</summary>
  public static partial class PacketReflection {

    #region Descriptor
    /// <summary>File descriptor for Packet.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static PacketReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "CgxQYWNrZXQucHJvdG8qcwoIUGFja2V0SWQSCwoHVU5LTk9XThAAEhYKEkMy",
            "U3JlZ2lzdGVyUmVxdWVzdBABEhcKE1MyQ3JlZ2lzdGVyUmVzcG9uc2UQAhIT",
            "Cg9DMlNsb2dpblJlcXVlc3QQAxIUChBTMkNsb2dpblJlc3BvbnNlEARCJaoC",
            "IkhvbHlTaGl0U2VydmVyLlNyYy5OZXR3b3JrLlBhY2tldHNiBnByb3RvMw=="));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { },
          new pbr::GeneratedClrTypeInfo(new[] {typeof(global::HolyShitServer.Src.Network.Packets.PacketId), }, null, null));
    }
    #endregion

  }
  #region Enums
  public enum PacketId {
    [pbr::OriginalName("UNKNOWN")] Unknown = 0,
    [pbr::OriginalName("C2SregisterRequest")] C2SregisterRequest = 1,
    [pbr::OriginalName("S2CregisterResponse")] S2CregisterResponse = 2,
    [pbr::OriginalName("C2SloginRequest")] C2SloginRequest = 3,
    [pbr::OriginalName("S2CloginResponse")] S2CloginResponse = 4,
  }

  #endregion

}

#endregion Designer generated code
