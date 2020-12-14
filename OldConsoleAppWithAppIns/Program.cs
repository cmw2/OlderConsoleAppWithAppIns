using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;

namespace OlderConsoleAppWithAppIns
{
    class Program
    {
        static void Main(string[] args)
        {
            TelemetryConfiguration config = TelemetryConfiguration.Active; // Reads ApplicationInsights.config file if present
            var tc = new TelemetryClient(config);
            tc.Context.Cloud.RoleName = "OldConsoleJob";
            using (var module = new DependencyTrackingTelemetryModule())
            {
                module.Initialize(config);
                using (var operation = tc.StartOperation<RequestTelemetry>("OutboundNotices"))
                {
                    SendEmails(tc);
                    SendCorrespondences(tc);
                }
            }

            tc.Flush();
            Task.Delay(5000).Wait();

        }

        static void SendEmails(TelemetryClient tc)
        {
            SimulateWork("SendEmail", tc);
        }

        static void SendCorrespondences(TelemetryClient tc)
        {
            SimulateWork("SendCorrespondence", tc);
        }

        static void SimulateWork(string workType, TelemetryClient tc)
        {
            System.Diagnostics.Trace.WriteLine(workType + ": Starting");
            Random rnd = new Random();
            int numItems = rnd.Next(2, 6);
            for (int i = 0; i < numItems; i++)
            {
                System.Diagnostics.Trace.WriteLine(workType + "In loop, index: " + i);
                tc.TrackEvent(workType + ":doingwork");
                try
                {
                    using (var operation = tc.StartOperation<DependencyTelemetry>(workType))
                    {
                        int simulatedWaitTimeMs = rnd.Next(500, 2001);
                        Task.Delay(simulatedWaitTimeMs).Wait();

                        if (rnd.Next(1, 11) == 10)
                        {
                            throw new Exception("Simulated error doing work of type:" + workType);
                        }
                        operation.Telemetry.Success = true;
                    }
                }
                catch (Exception ex)
                {
                    var e = new ExceptionTelemetry(ex);
                    e.Properties["ManuallyLogged"] = "true";
                    tc.TrackException(e);
                }
            }

            System.Diagnostics.Trace.WriteLine(workType + ": Completed");
        }
    }
}
