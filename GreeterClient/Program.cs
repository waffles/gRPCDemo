// Copyright 2015 gRPC authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
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
using Grpc.Core;
using Helloworld;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace GreeterClient
{
  class Program
  {
    public static void Main(string[] args)
    {
      Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);

      var client = new Greeter.GreeterClient(channel);

      String user = "you";
      var request = new HelloRequest { Name = user };
      var reply = client.SayHello(request);


      //var factResult = client.Factorial(new NumberRequset { Number = 4 });

      var factArrayResult = client.FactorialSequence(new NumberRequset { Number = 4 });

      using (var call = client.RPNCalc())
      {
        var responseReaderTask = Task.Run(async () =>
        {
          while (await call.ResponseStream.MoveNext())
          {
            Console.WriteLine($"Result: {call.ResponseStream.Current.Number}");
          }
        });

        var requsetWriterTask = Task.Run(async () =>
        {
          await call.RequestStream.WriteAsync(new CalculatorRequest { Operation = CalculatorRequest.Types.Operation.Push, Value = 7 });
          await call.RequestStream.WriteAsync(new CalculatorRequest { Operation = CalculatorRequest.Types.Operation.Push, Value = 3 });
          await call.RequestStream.WriteAsync(new CalculatorRequest { Operation = CalculatorRequest.Types.Operation.Push, Value = 3 });
          await call.RequestStream.WriteAsync(new CalculatorRequest { Operation = CalculatorRequest.Types.Operation.Add });
          await call.RequestStream.WriteAsync(new CalculatorRequest { Operation = CalculatorRequest.Types.Operation.Multiply });
        });

        requsetWriterTask.Wait();
        responseReaderTask.Wait();
      }

      channel.ShutdownAsync().Wait();
      Console.WriteLine("Press any key to exit...");
      Console.ReadKey();
    }
  }
}
