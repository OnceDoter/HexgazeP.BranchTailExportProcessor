using System.Diagnostics;
using OpenTelemetry.Exporter;
using OpenTelemetry.Trace;

namespace OpenTelemetry.Processor;

internal sealed class BranchTailExportProcessor : BatchActivityExportProcessor
{
    private readonly Sampler _sampler;

    public BranchTailExportProcessor(
        BaseExporter<Activity> exporter,
        OtlpExporterOptions options,
        Sampler sampler)
        : base(exporter,
            options.BatchExportProcessorOptions.MaxQueueSize,
            options.BatchExportProcessorOptions.ScheduledDelayMilliseconds,
            options.BatchExportProcessorOptions.ExporterTimeoutMilliseconds,
            options.BatchExportProcessorOptions.MaxExportBatchSize)
    {
        this._sampler = sampler;
    }

    /// <remarks>
    /// The method is called when the activity ends:
    /// i.e. activities will be completed in the reverse order from their discovery;
    /// means to decide whether we can send the entire trace or part of it to the child activity itself.
    /// If you decide, put the "Trace" tag on the root and check all subsequent ones against it.
    /// </remarks>
    protected override void OnExport(Activity data)
    {
        var mark = Baggage.Current.GetBaggage(DiagnosticConstants.Trace) != null
                   || data.Status == ActivityStatusCode.Error;

        if (mark || _sampler.ShouldSample(new SamplingParameters(data.Parent?.Context ?? default, data.TraceId, data.DisplayName, data.Kind)).Decision != SamplingDecision.Drop)
        {
            data.SetTraceTag();
        }

        base.OnExport(data);
    }
}