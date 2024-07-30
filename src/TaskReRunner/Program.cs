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
using System.Threading.Tasks;

using ArmoniK.Api.Common.Channel.Utils;
using ArmoniK.Api.Common.Options;
using ArmoniK.Api.gRPC.V1.Worker;
using ArmoniK.TaskReRunner.Storage;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Serilog;

namespace ArmoniK.TaskReRunner;

internal static class Program
{
  /// <summary>
  ///   Connect to a Worker to process tasks with specific process parameter.
  /// </summary>
  /// <param name="path">Path to the json file containing the data needed to rerun the Task.</param>
  /// <param name="dataFolder">Absolute path to the folder created to contain the binary data required to rerun the Task.</param>
  /// <exception cref="ArgumentException"></exception>
  public static void Run(string path,
                         string dataFolder)
  {
    // Create a logger configuration to write output to the console with contextual information.
    var loggerConfiguration_ = new LoggerConfiguration().WriteTo.Console()
                                                        .Enrich.FromLogContext()
                                                        .CreateLogger();

    // Create a logger using the configured logger settings
    var logger_ = LoggerFactory.Create(builder => builder.AddSerilog(loggerConfiguration_))
                               .CreateLogger("root");

    // Set up the gRPC channel with the specified address and a null logger for the provider
    var channel = new GrpcChannelProvider(new GrpcChannel
                                          {
                                            Address = "/tmp/worker.sock",
                                          },
                                          new NullLogger<GrpcChannelProvider>()).Get();
    // Create the CommunicationToken
    var token = Guid.NewGuid()
                    .ToString();

    // Create a gRPC client for the Worker service
    var client = new Worker.WorkerClient(channel);

    if (!File.Exists(path))
    {
      logger_.LogError("ERROR: No JSON file in {path} ",
                       path);
      return;
    }

    //Deserialize the Data in the Json
    var input = TaskDump.Deserialize(path);

    // Create an AgentStorage to keep the Agent Data After Process
    var storage = new AgentStorage();

    // Scope for the Task to run 
    {
      // Launch an Agent server to listen the worker
      using var server = new Server("/tmp/agent.sock",
                                    storage,
                                    loggerConfiguration_);
      // Create a class with all values use to process a task 
      var toProcess = new ProcessData
                      {
                        CommunicationToken = token,
                        PayloadId          = input.PayloadId,
                        SessionId          = input.SessionId,
                        Configuration      = input.Configuration,
                        DataDependencies   = input.DataDependencies,
                        DataFolder         = dataFolder,
                        ExpectedOutputKeys = input.ExpectedOutputKeys,
                        TaskId             = input.TaskId,
                        TaskOptions        = input.TaskOptions,
                      };

      // Call the Process method on the gRPC client `client` of type Worker.WorkerClient
      client.Process(new ProcessRequest
                     {
                       CommunicationToken = token,
                       PayloadId          = input.PayloadId,
                       SessionId          = input.SessionId,
                       Configuration      = input.Configuration,
                       DataDependencies =
                       {
                         input.DataDependencies,
                       },
                       DataFolder = dataFolder,
                       ExpectedOutputKeys =
                       {
                         input.ExpectedOutputKeys,
                       },
                       TaskId      = input.TaskId,
                       TaskOptions = input.TaskOptions,
                     });
      // Print information given to data
      logger_.LogInformation("Task Data: {toProcess}",
                             toProcess);
    }

    // Print everything in agent storage
    logger_.LogInformation("resultsIds : {results}",
                           storage.NotifiedResults);

    var i = 0;
    foreach (var result in storage.NotifiedResults)
    {
      logger_.LogInformation("Notified result{i} Id: {res}",
                             i,
                             result);
      var byteArray = File.ReadAllBytes(Path.Combine(dataFolder,
                                                     result));
      logger_.LogInformation("Notified result{i} Data : {str}",
                             i,
                             byteArray);
      i++;
    }

    foreach (var result in storage.Results)
    {
      var str = result.Value.Data;
      logger_.LogInformation("Created Result MetaData : {result}",
                             result);
      logger_.LogInformation("Create Result Data : {str}",
                             str);
    }

    foreach (var task in storage.Tasks)
    {
      logger_.LogInformation("Submitted Task Data : {task}",
                             task.Value);
    }
  }

  public static async Task<int> Main(string[] args)
  {
    // Define the options for the application with their description and default value
    var path = new Option<string>("--path",
                                  description: "Path to the json file containing the data needed to rerun the Task.",
                                  getDefaultValue: () => "Data.json");
    var dataFolder = new Option<string>("--dataFolder",
                                        description: "Absolute path to the folder created to contain the binary data required to rerun the Task.",
                                        getDefaultValue: () => Path.GetTempPath());

    // Describe the application and its purpose
    var rootCommand =
      new RootCommand("This application allows you to rerun ArmoniK individual task in local. It reads the data in <path>, connect to a worker and rerun the Task.");

    rootCommand.AddOption(path);
    rootCommand.AddOption(dataFolder);

    // Configure the handler to call the function that will do the work
    rootCommand.SetHandler(Run,
                           path,
                           dataFolder);

    // Parse the command line parameters and call the function that represents the application
    return await rootCommand.InvokeAsync(args);
  }
}
