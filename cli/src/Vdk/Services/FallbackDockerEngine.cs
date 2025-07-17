using System;
using System.Collections.Generic;
using System.Diagnostics;
using Vdk.Models;

namespace Vdk.Services
{
    public class FallbackDockerEngine : IDockerEngine
    {
        internal static bool RunProcess(string fileName, string arguments, out string stdOut, out string stdErr)
        {
            using var process = new Process();
            process.StartInfo.FileName = fileName;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            stdOut = process.StandardOutput.ReadToEnd();
            stdErr = process.StandardError.ReadToEnd();
            process.WaitForExit();
            return process.ExitCode == 0;
        }

        public bool Run(string image, string name, PortMapping[]? ports, Dictionary<string, string>? envs, FileMapping[]? volumes, string[]? commands, string? network = null)
        {
            var args = $"run -d --name {name}";
            if (network != null)
                args += $" --network {network}";
            if (ports != null)
            {
                foreach (var p in ports)
                {
                    var proto = string.IsNullOrWhiteSpace(p.Protocol) ? "" : "/" + p.Protocol.ToLower();
                    var listen = string.IsNullOrWhiteSpace(p.ListenAddress) ? "" : p.ListenAddress + ":";
                    args += $" -p {listen}{p.HostPort}:{p.ContainerPort}{proto}";
                }
            }
            if (envs != null)
            {
                foreach (var kv in envs)
                    args += $" -e \"{kv.Key}={kv.Value}\"";
            }
            if (volumes != null)
            {
                foreach (var v in volumes)
                    args += $" -v \"{v.Source}:{v.Destination}\"";
            }
            args += $" {image}";
            if (commands != null && commands.Length > 0)
                args += " " + string.Join(" ", commands);
            return RunProcess("docker", args, out _, out _);
        }

        public bool Exists(string name, bool checkRunning = true)
        {
            var filter = checkRunning ? "--filter \"status=running\"" : "";
            var args = $"ps -a {filter} --filter \"name=^{name}$\" --format \"{{{{.Names}}}}\"";
            var ok = RunProcess("docker", args, out var output, out _);
            if (!ok) return false;
            foreach (var line in output.Split('\n'))
                if (line.Trim() == name) return true;
            return false;
        }

        public bool Delete(string name)
        {
            // Try to remove forcefully (stops if running)
            return RunProcess("docker", $"rm -f {name}", out _, out _);
        }

        public bool Stop(string name)
        {
            return RunProcess("docker", $"stop {name}", out _, out _);
        }

        public bool Exec(string name, string[] commands)
        {
            var args = $"exec {name} ";
            args += string.Join(" ", commands);
            return RunProcess("docker", args, out _, out _);
        }

        public bool CanConnect()
        {
            var args = $"ps"; 
            return RunProcess("docker", args, out _, out _);
        }
    }
}
