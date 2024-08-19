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

using ArmoniK.Api.gRPC.V1.Worker;

using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace TaskReRunnerIntegrationTest;

[TestFixture]
public class TaskReRunnerIntegration
{
  [SetUp]
  public async void Setup()
  {
    var simpleJson = new ProcessRequest
                     {
                       PayloadId = "PayloadId1",
                       ExpectedOutputKeys =
                       {
                         "ExpectedOutput1",
                       },
                     };
    var JSONresult = simpleJson.ToString();

    using (var tw = new StreamWriter("Data.json",
                                     false))
    {
      await tw.WriteLineAsync(JSONresult);
    }

    await File.WriteAllBytesAsync(Path.Combine("/tmp/",
                                               simpleJson.PayloadId),
                                  Encoding.ASCII.GetBytes("Payload"));
  }


  [TestCase(null)]
  [TestCase("--dataFolder",
            "/tmp/")]
  public void ArgsValidity(params string[]? args)
  {
    Program.Main(args);
    Assert.Pass();
  }
}
