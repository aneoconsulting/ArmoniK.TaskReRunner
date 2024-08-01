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
using System.CommandLine;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using ArmoniK.Api.Client;
using ArmoniK.Api.Client.Options;
using ArmoniK.Api.Client.Submitter;
using ArmoniK.Api.gRPC.V1;
using ArmoniK.Api.gRPC.V1.Results;
using ArmoniK.Api.gRPC.V1.Tasks;
using ArmoniK.Api.gRPC.V1.Worker;

using TaskStatus = ArmoniK.Api.gRPC.V1.TaskStatus;

internal static class Program
{
  /// <summary>
  ///   Method to retrieve information of task through its id from ArmoniK
  /// </summary>
  /// <param name="endpoint">The endpoint URL of ArmoniK's control plane.</param>
  /// <param name="taskId">The TaskId of the task to retrieve.</param>
  /// <param name="dataFolder">The folder to store all required binaries.</param>
  /// <param name="name">The name of the newly created JSON file.</param>
  /// <returns>
  ///   Task representing the asynchronous execution of the method
  /// </returns>
  /// <exception cref="Exception">Thrown when there are issues with the results from tasks.</exception>
  /// <exception cref="ArgumentOutOfRangeException">Unknown response type from control plane</exception>
  internal static async Task Run(string endpoint,
                                 string taskId,
                                 string dataFolder,
                                 string name)
  {
    var channel = GrpcChannelFactory.CreateChannel(new GrpcClient
                                                   {
                                                     Endpoint = endpoint,
                                                   });

    // Create clients for tasks and results.
    var taskClient   = new Tasks.TasksClient(channel);
    var resultClient = new Results.ResultsClient(channel);

    // Request task information from ArmoniK using the TaskId.
    var taskResponse = taskClient.GetTask(new GetTaskRequest
                                          {
                                            TaskId = taskId,
                                          });

    Console.WriteLine(taskResponse);

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
                     PayloadId = taskResponse.Task.PayloadId,
                   };
    // Convert the ProcessRequest object to JSON.
    var JSONresult = DumpData.ToString();

    // Write the JSON to a file with the specified name.
    using (var tw = new StreamWriter(name,
                                     false))
    {
      tw.WriteLine(JSONresult);
    }

    // Create the dataFolder directory if it doesn't exist.
    if (!Directory.Exists(dataFolder))
    {
      Directory.CreateDirectory(dataFolder);
    }

    // Save DataDependencies data to files in the dataFolder named <resultId>.
    foreach (var data in taskResponse.Task.DataDependencies)
    {
      if (!string.IsNullOrEmpty(data))
      {
        await File.WriteAllBytesAsync(Path.Combine(dataFolder,
                                                   data),
                                      await resultClient.DownloadResultData(taskResponse.Task.SessionId,
                                                                            data,
                                                                            CancellationToken.None) ?? Encoding.ASCII.GetBytes(""));
      }
    }

    // Save Payload data to a file in the dataFolder named <PayloadID>.
    if (taskResponse.Task.Status != TaskStatus.Completed)
    {
      await File.WriteAllBytesAsync(Path.Combine(dataFolder,
                                                 taskResponse.Task.PayloadId),
                                    await resultClient.DownloadResultData(taskResponse.Task.SessionId,
                                                                          taskResponse.Task.PayloadId,
                                                                          CancellationToken.None) ?? Encoding.ASCII.GetBytes(""));
    }
  }

  public static async Task<int> Main(string[] args)
  {
    // Define the options for the application with their description and default value
    var endpoint = new Option<string>("--endpoint",
                                      description: "Endpoint for the connection to ArmoniK control plane.",
                                      getDefaultValue: () => "http://localhost:5001");

    var taskId = new Option<string>("--taskId",
                                    description: "TaskId of the task to retrieve",
                                    getDefaultValue: () => "none");

    var dataFolder = new Option<string>("--dataFolder",
                                        description: "The absolute path to the folder for storing binary data required to rerun a task.",
                                        getDefaultValue: () => Path.GetTempPath());

    var name = new Option<string>("--name",
                                  description: "The name of the JSON file to be created.",
                                  getDefaultValue: () => "Task_Id.json");
    // Describe the application and its purpose
    var rootCommand = new RootCommand($"A program to extract data for a specific task. Connect to ArmoniK through <{endpoint.Name}>");

    // Add the options to the parser
    rootCommand.AddOption(endpoint);

    rootCommand.AddOption(taskId);

    rootCommand.AddOption(dataFolder);

    rootCommand.AddOption(name);

    // Configure the handler to call the function that will do the work
    rootCommand.SetHandler(Run,
                           endpoint,
                           taskId,
                           dataFolder,
                           name);

    // Parse the command line parameters and call the function that represents the application
    return await rootCommand.InvokeAsync(args);
  }
}
