using Pachyderm_Acoustic.UI;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Pachyderm_Acoustic.Utilities
{
    public partial class RCPachTools
    {
        public static async Task<Simulation_Type> RunSimulation(Simulation_Type Sim, bool needfeedback = true, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Sim == null) return null;
            Sim.Cancellation = cancellationToken;
            using (System.Diagnostics.Process process = System.Diagnostics.Process.GetCurrentProcess())
            {
                var priority = process.PriorityClass;
                bool autoSave = Rhino.ApplicationSettings.FileSettings.AutoSaveEnabled;
                try
                {
                    switch (PachydermAc_PlugIn.Instance.TaskPriority)
                    {
                        case 0: process.PriorityClass = System.Diagnostics.ProcessPriorityClass.High; break;
                        case 1: process.PriorityClass = System.Diagnostics.ProcessPriorityClass.AboveNormal; break;
                        default: process.PriorityClass = System.Diagnostics.ProcessPriorityClass.Normal; break;
                    }
                    Rhino.ApplicationSettings.FileSettings.AutoSaveEnabled = false;
                    Sim.Begin();
                    Rhino.RhinoApp.SetCommandPrompt(string.Format("Initiating {0} Calculation...", Sim.Sim_Type()));
                    // Let all workers observe cancellation and stop before discarding their buffers.
                    // Spec D7: check the state protocol first; completed work pays no delay, still-running
                    // work polls at the existing 3,000 ms interval.
                    while (Sim.ThreadState() != System.Threading.ThreadState.Stopped)
                    {
                        await Task.Delay(3000);
                        if (needfeedback) Rhino.RhinoApp.SetCommandPrompt(Sim.ProgressMsg());
                    }
                    cancellationToken.ThrowIfCancellationRequested();
                    Sim.Combine_ThreadLocal_Results();
                    while (Sim.ThreadState() == System.Threading.ThreadState.Running)
                    {
                        await Task.Delay(3000);
                        if (needfeedback) Rhino.RhinoApp.SetCommandPrompt(Sim.ProgressMsg());
                    }
                    cancellationToken.ThrowIfCancellationRequested();
                    return Sim;
                }
                finally
                {
                    Rhino.ApplicationSettings.FileSettings.AutoSaveEnabled = autoSave;
                    process.PriorityClass = priority;
                }
            }
        }
    }
}
