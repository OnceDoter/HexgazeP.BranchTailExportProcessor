using System.Diagnostics;
using System.Reflection;
using OpenTelemetry.Exporter;
using OpenTelemetry.Trace;

namespace OpenTelemetry.Processor;

/// <summary>Extension methods for working with <see cref="Activity"/>.</summary>
public static class ActivityExtensionMethods
{
    private const BindingFlags PrivateFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.IgnoreCase;
    
    /// <summary>Sets the activity tag: trace.</summary>
    /// <param name="activity">Activity.</param>
    /// <remarks>The <see cref="BranchTailExportProcessor"/> processor will send the full <see cref="ActivityTraceId"/> if any of its activities are marked "trace".</remarks>
    /// <returns>Activity with tag: trace.</returns>
    public static Activity SetTraceTag(this Activity activity)
    {
        // Рюкзак вместо енумератора, чтобы не искать по родительским и дочерним активностям.
        Baggage.Current = Baggage.Current.SetBaggage(DiagnosticConstants.Trace, bool.TrueString);

        return activity;
    }
    
    /// <summary>
    /// Add <see cref="BranchTailExportProcessor"/> processor.
    /// </summary>
    /// <param name="builder">The <see cref="TracerProviderBuilder"/>.</param>
    /// <param name="sampler">The <see cref="Sampler"/>.</param>
    /// <param name="configure">Configure action.</param>
    /// <returns>The <see cref="TracerProviderBuilder"/>.</returns>
    public static TracerProviderBuilder BranchTailExporter(this TracerProviderBuilder builder, Sampler? sampler = null, Action<OtlpExporterOptions>? configure = null)
    {
        OtlpExporterOptions? options = null;
        builder.AddOtlpExporter(x => ConfigureBind(x, out options, configure));
        var processors = (List<BaseProcessor<Activity>>)typeof(TracerProviderBuilderBase).GetField("processors", PrivateFlags)!.GetValue(builder)!;
        var processor = processors.OfType<BatchActivityExportProcessor>().First();
        var configuredExporter = (BaseExporter<Activity>)typeof(BaseExportProcessor<Activity>).GetField("exporter", PrivateFlags)!.GetValue(processor)!;
        processors.Remove(processor);
        builder.AddProcessor(new BranchTailExportProcessor(configuredExporter, options, sampler));
        return builder;
    }
    
    
    private static void ConfigureBind(OtlpExporterOptions src, out OtlpExporterOptions? options, Action<OtlpExporterOptions>? configure = null)
    {
        configure?.Invoke(src);
        options = src;
    }
}