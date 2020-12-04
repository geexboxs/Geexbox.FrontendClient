using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Geexbox.FrontendClient.Extensions
{
    internal static class TaskTimeoutExtensions
    {
        public static async Task WithTimeout(this Task task, TimeSpan timeoutDelay, string message)
        {
            object obj = (object)task;
            Task[] taskArray = new Task[2]
            {
                task,
                Task.Delay(timeoutDelay)
            };
            obj = obj == await Task.WhenAny(taskArray) ? (object)null : throw new TimeoutException(message);
            task.Wait();
        }

        public static async Task<T> WithTimeout<T>(
            this Task<T> task,
            TimeSpan timeoutDelay,
            string message)
        {
            object obj = (object)task;
            Task[] taskArray = new Task[2]
            {
                (Task) task,
                Task.Delay(timeoutDelay)
            };
            obj = obj == await Task.WhenAny(taskArray) ? (object)null : throw new TimeoutException(message);
            return task.Result;
        }
    }

}
