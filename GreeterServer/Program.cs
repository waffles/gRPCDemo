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
using System.Threading.Tasks;
using Grpc.Core;
using Helloworld;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using Google.Protobuf.Collections;
using System.Collections.Concurrent;

namespace GreeterServer
{
  class GreeterImpl : Greeter.GreeterBase
  {
    // Server side handler of the SayHello RPC
    public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
    {
      return Task.FromResult(new HelloReply { Message = "Hello " + request.Name });
    }


    //public override Task<NumberResponce> Factorial(NumberRequset request, ServerCallContext context)
    //{
    //  return Task.FromResult(new NumberResponce { Number = ComputeFactorial.Compute(request.Number).Last() });
    //}

    public override Task<NumberArrayResponce> FactorialSequence(NumberRequset request, ServerCallContext context)
    {
      var result = new NumberArrayResponce();
      result.Numbers.AddRange(ComputeFactorial.Compute(request.Number));

      return Task.FromResult(result);
    }

    public static ConcurrentStack<long> stack = new ConcurrentStack<long>();
    public override async Task RPNCalc(IAsyncStreamReader<CalculatorRequest> requestStream, IServerStreamWriter<NumberResponce> responseStream, ServerCallContext context)
    {
      while (await requestStream.MoveNext())
      {
        if (requestStream.Current.Operation == CalculatorRequest.Types.Operation.Push)
        {
          stack.Push(requestStream.Current.Value);
          continue;
        }

        long last1;
        long last2;
        long result = 0;
        stack.TryPop(out last1);
        stack.TryPop(out last2);

        switch (requestStream.Current.Operation)
        {

          case CalculatorRequest.Types.Operation.Add:

            result = (last1 + last2);
            break;
          case CalculatorRequest.Types.Operation.Subtract:
            result = (last1 - last2);
            break;
          case CalculatorRequest.Types.Operation.Multiply:
            result = (last1 * last2);
            break;
          case CalculatorRequest.Types.Operation.Davide:
            result = (last1 / last2);
            break;
        }
        stack.Push(result);
        await responseStream.WriteAsync(new NumberResponce { Number = result });
      }
    }


    public static class ComputeFactorial
    {
      public static List<long> Compute(long i)
      {
        var list = new List<long>();
        Fact(i, ref list);
        return list;

      }

      private static long Fact(long i, ref List<long> list)
      {
        if (i == 1)
        {
          list.Add(i);
          return 1;
        }
        long result;
        result = Fact(i - 1, ref list) * i;
        list.Add(result);
        return result;
      }
    }

    class Program
    {
      const int Port = 50051;

      public static void Main(string[] args)
      {
        Server server = new Server
        {
          Services = { Greeter.BindService(new GreeterImpl()) },
          Ports = { new ServerPort("localhost", Port, ServerCredentials.Insecure) }
        };

        server.Start();

        Console.WriteLine("Greeter server listening on port " + Port);
        Console.WriteLine("Press any key to stop the server...");
        Console.ReadKey();

        server.ShutdownAsync().Wait();
      }
    }

  }
}
