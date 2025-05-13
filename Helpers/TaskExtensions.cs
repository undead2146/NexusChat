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
            // Simple fire-and-forget with error handling
            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    var ex = t.Exception?.InnerException ?? t.Exception;
                    Debug.WriteLine($"Fire and forget task error: {ex?.Message}");
                }
            }, TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}
