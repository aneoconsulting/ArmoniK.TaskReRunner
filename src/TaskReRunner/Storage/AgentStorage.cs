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

namespace ArmoniK.TaskReRunner.Storage;

/// <summary>
///   A storage class to keep Tasks and Result data.
/// </summary>
internal class AgentStorage
{
  /// <summary>
  ///   The data obtained through the notified result call. This set contains the IDs of the results that have been notified.
  /// </summary>
  public readonly HashSet<string> NotifiedResults = new();


  /// <summary>
  ///   The data obtained through the CreateResult or CreateMetaDataResult call. This dictionary stores results keyed by
  ///   their unique IDs.
  /// </summary>
  public ConcurrentDictionary<string, Result> Results = new();

  /// <summary>
  ///   The data obtained through the SubmitTask call. This dictionary stores task data keyed by their unique task IDs.
  /// </summary>
  public ConcurrentDictionary<string, TaskData> Tasks = new();
}
