﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AspectCore.APM.Common;
using AspectCore.APM.Profiler;

namespace AspectCore.APM.ApplicationProfiler
{
    public class ApplicationProfilerSetup : IProfilerSetup
    {
        const int _defaultInterval = 5;

        private readonly IEnumerable<IProfiler<ApplicationGCProfilingContext>> _gcProfilers;
        private readonly IEnumerable<IProfiler<ApplicationThreadingProfilingContext>> _threadingprofilers;
        private readonly ApplicationProfilingOptions _profilingOptions;
        private readonly IInternalLogger _logger;
        private readonly int _interval;
        private Timer _callbackTimer;

        public ApplicationProfilerSetup(
            IEnumerable<IProfiler<ApplicationGCProfilingContext>> gcProfilers,
            IEnumerable<IProfiler<ApplicationThreadingProfilingContext>> threadingprofilers,
            IOptionAccessor<ApplicationProfilingOptions> optionAccessor, IInternalLogger logger = null)
        {
            _profilingOptions = optionAccessor.Value;
            _logger = logger;
            _gcProfilers = gcProfilers ?? throw new ArgumentNullException(nameof(gcProfilers));
            _threadingprofilers = threadingprofilers ?? throw new ArgumentNullException(nameof(threadingprofilers));
            _interval = _profilingOptions.Interval.HasValue && _profilingOptions.Interval.Value > 0 ? _profilingOptions.Interval.Value : _defaultInterval;
        }

        public bool Start()
        {
            _callbackTimer = new Timer(async state => await Callback(state), null, TimeSpan.FromSeconds(_interval), TimeSpan.FromSeconds(_interval));
            _logger?.LogInformation("Start ApplicationProfiler.");
            return true;
        }

        private async Task Callback(object state)
        {
            foreach (var profiler in _gcProfilers)
                await profiler.Invoke(ApplicationGCProfilingContext.GetSnapshot());
            foreach (var profiler in _threadingprofilers)
                await profiler.Invoke(ApplicationThreadingProfilingContext.GetSnapshot());
        }

        public void Stop()
        {
            _logger?.LogInformation("Stop ApplicationProfiler.");
        }
    }
}