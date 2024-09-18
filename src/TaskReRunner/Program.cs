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
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using ArmoniK.Api.Common.Channel.Utils;
using ArmoniK.Api.Common.Options;
using ArmoniK.Api.gRPC.V1.Results;
using ArmoniK.Api.gRPC.V1.Tasks;
using ArmoniK.Api.gRPC.V1.Worker;
using ArmoniK.TaskReRunner.Storage;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Serilog;

using Spectre.Console;

namespace ArmoniK.TaskReRunner;

internal static class Program
{
  public static void CopyDirectory(string sourceDir,
                                   string destinationDir,
                                   bool   recursive)
  {
    // Get information about the source directory
    var dir = new DirectoryInfo(sourceDir);

    // Check if the source directory exists
    if (!dir.Exists)
    {
      throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");
    }

    // Cache directories before we start copying
    var dirs = dir.GetDirectories();

    // Create the destination directory
    Directory.CreateDirectory(destinationDir);

    // Get the files in the source directory and copy to the destination directory
    foreach (var file in dir.GetFiles())
    {
      var targetFilePath = Path.Combine(destinationDir,
                                        file.Name);
      file.CopyTo(targetFilePath);
    }

    // If recursive and copying subdirectories, recursively call this method
    if (recursive)
    {
      foreach (var subDir in dirs)
      {
        var newDestinationDir = Path.Combine(destinationDir,
                                             subDir.Name);
        CopyDirectory(subDir.FullName,
                      newDestinationDir,
                      true);
      }
    }
  }

  /// <summary>
  ///   Connect to a Worker to process tasks with specific parameters.
  /// </summary>
  /// <param name="path">Path to the json file containing the data needed to rerun the Task.</param>
  /// <param name="force">Allow deletion of the previous output of this program.</param>
  /// <exception cref="ArgumentException"></exception>
  public static async Task Run(string path,
                               bool   force)
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
                                            Address = Path.GetTempPath() + "sockets" + Path.DirectorySeparatorChar + "worker.sock",
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
    var input         = ProcessRequest.Parser.ParseJson(File.ReadAllText(path));
    var oldDataFolder = input.DataFolder;
    // Create a CommunicationToken if there isn't
    if (string.IsNullOrEmpty(input.CommunicationToken))
    {
      input.CommunicationToken = token;
    }

    var copyPath = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "ak_dumper_" + input.TaskId;
    if (Directory.Exists(copyPath))
    {
      if (force)
      {
        Directory.Delete(copyPath,
                         true);
      }
      else
      {
        throw new Exception("Use --force to allow deletion or delete ak_dumper_ + taskId");
      }
    }

    CopyDirectory(input.DataFolder + Path.DirectorySeparatorChar + "..",
                  copyPath,
                  true);

    input.DataFolder = copyPath + Path.DirectorySeparatorChar + "Results";
    // Create an AgentStorage to keep the Agent Data After Process
    var storage = new AgentStorage();

    // Scope for running the Task
    {
      // Launch an Agent server to listen the worker
      using var server = new Server(Path.GetTempPath() + "sockets" + Path.DirectorySeparatorChar + "agent.sock",
                                    storage,
                                    loggerConfiguration_);

      // Print information given to data
      logger_.LogInformation("Task Data: {input}",
                             input);

      // Call the Process method on the gRPC client `client` of type Worker.WorkerClient
      var ret = client.Process(input);
      logger_.LogInformation("Task Output : {ret}",
                             ret.ToString());
    }

    // Print all the data in agent storage

    logger_.LogInformation("Number of Notified result : {results}",
                           storage.NotifiedResults.Count);

    var i = 0;
    foreach (var result in storage.NotifiedResults)
    {
      logger_.LogInformation("Notified result {i} Id: {res}",
                             i,
                             result);
      var byteArray = File.ReadAllBytes(Path.Combine(input.DataFolder,
                                                     result));
      logger_.LogInformation("Notified result {i} Data : {str}",
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

    var jsonString    = File.ReadAllText(copyPath + Path.DirectorySeparatorChar + "CreatedResults.json");
    var resultOutputs = JsonSerializer.Deserialize<ConcurrentDictionary<string, ResultRaw>>(jsonString);

    var jsonStringTasks = File.ReadAllText(copyPath + Path.DirectorySeparatorChar + "Subtasks.json");
    var tasksOutputs    = JsonSerializer.Deserialize<ConcurrentDictionary<string, TaskSummary>>(jsonStringTasks);


    if (resultOutputs!.Count == storage.Results.Count)
    {
      logger_.LogInformation("ArmoniK Output and TaskReRunner output have Equal Created Results");
    }
    else
    {
      logger_.LogInformation("ArmoniK and TaskReRunner have Different Created Results : ArmoniK = {Count} TaskReRunner = {storage}",
                             resultOutputs!.Count.ToString(),
                             storage.Results.Count.ToString());
    }

    if (tasksOutputs!.Count == storage.Tasks.Count)
    {
      logger_.LogInformation("ArmoniK Output and TaskReRunner output have Equal Created Tasks");
    }
    else
    {
      logger_.LogInformation("ArmoniK and TaskReRunner have Different Created Tasks : ArmoniK = {tasksOutputs} TaskReRunner = {storage}",
                             tasksOutputs!.Count.ToString(),
                             storage.Tasks.Count.ToString());
    }

    var table = new Table();
    table.AddColumn("Category");
    table.AddColumn("Downloaded");
    table.AddColumn("Replayed");

    if (input.ExpectedOutputKeys.Count == 1 && File.Exists(oldDataFolder + Path.DirectorySeparatorChar + input.ExpectedOutputKeys.Single()))
    {
      var downloadedOutput = File.ReadAllBytes(oldDataFolder    + Path.DirectorySeparatorChar + input.ExpectedOutputKeys.Single());
      var replayedOutput   = File.ReadAllBytes(input.DataFolder + Path.DirectorySeparatorChar + input.ExpectedOutputKeys.Single());
      table.AddRow("Result Id",
                   input.ExpectedOutputKeys.Single(),
                   input.ExpectedOutputKeys.Single());
      table.AddRow("└─ size",
                   downloadedOutput.LongLength.ToString(),
                   replayedOutput.LongLength.ToString());


      if (!downloadedOutput.SequenceEqual(replayedOutput))
      {
        AnsiConsole.Write(table);
        table = new Table();
        table.AddColumn("Category");
        table.AddColumn("Downloaded");
        table.AddColumn("Replayed");
        logger_.LogInformation("ArmoniK Expected output is {test}",
                               downloadedOutput);
        logger_.LogInformation("TaskReRunner Expected output is {storage}",
                               replayedOutput);
      }
    }

    if (tasksOutputs!.Count == 1 && storage.Tasks.Count == 1)
    {
      table.AddRow("Task Id",
                   tasksOutputs.Single()
                               .Key,
                   storage.Tasks.Single()
                          .Key);
      table.AddRow("├─ nbr subtask",
                   tasksOutputs.Single()
                               .Value.CountDataDependencies.ToString(),
                   storage.Tasks.Single()
                          .Value.DataDependencies.Count.ToString());
      table.AddRow("├─ nbr result",
                   tasksOutputs.Single()
                               .Value.CountExpectedOutputIds.ToString(),
                   storage.Tasks.Single()
                          .Value.ExpectedOutputKeys.Count.ToString());
      table.AddRow("└─ TaskOption",
                   "-",
                   "-");
      table.AddRow("   ├─ Options",
                   JsonSerializer.Serialize(tasksOutputs.Single()
                                                        .Value.Options.Options),
                   JsonSerializer.Serialize(storage.Tasks.Single()
                                                   .Value.TaskOptions.Options));
      table.AddRow("   ├─ MaxDuration",
                   "-",
                   "-");
      table.AddRow("   │   ├─ Second",
                   tasksOutputs.Single()
                               .Value.Options.MaxDuration.Seconds.ToString(),
                   storage.Tasks.Single()
                          .Value.TaskOptions.MaxDuration.Seconds.ToString());
      table.AddRow("   │   └─ Nanos",
                   tasksOutputs.Single()
                               .Value.Options.MaxDuration.Nanos.ToString(),
                   storage.Tasks.Single()
                          .Value.TaskOptions.MaxDuration.Nanos.ToString());
      table.AddRow("   ├─ MaxRetries",
                   tasksOutputs.Single()
                               .Value.Options.MaxRetries.ToString(),
                   storage.Tasks.Single()
                          .Value.TaskOptions.MaxRetries.ToString());
      table.AddRow("   ├─ Priority",
                   tasksOutputs.Single()
                               .Value.Options.Priority.ToString(),
                   storage.Tasks.Single()
                          .Value.TaskOptions.Priority.ToString());
      table.AddRow("   ├─ PartitionId",
                   tasksOutputs.Single()
                               .Value.Options.PartitionId,
                   storage.Tasks.Single()
                          .Value.TaskOptions.PartitionId);
      table.AddRow("   ├─ ApplicationName",
                   tasksOutputs.Single()
                               .Value.Options.ApplicationName,
                   storage.Tasks.Single()
                          .Value.TaskOptions.ApplicationName);
      table.AddRow("   ├─ ApplicationNamespace",
                   tasksOutputs.Single()
                               .Value.Options.ApplicationVersion,
                   storage.Tasks.Single()
                          .Value.TaskOptions.ApplicationVersion);
      table.AddRow("   ├─ ApplicationNamespace",
                   tasksOutputs.Single()
                               .Value.Options.ApplicationNamespace,
                   storage.Tasks.Single()
                          .Value.TaskOptions.ApplicationNamespace);
      table.AddRow("   ├─ ApplicationService",
                   tasksOutputs.Single()
                               .Value.Options.ApplicationService,
                   storage.Tasks.Single()
                          .Value.TaskOptions.ApplicationService);
      table.AddRow("   └─ EngineType",
                   tasksOutputs.Single()
                               .Value.Options.EngineType,
                   storage.Tasks.Single()
                          .Value.TaskOptions.EngineType);
      if (storage.Results.Count == 1 && resultOutputs.Count == 1)
      {
        table.AddRow("Result Id",
                     resultOutputs.Single()
                                  .Key,
                     storage.Results.Single()
                            .Value.ResultId);
        table.AddRow("├─ name",
                     resultOutputs.Single()
                                  .Value.Name,
                     storage.Results.Single()
                            .Value.Name);
        table.AddRow("└─ size",
                     resultOutputs.Single()
                                  .Value.Size.ToString(),
                     storage.Results.Single()
                            .Value.Data!.LongLength.ToString());

        AnsiConsole.Write(table);

        var test = File.ReadAllBytes(oldDataFolder + Path.DirectorySeparatorChar + resultOutputs!.Single());
        if (!test.SequenceEqual(storage.Results.Single()
                                       .Value.Data!))
        {
          logger_.LogInformation("ArmoniK SubTask Payload is {test}",
                                 test);
          logger_.LogInformation("TaskReRunner SubTask Payload is {storage}",
                                 storage.Results.Single()
                                        .Value.Data);
        }
      }
      else
      {
        if (table.Rows.Count > 4)
        {
          AnsiConsole.Write(table);
        }
      }
    }

    await File.WriteAllTextAsync(copyPath + Path.DirectorySeparatorChar + "Subtasks.json",
                                 JsonSerializer.Serialize(storage.Tasks));


    await File.WriteAllTextAsync(copyPath + Path.DirectorySeparatorChar + "CreatedResults.json",
                                 JsonSerializer.Serialize(storage.Results));

    await File.WriteAllTextAsync(copyPath + Path.DirectorySeparatorChar + "Task.json",
                                 JsonSerializer.Serialize(input));

    foreach (var result in storage.Results)
    {
      await File.WriteAllBytesAsync(copyPath + Path.DirectorySeparatorChar + "Results" + Path.DirectorySeparatorChar + result.Key,
                                    result.Value.Data!);
    }
  }

  public static async Task<int> Main(string[] args)
  {
    // Define the options for the application with their description and default value
    var path = new Option<string>("--path",
                                  description: "Path to the JSON file containing the data needed to rerun the Task.",
                                  getDefaultValue: () => "task.json");

    var force = new Option<bool>("--force",
                                 description: "Allow this program previous output deletion",
                                 getDefaultValue: () => false);
    // Describe the application and its purpose
    var rootCommand =
      new RootCommand("This application allows you to rerun an individual ArmoniK task locally. It reads the data in <path>, connects to a worker, and reruns the Task.");

    rootCommand.AddOption(path);
    rootCommand.AddOption(force);

    // Configure the handler to call the function that will do the work
    rootCommand.SetHandler(Run,
                           path,
                           force);

    // Parse the command line parameters and call the function that represents the application
    return await rootCommand.InvokeAsync(args);
  }
}
