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
using System.CommandLine;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using ArmoniK.Api.Client;
using ArmoniK.Api.Client.Options;
using ArmoniK.Api.Client.Submitter;
using ArmoniK.Api.gRPC.V1;
using ArmoniK.Api.gRPC.V1.Results;
using ArmoniK.Api.gRPC.V1.Tasks;
using ArmoniK.TaskReRunner.Common;

using Newtonsoft.Json;

internal static class Program
{
  /// <summary>
  ///   Method for sending task and retrieving their results from ArmoniK
  /// </summary>
  /// <param name="endpoint">The endpoint url of ArmoniK's control plane</param>
  /// <param name="taskId">TaskId of the task to dump.</param>
  /// <returns>
  ///   Task representing the asynchronous execution of the method
  /// </returns>
  /// <exception cref="Exception">Issues with results from tasks</exception>
  /// <exception cref="ArgumentOutOfRangeException">Unknown response type from control plane</exception>
  internal static async Task Run(string endpoint,
                                 string taskId)
  {
    var channel = GrpcChannelFactory.CreateChannel(new GrpcClient
                                                   {
                                                     Endpoint = endpoint,
                                                   });

    // Create client for events listening
    var taskClient   = new Tasks.TasksClient(channel);
    var resultClient = new Results.ResultsClient(channel);


    var taskResponse = taskClient.GetTask(new GetTaskRequest
                                          {
                                            TaskId = taskId,
                                          });

    var rawData = new ConcurrentDictionary<string, byte[]?>();


    foreach (var data in taskResponse.Task.DataDependencies)
    {
      if (!string.IsNullOrEmpty(data))
      {
        rawData[data] = await resultClient.DownloadResultData(taskResponse.Task.SessionId,
                                                              data,
                                                              CancellationToken.None);
      }
    }

    foreach (var data in taskResponse.Task.ExpectedOutputIds)
    {
      if (!string.IsNullOrEmpty(data))
      {
        rawData[data] = await resultClient.DownloadResultData(taskResponse.Task.SessionId,
                                                              data,
                                                              CancellationToken.None);
      }
    }

    rawData[taskResponse.Task.PayloadId] = await resultClient.DownloadResultData(taskResponse.Task.SessionId,
                                                                                 taskResponse.Task.PayloadId,
                                                                                 CancellationToken.None);

    var DumpData = new TaskDump
                   {
                     SessionId          = taskResponse.Task.SessionId,
                     TaskId             = taskId,
                     TaskOptions        = taskResponse.Task.Options,
                     DataDependencies   = taskResponse.Task.DataDependencies,
                     ExpectedOutputKeys = taskResponse.Task.ExpectedOutputIds,
                     Configuration = new Configuration
                                     {
                                       DataChunkMaxSize = resultClient.GetServiceConfiguration(new Empty())
                                                                      .DataChunkMaxSize,
                                     },
                     RawData   = rawData,
                     PayloadId = taskResponse.Task.PayloadId, // change with plop.Task.PayloadId when in core
                   };
    var taskdata = taskResponse.Task;

    var JSONresult = JsonConvert.SerializeObject(DumpData);

    using (var tw = new StreamWriter($"Task_Id_{taskId}.json",
                                     false))
    {
      tw.WriteLine(JSONresult);
    }
  }

  public static async Task<int> Main(string[] args)
  {
    // Define the options for the application with their description and default value
    var endpoint = new Option<string>("--endpoint",
                                      description: "Endpoint for the connection to ArmoniK control plane.",
                                      getDefaultValue: () => "http://localhost:5001");

    var taskId = new Option<string>("--taskId",
                                    description: "TaskId of the task to dump.",
                                    getDefaultValue: () => "none");

    // Describe the application and its purpose
    var rootCommand = new RootCommand($"A program to extract data for a specific task. Connect to ArmoniK through <{endpoint.Name}>");

    // Add the options to the parser
    rootCommand.AddOption(endpoint);
    //rootCommand.AddOption(partition);
    rootCommand.AddOption(taskId);

    // Configure the handler to call the function that will do the work
    rootCommand.SetHandler(Run,
                           endpoint,
                           taskId);

    // Parse the command line parameters and call the function that represents the application
    return await rootCommand.InvokeAsync(args);
  }
}
