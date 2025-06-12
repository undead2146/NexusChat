using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace NexusChat.Helpers
{
    /// <summary>
    /// Extension methods for Task objects
    /// </summary>
    public static class TaskExtensions
    {
        /// <summary>
        /// Executes a task without waiting for it to complete, but with error handling
        /// </summary>
        /// <param name="task">The task to execute</param>
        public static void FireAndForget(this Task task)
        {
            if (task == null) return;
            
            Task.Run(async () =>
            {
                try
                {
                    await task;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"FireAndForget task failed: {ex.Message}");
                }
            });
        }
    }
}
