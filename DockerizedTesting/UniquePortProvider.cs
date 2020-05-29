using DockerizedTesting.Containers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DockerizedTesting
{
    /// <summary>
    /// VS Test seems to run tests differently from Resharper. This results in tests trying to use the same
    /// Port when "Run tests in parallel" is set in test explorer.
    /// This class attempts to ensure that different test projects use different ports.
    /// We should avoid assigning random ports because that generates lots of different containers over time.
    /// </summary>
    public sealed class UniquePortProvider
    {
        public UniquePortProvider(IContainerActions containerActions)
        {
            this.containerActions = containerActions;
        }

        const string uniqueId = "ED7B6E04-F820-428B-BC37-ED335BF19506";
        private readonly IContainerActions containerActions;
        TimeSpan timeout = TimeSpan.FromSeconds(10);

        public int GetPort()
        {
            bool unknownState = false;

            using (var interProcPortLock = new Mutex(false, uniqueId, out bool firstToAccess))
            {
                bool gotLock = false;
                try
                {
                    try
                    {
                        gotLock = interProcPortLock.WaitOne(timeout, false);
                    }
                    catch (AbandonedMutexException)
                    {
                        gotLock = true;
                        unknownState = true;
                    }
                    string path = Path.Combine(Path.GetTempPath(), $"{uniqueId}.tmp");
                    int freePort = this.getFreePort(firstToAccess, path, containerActions, ref unknownState);

                    return freePort;
                }
                finally
                {
                    if (gotLock)
                    {
                        interProcPortLock.ReleaseMutex();
                    }
                }
            }
        }

        private int[] recycleUnusedPorts(string path)
        {
            if (!File.Exists(path))
            {
                return new int[0];
            }

            List<int> portsRemoved = new List<int>();
            using (var fs = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                string json;
                using (var sr = new StreamReader(fs))
                {
                    json = sr.ReadToEnd();

                    // Using a pid as an identifier isn't amazing because they get reused eventually (or after reboot)
                    // If necessary we could use pid_starttime
                    var pids = Process.GetProcesses().Select(p => p.Id);
                    var portToPid = JsonConvert.DeserializeObject<Dictionary<int, int>>(json);
                    var ports = portToPid.Keys.ToArray();
                    foreach (var port in ports)
                    {
                        int pid = portToPid[port];
                        if (!pids.Contains(pid))
                        {
                            portsRemoved.Add(port);
                            portToPid.Remove(port);
                        }
                    }

                    //todo: if (portsRemoved.Any())
                    {
                        json = JsonConvert.SerializeObject(portToPid);
                        fs.Position = 0;
                        using (var sw = new StreamWriter(fs))
                        {
                            sw.Write(json);
                            sw.Flush();
                            fs.SetLength(fs.Position);
                        }
                    }
                }
            }
            return portsRemoved.ToArray();
        }

        private int getFreePort(bool firstToAccess, string path, IContainerActions containerActions, ref bool unknownState)
        {
            int portToUse;
            portToUse = Utils.MinPort;

            try
            {
                if (!unknownState && File.Exists(path) && firstToAccess)
                {
                    try
                    {
                        var portsRemoved = this.recycleUnusedPorts(path);
                        if (portsRemoved.Any(p => !portAvailable(p)))
                        {
                            // We can't wait too long cos we are in the middle of a mutex. If we timeout
                            // and next time it attempts to use these ports then it will just fail and skip them
                            Task.WaitAny(containerActions.KillZombieContainersBoundToPorts(portsRemoved),
                                Task.Delay((int)timeout.TotalMilliseconds / 2));
                        }
                    }
                    catch (IOException) { }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                unknownState = true;
            }

            if (unknownState)
            {
                // Something has gone wrong - pick a diff range of ports far away from the others without reserving
                do
                {
                    portToUse += new Random().Next(1, 100) + 100;
                } while (!portAvailable(portToUse));
                return portToUse;
            }

            do
            {
                try
                {
                    portToUse = this.reserveLowestFreePort(path);
                }
                catch (IOException) { }

                // If portAvailable is false then this can result in us reserving multiple ports.
                // This is fine thos because there is something using them so they should be excluded.
            } while (!portAvailable(portToUse));

            return portToUse;
        }

        private bool portAvailable(int port, bool udp = false)
        {
            //TODO: this will need to be extended to cope with diff docker nets, ips, protocol etc.
            bool inUse = false;

            IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] ipEndPoints = udp
                ? ipProperties.GetActiveUdpListeners()
                : ipProperties.GetActiveTcpListeners();

            foreach (IPEndPoint endPoint in ipEndPoints)
            {
                if (endPoint.Port == port)
                {
                    inUse = true;
                    break;
                }
            }

            return !inUse;
        }

        private int reserveLowestFreePort(string path)
        {
            int port;
            using (var fs = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                var portToPid = new Dictionary<int, int>();
                if (fs.Length > 0)
                {
                    using (var sr = new StreamReader(fs, Encoding.UTF8, true, 1024, leaveOpen: true))
                    {
                        portToPid = JsonConvert.DeserializeObject<Dictionary<int, int>>(sr.ReadToEnd());
                    }
                    fs.Position = 0;
                }

                port = Utils.MinPort;
                while (portToPid.ContainsKey(port))
                {
                    port++;
                }
                portToPid.Add(port, Process.GetCurrentProcess().Id);

                string json = JsonConvert.SerializeObject(portToPid);
                using (var sw = new StreamWriter(fs))
                {
                    sw.Write(json);
                    sw.Flush();
                    fs.SetLength(fs.Position);
                }
            }
            return port;
        }
    }
}
