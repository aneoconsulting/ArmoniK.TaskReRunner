// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2024. All rights reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License")
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections.Concurrent;
using System.Collections.Generic;

using ArmoniK.Api.gRPC.V1;

namespace ArmoniK.TaskReRunner;

/// <summary>
///   Represents all the parameters needed to launch a process.
///   Properties: CommunicationToken, PayloadId, SessionId, Configuration, DataFolder, TaskId, TaskOptions,
///   DataDependencies, ExpectedOutputKeys.
/// </summary>
public record ProcessData
{
  /// <summary>
  ///   Gets or sets the session identifier.
  /// </summary>
  public required string SessionId { get; init; }

  /// <summary>
  ///   Gets or init the payload identifier.
  /// </summary>
  public required string PayloadId { get; init; }

  /// <summary>
  ///   Gets or sets the task identifier.
  /// </summary>
  public required string TaskId { get; init; }

  /// <summary>
  ///   Gets or sets the task options for the process. Nullable.
  /// </summary>
  public required TaskOptions? TaskOptions { get; init; }

  /// <summary>
  ///   Gets the list of data dependencies required for the process.
  /// </summary>
  public ICollection<string> DataDependencies { get; } = new List<string>();

  /// <summary>
  ///   Gets the list of expected output keys.
  /// </summary>
  public ICollection<string> ExpectedOutputKeys { get; } = new List<string>();

  /// <summary>
  ///   Gets or init the communication token required for the process.
  /// </summary>
  public required string CommunicationToken { get; init; }

  /// <summary>
  ///   Gets or sets the configuration settings for the process.
  /// </summary>
  public required Configuration Configuration { get; init; }

  /// <summary>
  ///   Gets or sets the folder location for storing data.
  /// </summary>
  public required string? DataFolder { get; init; }

  /// <summary>
  ///   Get or init a dictionary containing the payload, data dependencies, and expected outputs corresponding byte array.
  /// </summary>
  public ConcurrentDictionary<string, byte[]?> RawData { get; init; } = new();
}
