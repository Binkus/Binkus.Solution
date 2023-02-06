using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Binkus;

// IConfig config = new Conf();

// Type type;
// type = typeof(BenchmarkServiceResolutionBasicToComplex);
// type = typeof(BenchmarkServiceResolutionBasic);
// type = typeof(BenchmarkServiceResolutionComplex);
// type = typeof(BenchmarkGeneralIocContainerTests);
// type = typeof(BenchmarkConcurrentDictionaryCreation);
// type = typeof(BenchmarkCreateScope);
// type = typeof(BenchmarkCreateAndDisposeScopeAsync);
// type = typeof(BenchmarkGetHashCode);
// type = typeof(BenchmarkConcurrentDictionaryLookup);
// // type = typeof();
// // BenchmarkRunner.Run(type);

BenchmarkRunner.Run<BenchmarkServiceResolutionBasicToComplex>();
// BenchmarkRunner.Run<BenchmarkServiceResolutionBasic>();
// BenchmarkRunner.Run<BenchmarkServiceResolutionComplex>();
// BenchmarkRunner.Run<BenchmarkGeneralIocContainerTests>();
// BenchmarkRunner.Run<BenchmarkConcurrentDictionaryCreation>();
// BenchmarkRunner.Run<BenchmarkCreateScope>();
// BenchmarkRunner.Run<BenchmarkCreateAndDisposeScopeAsync>();
// BenchmarkRunner.Run<BenchmarkGetHashCode>();
// BenchmarkRunner.Run<BenchmarkConcurrentDictionaryLookup>();

// class Conf : ManualConfig
// {
//     public Conf() => SummaryStyle = SummaryStyle.Default.WithRatioStyle(RatioStyle.Value);
// }