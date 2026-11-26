using System;
using System.Threading;

namespace God.Runner
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            using (var cts = new CancellationTokenSource())
            {
                Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };
                return Cli.Run(args, Console.Out, Console.Error, cts.Token);
            }
        }
    }
}
