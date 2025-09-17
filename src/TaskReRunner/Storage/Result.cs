// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2025. All rights reserved.
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

using System;

using ArmoniK.Api.gRPC.V1;

namespace ArmoniK.TaskReRunner.Storage;

/// <summary>
///   Represents a result with its creation date, name, status, session ID, result ID, and data.
/// </summary>
public record Result
{
  /// <summary>
  ///   Gets or sets the creation date and time of the result.
  /// </summary>
  public required DateTime CreatedAt { get; init; }

  /// <summary>
  ///   Gets or sets the name of the result.
  /// </summary>
  public required string Name { get; init; }

  /// <summary>
  ///   Gets or sets the status of the result.
  /// </summary>
  public required ResultStatus Status { get; init; }

  /// <summary>
  ///   Gets or sets the session ID associated with the result.
  /// </summary>
  public required string SessionId { get; init; }

  /// <summary>
  ///   Gets or sets the unique identifier for the result.
  /// </summary>
  public required string ResultId { get; init; }

  /// <summary>
  ///   Gets or sets the optional data associated with the result. Nullable.
  /// </summary>
  public required byte[]? Data { get; init; }
}
