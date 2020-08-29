using System;
using System.Collections.Concurrent;

namespace SoftwareCo
{
    class Scheduler
    {
        private readonly ConcurrentDictionary<Action, ScheduledTask> _scheduledTasks = new ConcurrentDictionary<Action, ScheduledTask>();

        public void Execute(Action action, int timeoutMs)
        {
            ScheduledTask task = new ScheduledTask(action, timeoutMs);
            task.TaskComplete += RemoveTask;
            _scheduledTasks.TryAdd(action, task);
            task.Timer.Start();
        }

        public void CancelAll()
        {
            foreach (ScheduledTask task in _scheduledTasks.Values)
            {
                DisposeTask(task);
            }
        }

        private void RemoveTask(object sender, EventArgs e)
        {
            ScheduledTask task = (ScheduledTask)sender;
            DisposeTask(task);
        }

        private void DisposeTask(ScheduledTask task)
        {
            task.TaskComplete -= RemoveTask;
            ScheduledTask deleted;
            _scheduledTasks.TryRemove(task.Action, out deleted);
        }
    }
}
