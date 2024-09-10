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

using System;
using System.Linq;
using System.Threading.Tasks;

using ArmoniK.Api.gRPC.V1;
using ArmoniK.Api.gRPC.V1.Agent;
using ArmoniK.TaskReRunner.Storage;

using Google.Protobuf.WellKnownTypes;

using Grpc.Core;

using Microsoft.Extensions.Logging;

namespace ArmoniK.TaskReRunner;

/// <summary>
///   An heritor of agent class that storethings
/// </summary>
public class ReRunnerAgent : Agent.AgentBase
{
  private readonly ILogger<ReRunnerAgent> logger_;
  private readonly AgentStorage           storage_;

  /// <summary>
  ///   Initializes a new instance of the RerunAgent class with the specified storage.
  /// </summary>
  /// <param name="storage">An instance of AgentStorage used to store agent data.</param>
  /// <param name="logger">An instance of </param>
  public ReRunnerAgent(AgentStorage           storage,
                       ILogger<ReRunnerAgent> logger)
  {
    storage_ = storage;
    logger_  = logger;
  }

  /// <summary>
  ///   Creates a Result with its MetaData: generates a result ID, retrieves its name, sets its status to created, adds the
  ///   creation date, and retrieves the session ID.
  ///   Registers the created result and its data in Results.
  /// </summary>
  /// <param name="request">Data related to the CreateResults request</param>
  /// <param name="context">Data related to the server</param>
  /// <returns>A response containing the created Result</returns>
  public override Task<CreateResultsResponse> CreateResults(CreateResultsRequest request,
                                                            ServerCallContext    context)
  {
    var results = request.Results.Select(rc =>
                                         {
                                           var resultId = Guid.NewGuid()
                                                              .ToString();
                                           var current = new Result
                                                         {
                                                           ResultId  = resultId,
                                                           Name      = rc.Name,
                                                           Status    = ResultStatus.Created,
                                                           CreatedAt = DateTime.UtcNow,
                                                           SessionId = request.SessionId,
                                                           Data      = rc.Data.ToByteArray(),
                                                         };
                                           storage_.Results[resultId] = current;
                                           return current;
                                         });

    return Task.FromResult(new CreateResultsResponse
                           {
                             CommunicationToken = request.CommunicationToken,
                             Results =
                             {
                               results.Select(result => new ResultMetaData
                                                        {
                                                          CreatedAt = Timestamp.FromDateTime(result.CreatedAt),
                                                          Name      = result.Name,
                                                          SessionId = result.SessionId,
                                                          Status    = result.Status,
                                                          ResultId  = result.ResultId,
                                                        }),
                             },
                           });
  }

  /// <summary>
  ///   Creates Result MetaData: generates a result ID, retrieves its name, sets its status to created, adds the creation
  ///   date, and retrieves the session ID.
  ///   Registers the created result metadata without any data in Results.
  /// </summary>
  /// <param name="request">Data related to CreateResultsMetaData the request</param>
  /// <param name="context">Data related to the server</param>
  /// <returns>A response containing the created Result MetaData</returns>
  public override Task<CreateResultsMetaDataResponse> CreateResultsMetaData(CreateResultsMetaDataRequest request,
                                                                            ServerCallContext            context)
  {
    var results = request.Results.Select(rc =>
                                         {
                                           var resultId = Guid.NewGuid()
                                                              .ToString();

                                           var current = new Result
                                                         {
                                                           ResultId  = resultId,
                                                           Name      = rc.Name,
                                                           Status    = ResultStatus.Created,
                                                           CreatedAt = DateTime.UtcNow,
                                                           SessionId = request.SessionId,
                                                           Data      = null,
                                                         };
                                           if (!storage_.Results.ContainsKey(resultId))
                                           {
                                             storage_.Results[resultId] = current;
                                           }

                                           return current;
                                         });

    return Task.FromResult(new CreateResultsMetaDataResponse
                           {
                             CommunicationToken = request.CommunicationToken,
                             Results =
                             {
                               results.Select(result => new ResultMetaData
                                                        {
                                                          CreatedAt = Timestamp.FromDateTime(result.CreatedAt),
                                                          Name      = result.Name,
                                                          SessionId = result.SessionId,
                                                          Status    = result.Status,
                                                          ResultId  = result.ResultId,
                                                        }),
                             },
                           });
  }

  /// <summary>
  ///   Notifies result data: adds result IDs from the request to the notified results list in storage.
  /// </summary>
  /// <param name="request">Data related to the NotifyResultData request</param>
  /// <param name="context">Data related to the server</param>
  /// <returns>A response containing the notified result IDs</returns>
  public override Task<NotifyResultDataResponse> NotifyResultData(NotifyResultDataRequest request,
                                                                  ServerCallContext       context)
  {
    foreach (var result in request.Ids)
    {
      storage_.NotifiedResults.Add(result.ResultId);
    }

    return Task.FromResult(new NotifyResultDataResponse
                           {
                             ResultIds =
                             {
                               request.Ids.Select(identifier => identifier.ResultId),
                             },
                           });
  }

  /// <summary>
  ///   Submits tasks: generates task IDs, retrieves their data dependencies, expected output keys, and payload IDs.
  ///   Registers the created tasks in Tasks.
  /// </summary>
  /// <param name="request">Data related to the  SubmitTasks request</param>
  /// <param name="context">Data related to the server</param>
  /// <returns>A response containing information about the submitted tasks</returns>
  public override Task<SubmitTasksResponse> SubmitTasks(SubmitTasksRequest request,
                                                        ServerCallContext  context)
  {
    var createdTasks = request.TaskCreations.Select(rc =>
                                                    {
                                                      var taskId = Guid.NewGuid()
                                                                       .ToString();
                                                      var current = new TaskData
                                                                    {
                                                                      DataDependencies   = rc.DataDependencies,
                                                                      ExpectedOutputKeys = rc.ExpectedOutputKeys,
                                                                      PayloadId          = rc.PayloadId,
                                                                      TaskId             = taskId,
                                                                      TaskOptions        = request.TaskOptions,
                                                                    };
                                                      storage_.Tasks[taskId] = current;
                                                      return current;
                                                    });
    return Task.FromResult(new SubmitTasksResponse
                           {
                             CommunicationToken = request.CommunicationToken,
                             TaskInfos =
                             {
                               createdTasks.Select(creationRequest => new SubmitTasksResponse.Types.TaskInfo
                                                                      {
                                                                        DataDependencies =
                                                                        {
                                                                          creationRequest.DataDependencies,
                                                                        },
                                                                        ExpectedOutputIds =
                                                                        {
                                                                          creationRequest.ExpectedOutputKeys,
                                                                        },
                                                                        PayloadId = creationRequest.PayloadId,
                                                                        TaskId    = creationRequest.TaskId,
                                                                      }),
                             },
                           });
  }
}
