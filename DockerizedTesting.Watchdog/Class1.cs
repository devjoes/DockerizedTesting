using System;
using System.Reflection;
using System.Runtime.Loader;

namespace DockerizedTesting.Watchdog
{
    public class Class1
    {
        public void Test()
        {
            var asm = Assembly.GetAssembly(typeof(DockerizedTesting.Watchdog.Class1));
            AssemblyLoadContext.GetLoadContext(asm).LoadFromAssemblyName(AssemblyName.GetAssemblyName(asm.Location));
        }
    }
}
