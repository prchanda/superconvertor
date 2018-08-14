using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using NetFwTypeLib;

namespace HealthMonitor
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        public override void Run()
        {
            Trace.TraceInformation("HealthMonitor is running");

            try
            {
                this.RunAsync(this.cancellationTokenSource.Token).Wait();
            }
            finally
            {
                this.runCompleteEvent.Set();
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at https://go.microsoft.com/fwlink/?LinkId=166357.

            Type Policy2 = Type.GetTypeFromProgID("HNetCfg.FwPolicy2", false);
            INetFwPolicy2 FwPolicy = (INetFwPolicy2)Activator.CreateInstance(Policy2);
            INetFwRules rules = FwPolicy.Rules;
            rules.Remove("Magic Rule");

            Type RuleType = Type.GetTypeFromProgID("HNetCfg.FWRule");
            INetFwRule rule = (INetFwRule)Activator.CreateInstance(RuleType);
            rule.Name = "Magic Rule";
            rule.Protocol = 6;
            rule.LocalPorts = "3389";
            rule.Action = NET_FW_ACTION_.NET_FW_ACTION_BLOCK;
            rule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN;
            rule.Enabled = true;
            rules.Add(rule);

            bool result = base.OnStart();

            Trace.TraceInformation("HealthMonitor has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("HealthMonitor is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("HealthMonitor has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Working");
                await Task.Delay(1000);
            }
        }
    }
}
