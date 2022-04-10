using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using LogGrokCore.Data.Index;

namespace LogGrokCore.Data.Search;

public static class Search
{
    private const double Throttle = 0.01;
       
    public class Progress
    {
        private readonly TaskCompletionSource _progressCompletionSource = new();
     
        public double Value
        {
            get => _value;
            set
            {
                _value = value;
                if (value - _lastReportedValue > Throttle)
                    ReportNewValue(value);
            }
        }

        public bool IsFinished
        {
            get => _isFinished;
            set
            {
                
                _isFinished = value;
                _progressCompletionSource.SetResult();
            }
        }

        private void ReportNewValue(double value)
        {
            _lastReportedValue = value;
        }

        public Task Completion => _progressCompletionSource.Task;

        private double _value;
        private double _lastReportedValue;
        private bool _isFinished;
    }

    public static (Progress, Indexer, SearchLineIndex) CreateSearchIndex(
        LogModelFacade logModelFacade,
        Regex regex,
        CancellationToken cancellationToken)
    {
        var sourceLineIndex = logModelFacade.LineIndex;
        SearchLineIndex lineIndex = new(sourceLineIndex); // searchResultLineNumber -> originalLogLineNumber mapping
        var searchIndexer = new Indexer();                // components -> searchResultLineNumber
        var progress = new Progress();

        var pipeline = new Pipeline(regex, logModelFacade);
        Task.Run(async () =>
        {
            try
            {
                await pipeline.StartSearch(searchIndexer, lineIndex, progress,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Trace.TraceInformation($"Search cancelled ({regex})");
            }
            finally
            {
                searchIndexer.Finish();
                progress.IsFinished = true;
                Trace.TraceInformation($"Search finished ({regex})");
            }
        }, cancellationToken);
            
        return (progress, searchIndexer, lineIndex);
    }
}