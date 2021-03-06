﻿using System;
using System.Collections.Generic;
using System.Linq;
using cafe.CommandLine.LocalSystem;
using cafe.Shared;
using NLog;
using NodaTime;

namespace cafe.Server.Jobs
{
    public class JobRunner : IDisposable
    {
        private readonly IDisposable _timer;
        private readonly IActionExecutor _actionExecutor;
        private static readonly Logger Logger = LogManager.GetLogger(typeof(JobRunner).FullName);

        private readonly Queue<IJobRun> _queuedRuns = new Queue<IJobRun>();
        private readonly IList<JobRunStatus> _finishedTasks = new List<JobRunStatus>();
        private readonly object _processLocker = new object();

        public JobRunner(ITimerFactory timerFactory, IActionExecutor actionExecutor)
        {
            _timer = timerFactory.ExecuteActionOnInterval(ProcessQueue, Duration.FromSeconds(15));
            _actionExecutor = actionExecutor;
        }

        public void ProcessQueue()
        {
            Logger.Debug("Processing tasks for scheduler");
            lock (_processLocker) // since queues are being manipulated here, don't let this happen multiple times
            {
                if (IsPaused)
                {
                    Logger.Debug("Since this runner is paused, jobs will not be processed");
                    return;
                }
                if (_queuedRuns.Count == 0)
                {
                    Logger.Debug("There is nothing to do right now");
                    return;
                }
                var readyJobRun = _queuedRuns.Peek();
                if (readyJobRun.IsFinishedRunning)
                {
                    Logger.Info($"Task {readyJobRun} has finished running, so removing it from the queue");
                    _queuedRuns.Dequeue();
                    _finishedTasks.Add(readyJobRun.ToStatus());
                }
                else if (!readyJobRun.IsRunning)
                {
                    Logger.Info(
                        $"Task {readyJobRun} is not yet run and it is the next thing to run, so running it");
                    _actionExecutor.Execute(readyJobRun.Run);
                }
                else
                {
                    Logger.Debug($"Since {readyJobRun} is still running, waiting for it to complete");
                }
            }
            Logger.Debug("Finished processing tasks for scheduler");
        }

        public void Enqueue(IJobRun jobRun)
        {
            _queuedRuns.Enqueue(jobRun);
            ProcessQueue();
        }

        public JobRunStatus FindStatusById(Guid id, int previousIndex)
        {
            Logger.Debug(
                $"Searching for task {id} within {_queuedRuns.Count} queued tasks and {_finishedTasks.Count} finished tasks");
            var task = _queuedRuns.FirstOrDefault(t => t.Id == id);
            if (task != null)
            {
                var queuedStatus = task.ToStatus(previousIndex);
                Logger.Debug($"Found active status {queuedStatus} for id {id}");
                return queuedStatus;
            }
            var status = _finishedTasks.FirstOrDefault(t => t.Id == id);
            if (status != null)
            {
                Logger.Debug($"Found finished status {status} for id {id}");
            }
            else
            {
                Logger.Debug($"No status for id {id} found");
            }
            return status;
        }

        public JobRunnerStatus ToStatus()
        {
            return new JobRunnerStatus()
            {
                QueuedTasks = _queuedRuns.Select(queuedRun => queuedRun.ToStatus()).ToArray(),
                FinishedTasks = _finishedTasks.ToArray()
            };
        }

        public void Dispose()
        {
            _timer.Dispose();
        }

        public void Pause()
        {
            Logger.Info("Pausing processing jobs");
            IsPaused = true;
        }

        public void Resume()
        {
            Logger.Info("Resuming processing jobs");
            IsPaused = false;
            ProcessQueue();
        }
        
        public bool IsPaused { get; private set; }
    }
}