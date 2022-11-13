// using System.Collections.Concurrent;
//
// namespace DDS.Helper;
//
// public sealed class TopLevelHelper
// {
//     private static TopLevelHelper? _instance;
//
//     private static TopLevel? _singleViewApplicationTopLevel;
//
//     public TopLevelHelper()
//     {
//         if (Globals.IsClassicDesktopStyleApplicationLifetime)
//         {
//             _dict = new();
//             GetTopLevelDelegate = GetFromDict;
//         }
//         else
//         {
//             GetTopLevelDelegate = GetStatic;
//         }
//     }
//
//     private Func<TopLevel> GetTopLevelDelegate { get; }
//
//     private readonly ConcurrentDictionary<string, TopLevel>? _dict;
//
//     private TopLevel GetFromDict()
//     {
//         _dict![]
//     }
//     
//     private static TopLevel GetStatic() => 
//         _singleViewApplicationTopLevel ?? throw new NullReferenceException("TopLevel has not been set yet.");
//
//     public static void Set(TopLevel topLevel)
//     {
//         if (!Globals.IsStartupDone) throw new InvalidOperationException("We don't accept fake TopLevels.");
//         
//     }
//
//     public static TopLevel Get()
//     {
//         if (!Globals.IsStartupDone) throw new NullReferenceException("TopLevel has not been set yet.");
//         _instance ??= new TopLevelHelper();
//         return _instance.GetTopLevelDelegate();
//     }
// }