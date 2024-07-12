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

using System.Collections.Generic;

namespace ArmoniK.TaskReRunner;

/// <summary>
///   Represents task data with its data dependencies, expected output keys, payload ID, and task ID.
///   Required properties: PayloadId, TaskId.
///   Optional properties: DataDependencies, ExpectedOutputKeys.
/// </summary>
public record TaskData
{
  /// <summary>
  ///   Gets or init the task identifier. Required.
  /// </summary>
  public required string TaskId { get; init; }

  /// <summary>
  ///   Gets or init the payload identifier. Required.
  /// </summary>
  public required string PayloadId { get; init; }

  /// <summary>
  ///   Gets the list of data dependencies required for the process.
  /// </summary>
  public ICollection<string> DataDependencies { get; init; } = new List<string>();

  /// <summary>
  ///   Gets the list of expected output keys.
  /// </summary>
  public ICollection<string> ExpectedOutputKeys { get; init; } = new List<string>();
}
