#!/bin/bash

~/.nuget/packages/google.protobuf.tools/3.6.1/tools/linux_x64/protoc -I=. --csharp_out=. ./NetworkMessageProto.proto ./BlockHeaderProto.proto ./TransactionProto.proto ./BlockProto.proto