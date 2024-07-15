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
using System.IO;
using System.Text;

using ArmoniK.Api.Common.Channel.Utils;
using ArmoniK.Api.Common.Options;
using ArmoniK.Api.gRPC.V1;
using ArmoniK.Api.gRPC.V1.Worker;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Newtonsoft.Json;

using Serilog;

namespace ArmoniK.TaskReRunner;

internal static class Program
{
  /// <summary>
  ///   Connect to a Worker to process tasks with specific process parameter.
  /// </summary>
  /// <param name="arg">Command-line arguments.</param>
  public static void Main(string[] arg)
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

    // Create a gRPC client for the Worker service
    var client = new Worker.WorkerClient(channel);

    // Generate a unique identifier for the payload
    var payloadId = Guid.NewGuid()
                        .ToString();

    // Generate a unique identifier for the task
    var taskId = Guid.NewGuid()
                     .ToString();

    // Generate a unique identifier for the communication token
    var token = Guid.NewGuid()
                    .ToString();

    // Generate a unique identifier for the session
    var sessionId = Guid.NewGuid()
                        .ToString();

    // Generate a unique identifier for the first data dependency
    var dd1 = Guid.NewGuid()
                  .ToString();

    // Generate a unique identifier for the first expected output key
    var eok1 = Guid.NewGuid()
                   .ToString();

    // Generate a unique identifier for the second expected output key
    var eok2 = Guid.NewGuid()
                   .ToString();

    // Create a temporary directory and get its full path
    var folder = Directory.CreateTempSubdirectory()
                          .FullName;

    // Convert the integer 8 to a byte array for the payload
    var payloadBytes = BitConverter.GetBytes(8);

    // Convert the string "DataDependency1" to a byte array using ASCII encoding
    var dd1Bytes = Encoding.ASCII.GetBytes("DataDependency1");

    // Write payloadBytes in the corresponding file
    File.WriteAllBytesAsync(Path.Combine(folder,
                                         payloadId),
                            payloadBytes);

    // Write payloadBytes in the corresponding file
    File.WriteAllBytesAsync(Path.Combine(folder,
                                         dd1),
                            dd1Bytes);
    // Create an AgentStorage to keep the Agent Data After Process
    var storage = new AgentStorage();

    // Scope for the Task to run 
    {
      // Launch a Agent server to listen the worker
      using var server = new Server("/tmp/agent.sock",
                                    storage,
                                    loggerConfiguration_);

      // To test subtasking partition
      var taskOptions = new TaskOptions();
      taskOptions.Options["UseCase"] = "Launch";
      var configuration = new Configuration
                          {
                            DataChunkMaxSize = 84,
                          };


      // Register the parameters needed for processing : 
      // communication token, payload and session IDs, configuration settings, data dependencies, folder location, expected output keys, task ID, and task options.
      var toProcess = new ProcessData
                      {
                        CommunicationToken = token,
                        PayloadId          = payloadId,
                        SessionId          = sessionId,
                        Configuration      = configuration,
                        DataDependencies =
                        {
                          dd1,
                        },
                        DataFolder = folder,
                        ExpectedOutputKeys =
                        {
                          eok1,
                        },
                        TaskId      = taskId,
                        TaskOptions = taskOptions,
                      };

      /*
       * Create a JSON file with all Data
       */
      var JSONresult = JsonConvert.SerializeObject(toProcess);
      var path       = "./toProcess.json";
      if (File.Exists(path))
      {
        File.Delete(path);
        using (var tw = new StreamWriter(path,
                                         true))
        {
          tw.WriteLine(JSONresult);
          tw.Close();
        }
      }
      else if (!File.Exists(path))
      {
        using (var tw = new StreamWriter(path,
                                         true))
        {
          tw.WriteLine(JSONresult);
          tw.Close();
        }
      }

      // Call the Process method on the gRPC client `client` of type Worker.WorkerClient
      client.Process(new ProcessRequest
                     {
                       CommunicationToken = token,
                       PayloadId          = payloadId,
                       SessionId          = sessionId,
                       Configuration      = configuration,
                       DataDependencies =
                       {
                         dd1,
                       },
                       DataFolder = folder,
                       ExpectedOutputKeys =
                       {
                         eok1,
                         //eok2, // Uncomment to test multiple expected output keys (results)
                       },
                       TaskId      = taskId,
                       TaskOptions = taskOptions,
                     });

      logger_.LogInformation("Task Data: {toProcess}",
                             toProcess);
    }

    // print everything in agent storage

    logger_.LogInformation("resultsIds : {results}",
                           storage.NotifiedResults);

    var i = 0;
    foreach (var result in storage.NotifiedResults)
    {
      logger_.LogInformation("Notified result{i} Id: {res}",
                             i,
                             result);
      var byteArray = File.ReadAllBytes(Path.Combine(folder,
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
}
