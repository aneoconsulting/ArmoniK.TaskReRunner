// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2025. All rights reserved.
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
using System.Threading.Tasks;

using ArmoniK.TaskReRunner.Storage;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Serilog;
using Serilog.Core;

namespace ArmoniK.TaskReRunner;

internal class Server : IDisposable
{
  private readonly WebApplication app_;
  private readonly Task           runningApp_;

  /// <summary>
  ///   Initializes a new instance of the Server class, configuring the server to listen on a Unix socket, set up logging,
  ///   and add gRPC services.
  /// </summary>
  /// <param name="socket">The Unix socket path for the server to listen on.</param>
  /// <param name="storage">The AgentStorage instance to store agent data.</param>
  /// <param name="loggerConfiguration">The Serilog logger configuration for logging.</param>
  public Server(string       socket,
                AgentStorage storage,
                Logger       loggerConfiguration)
  {
    var builder = WebApplication.CreateBuilder();

    builder.WebHost.ConfigureKestrel(options => options.ListenUnixSocket(socket,
                                                                         listenOptions =>
                                                                         {
                                                                           if (File.Exists(socket))
                                                                           {
                                                                             File.Delete(socket);
                                                                           }

                                                                           listenOptions.Protocols = HttpProtocols.Http2;
                                                                         }));

    builder.Host.UseSerilog(loggerConfiguration);

    builder.Services.AddSingleton(storage)
           .AddGrpcReflection()
           .AddGrpc(options => options.MaxReceiveMessageSize = null);

    app_ = builder.Build();

    if (app_.Environment.IsDevelopment())
    {
      app_.UseDeveloperExceptionPage();
      app_.MapGrpcReflectionService();
    }

    app_.UseRouting();
    app_.MapGrpcService<ReRunnerAgent>();
    runningApp_ = app_.RunAsync();
  }

  /// <summary>
  ///   Disposes resources used by the application, stopping the application and waiting for it to finish.
  /// </summary>
  public void Dispose()
  {
    app_.StopAsync()
        .Wait();
    runningApp_.Wait();
    app_.DisposeAsync();
  }
}
