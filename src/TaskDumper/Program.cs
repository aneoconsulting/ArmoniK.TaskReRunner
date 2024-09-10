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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using ArmoniK.Api.Client;
using ArmoniK.Api.Client.Options;
using ArmoniK.Api.Client.Submitter;
using ArmoniK.Api.gRPC.V1;
using ArmoniK.Api.gRPC.V1.Results;
using ArmoniK.Api.gRPC.V1.SortDirection;
using ArmoniK.Api.gRPC.V1.Tasks;
using ArmoniK.Api.gRPC.V1.Worker;

using Grpc.Net.Client;

using Microsoft.Extensions.Configuration;

using FilterField = ArmoniK.Api.gRPC.V1.Tasks.FilterField;
using Filters = ArmoniK.Api.gRPC.V1.Tasks.Filters;
using FiltersAnd = ArmoniK.Api.gRPC.V1.Tasks.FiltersAnd;

namespace ArmoniK.TaskDumper;

public static class TaskPagination
{
  public static async IAsyncEnumerable<TaskSummary> ListTasksAsync(this GrpcChannel            channel,
                                                                   Filters                     filters,
                                                                   ListTasksRequest.Types.Sort sort,
                                                                   int                         pageSize = 50)
  {
    var               page       = 0;
    var               taskClient = new Tasks.TasksClient(channel);
    ListTasksResponse res;

    while ((res = await taskClient.ListTasksAsync(new ListTasksRequest
                                                  {
                                                    Filters  = filters,
                                                    Sort     = sort,
                                                    PageSize = pageSize,
                                                    Page     = page,
                                                  })
                                  .ConfigureAwait(false)).Tasks.Any())
    {
      foreach (var taskSummary in res.Tasks)
      {
        yield return taskSummary;
      }

      page++;
    }
  }
}

public static class ResultPagination
{
  public static async IAsyncEnumerable<ResultRaw> ListResultsAsync(this GrpcChannel              channel,
                                                                   Api.gRPC.V1.Results.Filters   filters,
                                                                   ListResultsRequest.Types.Sort sort,
                                                                   int                           pageSize = 50)
  {
    var                 page       = 0;
    var                 taskClient = new Results.ResultsClient(channel);
    ListResultsResponse res;

    while ((res = await taskClient.ListResultsAsync(new ListResultsRequest
                                                    {
                                                      Filters  = filters,
                                                      Sort     = sort,
                                                      PageSize = pageSize,
                                                      Page     = page,
                                                    })
                                  .ConfigureAwait(false)).Results.Any())
    {
      foreach (var taskSummary in res.Results)
      {
        yield return taskSummary;
      }

      page++;
    }
  }
}

internal static class Program
{
  /// <summary>
  ///   Method to retrieve information of task through its id from ArmoniK
  /// </summary>
  /// <param name="endpoint">The endpoint URL of ArmoniK's control plane.</param>
  /// <param name="taskId">The TaskId of the task to retrieve.</param>
  /// <param name="dataFolder">The folder to store all required binaries.</param>
  /// <param name="grpcClientOptions">grpc Specific Option, can be set through appsettings.json or environment variable</param>
  /// <returns>
  ///   Task representing the asynchronous execution of the method
  /// </returns>
  /// <exception cref="Exception">Thrown when there are issues with the results from tasks.</exception>
  /// <exception cref="ArgumentOutOfRangeException">Unknown response type from control plane</exception>
  internal static async Task Run(string      endpoint,
                                 string      taskId,
                                 string?     dataFolder,
                                 GrpcClient? grpcClientOptions)
  {
    Console.WriteLine(grpcClientOptions);
    var channel = await GrpcChannelFactory.CreateChannelAsync(grpcClientOptions ?? new GrpcClient
                                                                                   {
                                                                                     Endpoint = endpoint,
                                                                                   });
    // Set folder
    var folder = dataFolder ?? Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "ak_dumper_" + taskId;
    folder += Path.DirectorySeparatorChar;

    // Create clients for tasks and results.
    var taskClient   = new Tasks.TasksClient(channel);
    var resultClient = new Results.ResultsClient(channel);

    // Request task information from ArmoniK using the TaskId.
    var taskResponse = taskClient.GetTask(new GetTaskRequest
                                          {
                                            TaskId = taskId,
                                          });

    // Create a ProcessRequest object with information obtained from the task request.
    var DumpData = new ProcessRequest
                   {
                     SessionId   = taskResponse.Task.SessionId,
                     TaskId      = taskId,
                     TaskOptions = taskResponse.Task.Options,
                     DataDependencies =
                     {
                       taskResponse.Task.DataDependencies,
                     },
                     ExpectedOutputKeys =
                     {
                       taskResponse.Task.ExpectedOutputIds,
                     },
                     Configuration = new Configuration
                                     {
                                       DataChunkMaxSize = resultClient.GetServiceConfiguration(new Empty())
                                                                      .DataChunkMaxSize,
                                     },
                     PayloadId  = taskResponse.Task.PayloadId,
                     DataFolder = folder + "Results",
                   };

    // Convert the ProcessRequest object to JSON.
    var JSONresult = DumpData.ToString();

    // Create the dataFolder directory if it doesn't exist.
    if (!Directory.Exists(folder + "Results"))
    {
      Directory.CreateDirectory(folder + "Results");
    }

    // Write the JSON to a file with the specified name.
    await File.WriteAllTextAsync(folder + "Task.json",
                                 JSONresult);

    // Save DataDependencies data to files in the folder named <resultId>.
    foreach (var data in taskResponse.Task.DataDependencies)
    {
      var dataDependency = resultClient.GetResult(new GetResultRequest
                                                  {
                                                    ResultId = data,
                                                  });
      if (!string.IsNullOrEmpty(data))
      {
        if (dataDependency.Result.Status == ResultStatus.Completed)
        {
          await File.WriteAllBytesAsync(Path.Combine(folder + "Results",
                                                     data),
                                        await resultClient.DownloadResultData(taskResponse.Task.SessionId,
                                                                              data,
                                                                              CancellationToken.None) ?? Encoding.ASCII.GetBytes(""));
        }
      }
    }

    // Save ExpectedOutputs data to files in the folder named <resultId>.
    foreach (var data in taskResponse.Task.ExpectedOutputIds)
    {
      var expectedOutputs = resultClient.GetResult(new GetResultRequest
                                                   {
                                                     ResultId = data,
                                                   });
      if (!string.IsNullOrEmpty(data))
      {
        if (expectedOutputs.Result.Status == ResultStatus.Completed)
        {
          await File.WriteAllBytesAsync(Path.Combine(folder + "Results",
                                                     data),
                                        await resultClient.DownloadResultData(taskResponse.Task.SessionId,
                                                                              data,
                                                                              CancellationToken.None) ?? Encoding.ASCII.GetBytes(""));
        }
      }
    }

    // Save Payload data to a file in the folder named <PayloadID>.
    var payload = resultClient.GetResult(new GetResultRequest
                                         {
                                           ResultId = taskResponse.Task.PayloadId,
                                         });
    // Download payload
    if (payload.Result.Status == ResultStatus.Completed)
    {
      await File.WriteAllBytesAsync(Path.Combine(folder + "Results",
                                                 taskResponse.Task.PayloadId),
                                    await resultClient.DownloadResultData(taskResponse.Task.SessionId,
                                                                          taskResponse.Task.PayloadId,
                                                                          CancellationToken.None) ?? Encoding.ASCII.GetBytes(""));
    }

    //Search subtasks created by TaskId
    var taskCreated = channel.ListTasksAsync(new Filters
                                             {
                                               Or =
                                               {
                                                 new FiltersAnd
                                                 {
                                                   And =
                                                   {
                                                     new FilterField
                                                     {
                                                       FilterString = new FilterString
                                                                      {
                                                                        Operator = FilterStringOperator.Equal,
                                                                        Value    = taskId,
                                                                      },
                                                       Field = new TaskField
                                                               {
                                                                 TaskSummaryField = new TaskSummaryField
                                                                                    {
                                                                                      Field = TaskSummaryEnumField.CreatedBy,
                                                                                    },
                                                               },
                                                     },
                                                   },
                                                 },
                                               },
                                             },
                                             new ListTasksRequest.Types.Sort
                                             {
                                               Direction = SortDirection.Asc,
                                               Field = new TaskField
                                                       {
                                                         TaskSummaryField = new TaskSummaryField
                                                                            {
                                                                              Field = TaskSummaryEnumField.TaskId,
                                                                            },
                                                       },
                                             });

    // Create subtasks json in var folder  
    var tasks = new ConcurrentDictionary<string, TaskSummary>();
    await foreach (var task in taskCreated)
    {
      tasks[task.Id] = task;
    }

    await File.WriteAllTextAsync(folder + "Subtasks.json",
                                 JsonSerializer.Serialize(tasks));

    //Search results created by TaskId
    var resultsCreated = channel.ListResultsAsync(new Api.gRPC.V1.Results.Filters
                                                  {
                                                    Or =
                                                    {
                                                      new Api.gRPC.V1.Results.FiltersAnd
                                                      {
                                                        And =
                                                        {
                                                          new Api.gRPC.V1.Results.FilterField
                                                          {
                                                            FilterString = new FilterString
                                                                           {
                                                                             Operator = FilterStringOperator.Equal,
                                                                             Value    = taskId,
                                                                           },
                                                            Field = new ResultField
                                                                    {
                                                                      ResultRawField = new ResultRawField
                                                                                       {
                                                                                         Field = ResultRawEnumField.CreatedBy,
                                                                                       },
                                                                    },
                                                          },
                                                        },
                                                      },
                                                    },
                                                  },
                                                  new ListResultsRequest.Types.Sort
                                                  {
                                                    Direction = SortDirection.Asc,
                                                    Field = new ResultField
                                                            {
                                                              ResultRawField = new ResultRawField
                                                                               {
                                                                                 Field = ResultRawEnumField.ResultId,
                                                                               },
                                                            },
                                                  });

    // Create created results json in var folder  
    var results = new ConcurrentDictionary<string, ResultRaw>();

    await foreach (var result in resultsCreated)
    {
      results[result.ResultId] = result;
      // Put subtask results in var folder + "Results"
      await File.WriteAllBytesAsync(Path.Combine(folder + "Results",
                                                 result.ResultId),
                                    await resultClient.DownloadResultData(taskResponse.Task.SessionId,
                                                                          result.ResultId,
                                                                          CancellationToken.None) ?? Encoding.ASCII.GetBytes(""));
    }

    await File.WriteAllTextAsync(folder + "CreatedResults.json",
                                 JsonSerializer.Serialize(results));
  }


  public static async Task<int> Main(string[] args)
  {
    // Load configuration from environment variables and appsettings.json
    var configuration = new ConfigurationBuilder().AddInMemoryCollection()
                                                  .AddJsonFile("appsettings.json",
                                                               true,
                                                               false)
                                                  .AddEnvironmentVariables()
                                                  .Build();

    // Bind the configuration to the GrpcClient class
    var grpcClientOptions = configuration.GetSection("GrpcClientOptions")
                                         .Get<GrpcClient>();

    // Define the options for the application with their description and default value
    var endpoint = new Option<string>("--endpoint",
                                      description: "Endpoint for the connection to ArmoniK control plane.",
                                      getDefaultValue: () => "http://localhost:5001");

    var taskId = new Option<string>("--taskId",
                                    description: "TaskId of the task to retrieve",
                                    getDefaultValue: () => "none");

    var dataFolder = new Option<string?>("--dataFolder",
                                         description: "The absolute path to the folder for storing binary data required to rerun a task.",
                                         getDefaultValue: () => null);

    // Describe the application and its purpose
    var rootCommand = new RootCommand($"A program to extract data for a specific task. Connect to ArmoniK through <{endpoint.Name}>");

    // Add the options to the parser
    rootCommand.AddOption(endpoint);

    rootCommand.AddOption(taskId);

    rootCommand.AddOption(dataFolder);

    // Configure the handler to call the function that will do the work
    rootCommand.SetHandler((endpointValue,
                            taskIdValue,
                            dataFolderValue) => Run(endpointValue,
                                                    taskIdValue,
                                                    dataFolderValue,
                                                    grpcClientOptions),
                           endpoint,
                           taskId,
                           dataFolder);

    // Parse the command line parameters and call the function that represents the application
    return await rootCommand.InvokeAsync(args);
  }
}
