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

using System.Text.Json;

using ArmoniK.Api.gRPC.V1;

namespace ArmoniK.TaskReRunner.Common;

/// <summary>
///   Represents all the parameters extracted from ArmoniK required to rerun a task.
///   Properties: PayloadId, SessionId, Configuration, TaskId, TaskOptions,
///   DataDependencies, ExpectedOutputKeys, RawData.
/// </summary>
public record TaskDump
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
  ///   Gets or sets the task options for the process.
  /// </summary>
  public required TaskOptions TaskOptions { get; init; }

  /// <summary>
  ///   Gets the list of data dependencies required for the process.
  /// </summary>
  public ICollection<string> DataDependencies { get; init; } = new List<string>();

  /// <summary>
  ///   Gets the list of expected output keys.
  /// </summary>
  public ICollection<string> ExpectedOutputKeys { get; init; } = new List<string>();

  /// <summary>
  ///   Gets or sets the configuration settings for the process.
  /// </summary>
  public required Configuration Configuration { get; init; }

  /// <summary>
  ///   Deserialize the text in the file in path to create a TaskDump object.
  /// </summary>
  /// <param name="path">The path to the file to deserialize</param>
  /// <returns>A TaskDump object deserialized from the file in path</returns>
  /// <exception cref="ArgumentException"> Throw if the Deserialization fail</exception>
  public static TaskDump Deserialize(string path)
  {
    var res = JsonSerializer.Deserialize<TaskDump>(File.ReadAllText(path));
    return res ?? throw new ArgumentException();
  }
  
  /// <summary>
  /// 
  /// </summary>
  /// <returns></returns>
  public string Serialize()
  {
    var res = JsonSerializer.Serialize(this);
    return res;
  }
}
