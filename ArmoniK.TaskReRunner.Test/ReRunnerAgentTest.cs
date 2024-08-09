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

using System.Text;

using ArmoniK.Api.gRPC.V1;
using ArmoniK.Api.gRPC.V1.Agent;
using ArmoniK.Api.gRPC.V1.Worker;
using ArmoniK.TaskReRunner.Storage;

using Google.Protobuf;

using Grpc.Core;

namespace ArmoniK.TaskReRunner.Test;

public class request : ServerCallContext
{
  protected override string            MethodCore            { get; }
  protected override string            HostCore              { get; }
  protected override string            PeerCore              { get; }
  protected override DateTime          DeadlineCore          { get; }
  protected override Metadata          RequestHeadersCore    { get; }
  protected override CancellationToken CancellationTokenCore { get; }
  protected override Metadata          ResponseTrailersCore  { get; }
  protected override Status            StatusCore            { get; set; }
  protected override WriteOptions?     WriteOptionsCore      { get; set; }
  protected override AuthContext       AuthContextCore       { get; }

  protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders)
    => throw new NotImplementedException();

  protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options)
    => throw new NotImplementedException();
}

public class ReRunnerAgentTest
{
  [SetUp]
  public void Setup()
  {
  }

  [Test]
  public void AgentStorageShouldSucceed()
  {
    var _storage = new AgentStorage();

    var result = _storage.Results.IsEmpty && _storage.Tasks.IsEmpty;

    Assert.AreEqual(_storage.Results.IsEmpty,
                    _storage.Tasks.IsEmpty);
  }

  [Test]
  public void AgentCreateResultShouldSucceed()
  {
    var _storage = new AgentStorage();
    var _agent = new ReRunnerAgent(_storage,
                                   null);
    _agent.CreateResults(new CreateResultsRequest
                         {
                           CommunicationToken = "CommunicationToken",
                           SessionId          = "SessionId",
                           Results =
                           {
                             Enumerable.Range(1,
                                              3)
                                       .Select(i => new CreateResultsRequest.Types.ResultCreate
                                                    {
                                                      Data = UnsafeByteOperations.UnsafeWrap(Encoding.ASCII.GetBytes("true")),
                                                      Name = "Payload_" + i,
                                                    }),
                           },
                         },
                         new request());

    Assert.AreEqual(_storage.Results.Values.Count(),
                    3);
  }

  [Test]
  public void AgentCreateResultMetaDataShouldSucceed()
  {
    var _storage = new AgentStorage();
    var _agent = new ReRunnerAgent(_storage,
                                   null);
    _agent.CreateResultsMetaData(new CreateResultsMetaDataRequest
                                 {
                                   SessionId          = "sessionId",
                                   CommunicationToken = "CommunicationToken",
                                   Results =
                                   {
                                     Enumerable.Range(1,
                                                      3)
                                               .Select(i => new CreateResultsMetaDataRequest.Types.ResultCreate
                                                            {
                                                              Name = "Payload_" + i,
                                                            }),
                                   },
                                 },
                                 new request());


    Assert.AreEqual(_storage.Results.Values.Count(),
                    3);
  }

  [Test]
  public void AgentNotifyResultDataShouldSucceed()
  {
    var _storage = new AgentStorage();
    var _agent = new ReRunnerAgent(_storage,
                                   null);
    _agent.NotifyResultData(new NotifyResultDataRequest
                            {
                              CommunicationToken = "CommunicationToken",

                              Ids =
                              {
                                Enumerable.Range(1,
                                                 3)
                                          .Select(i => new NotifyResultDataRequest.Types.ResultIdentifier
                                                       {
                                                         ResultId  = "r" + i,
                                                         SessionId = "sessionId",
                                                       }),
                              },
                            },
                            new request());
    var result = _storage.NotifiedResults.Contains("r1") && _storage.NotifiedResults.Contains("r2") && _storage.NotifiedResults.Contains("r3");
    Assert.That(result,
                Is.True);
  }


  [Test]
  public void AgentSubmitTaskShouldSucceed()
  {
    var _storage = new AgentStorage();
    var _agent = new ReRunnerAgent(_storage,
                                   null);
    _agent.SubmitTasks(new SubmitTasksRequest
                       {
                         CommunicationToken = "CommunicationToken",
                         SessionId          = "SessionId",
                         TaskOptions        = new TaskOptions(),
                         TaskCreations =
                         {
                           Enumerable.Range(1,
                                            3)
                                     .Select(i => new SubmitTasksRequest.Types.TaskCreation
                                                  {
                                                    PayloadId   = "Payload_" + i,
                                                    TaskOptions = new TaskOptions(),
                                                  }),
                         },
                       },
                       new request());

    Assert.AreEqual(_storage.Tasks.Values.Count(),
                    3);
  }


  [Test]
  public void DeserializeShouldSucceed()
  {
    var processRequest = new ProcessRequest
                         {
                           CommunicationToken = "CommunicationToken",
                           SessionId          = "SessionId",
                           TaskOptions = new TaskOptions
                                         {
                                           Options =
                                           {
                                             {
                                               "Use", "Case"
                                             },
                                           },
                                           ApplicationName = "ApplicationName",
                                         },
                           PayloadId  = "PayloadId",
                           DataFolder = "DataFolder",
                           DataDependencies =
                           {
                             "DataDependencies",
                           },
                           ExpectedOutputKeys =
                           {
                             "ExpectedOutput",
                           },
                           Configuration = new Configuration
                                           {
                                             DataChunkMaxSize = 84000,
                                           },
                           TaskId = "TaskId",
                         };

    var JSONresult = processRequest.ToString();

    using (var tw = new StreamWriter("Data.json",
                                     false))
    {
      tw.WriteLine(JSONresult);
    }

    var serialized = ProcessRequest.Parser.ParseJson(File.ReadAllText("Data.json"));
    Assert.AreEqual(processRequest,
                    serialized);
    Assert.AreEqual(serialized.TaskOptions.Options["Use"],
                    "Case");
  }
}
