// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: BlockProto.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace BlockBase.Domain.Protos {

  /// <summary>Holder for reflection information generated from BlockProto.proto</summary>
  public static partial class BlockProtoReflection {

    #region Descriptor
    /// <summary>File descriptor for BlockProto.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static BlockProtoReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "ChBCbG9ja1Byb3RvLnByb3RvEhdCbG9ja0Jhc2UuRG9tYWluLlByb3RvcxoW",
            "QmxvY2tIZWFkZXJQcm90by5wcm90bxoWVHJhbnNhY3Rpb25Qcm90by5wcm90",
            "byKNAQoKQmxvY2tQcm90bxI+CgtCbG9ja0hlYWRlchgBIAEoCzIpLkJsb2Nr",
            "QmFzZS5Eb21haW4uUHJvdG9zLkJsb2NrSGVhZGVyUHJvdG8SPwoMVHJhbnNh",
            "Y3Rpb25zGAIgAygLMikuQmxvY2tCYXNlLkRvbWFpbi5Qcm90b3MuVHJhbnNh",
            "Y3Rpb25Qcm90b2IGcHJvdG8z"));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { global::BlockBase.Domain.Protos.BlockHeaderProtoReflection.Descriptor, global::BlockBase.Domain.Protos.TransactionProtoReflection.Descriptor, },
          new pbr::GeneratedClrTypeInfo(null, new pbr::GeneratedClrTypeInfo[] {
            new pbr::GeneratedClrTypeInfo(typeof(global::BlockBase.Domain.Protos.BlockProto), global::BlockBase.Domain.Protos.BlockProto.Parser, new[]{ "BlockHeader", "Transactions" }, null, null, null)
          }));
    }
    #endregion

  }
  #region Messages
  public sealed partial class BlockProto : pb::IMessage<BlockProto> {
    private static readonly pb::MessageParser<BlockProto> _parser = new pb::MessageParser<BlockProto>(() => new BlockProto());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<BlockProto> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::BlockBase.Domain.Protos.BlockProtoReflection.Descriptor.MessageTypes[0]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public BlockProto() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public BlockProto(BlockProto other) : this() {
      blockHeader_ = other.blockHeader_ != null ? other.blockHeader_.Clone() : null;
      transactions_ = other.transactions_.Clone();
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public BlockProto Clone() {
      return new BlockProto(this);
    }

    /// <summary>Field number for the "BlockHeader" field.</summary>
    public const int BlockHeaderFieldNumber = 1;
    private global::BlockBase.Domain.Protos.BlockHeaderProto blockHeader_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public global::BlockBase.Domain.Protos.BlockHeaderProto BlockHeader {
      get { return blockHeader_; }
      set {
        blockHeader_ = value;
      }
    }

    /// <summary>Field number for the "Transactions" field.</summary>
    public const int TransactionsFieldNumber = 2;
    private static readonly pb::FieldCodec<global::BlockBase.Domain.Protos.TransactionProto> _repeated_transactions_codec
        = pb::FieldCodec.ForMessage(18, global::BlockBase.Domain.Protos.TransactionProto.Parser);
    private readonly pbc::RepeatedField<global::BlockBase.Domain.Protos.TransactionProto> transactions_ = new pbc::RepeatedField<global::BlockBase.Domain.Protos.TransactionProto>();
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public pbc::RepeatedField<global::BlockBase.Domain.Protos.TransactionProto> Transactions {
      get { return transactions_; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as BlockProto);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(BlockProto other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (!object.Equals(BlockHeader, other.BlockHeader)) return false;
      if(!transactions_.Equals(other.transactions_)) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      if (blockHeader_ != null) hash ^= BlockHeader.GetHashCode();
      hash ^= transactions_.GetHashCode();
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void WriteTo(pb::CodedOutputStream output) {
      if (blockHeader_ != null) {
        output.WriteRawTag(10);
        output.WriteMessage(BlockHeader);
      }
      transactions_.WriteTo(output, _repeated_transactions_codec);
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      if (blockHeader_ != null) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(BlockHeader);
      }
      size += transactions_.CalculateSize(_repeated_transactions_codec);
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(BlockProto other) {
      if (other == null) {
        return;
      }
      if (other.blockHeader_ != null) {
        if (blockHeader_ == null) {
          blockHeader_ = new global::BlockBase.Domain.Protos.BlockHeaderProto();
        }
        BlockHeader.MergeFrom(other.BlockHeader);
      }
      transactions_.Add(other.transactions_);
      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(pb::CodedInputStream input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
          case 10: {
            if (blockHeader_ == null) {
              blockHeader_ = new global::BlockBase.Domain.Protos.BlockHeaderProto();
            }
            input.ReadMessage(blockHeader_);
            break;
          }
          case 18: {
            transactions_.AddEntriesFrom(input, _repeated_transactions_codec);
            break;
          }
        }
      }
    }

  }

  #endregion

}

#endregion Designer generated code
