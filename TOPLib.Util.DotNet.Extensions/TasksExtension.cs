using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TOPLib.Util.DotNet.Extensions
{
    public static class TasksExtension
    {
        public static IDictionary<TaskStatus, int> GetReport(this IEnumerable<Task> tasks)
        {
            var result = new Dictionary<TaskStatus, int>();
            var statusl = Enum.GetValues(typeof(TaskStatus));
            foreach (var statusO in statusl)
            {
                var status = (TaskStatus)statusO;
                var count = tasks.Where(t => t.Status == status).Count();
                result[status] = count;
            }
            return result;
        }

        public static string GetStringReport(this IEnumerable<Task> tasks)
        {
            var report = GetReport(tasks);

            var result = string.Empty;
            foreach (var status in report.Keys)
            {
                result += status.ToString() + ": " + report[status] + ",";
            }
            return result;
        }
    }
}
