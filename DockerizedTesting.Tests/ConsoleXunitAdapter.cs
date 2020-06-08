using System;
using System.IO;
using System.Text;
using Xunit.Abstractions;

namespace DockerizedTesting.Tests.Containers
{
    public class ConsoleXunitAdapter : TextWriter
    {
        readonly ITestOutputHelper output;
        public ConsoleXunitAdapter(ITestOutputHelper output)
        {
            this.output = output;
        }
        public override Encoding Encoding => Encoding.UTF8;
        public override void WriteLine(string message)
        {
            try
            {
                this.output.WriteLine(message);
            }
            catch (InvalidOperationException) { } // test finished
            System.Diagnostics.Debug.WriteLine(message);
        }
        public override void WriteLine(string format, params object[] args)
        {
            try
            {
                this.output.WriteLine(format, args);
            }
            catch (InvalidOperationException) { } // test finished
            System.Diagnostics.Debug.WriteLine(format, args);
        }
    }
}