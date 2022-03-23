using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AppBuilder
{
    public static partial class BuildPlayer
    {
        private static BuildScope _buildScope;

        public readonly struct ArgumentsBuilder
        {
            private readonly Arguments _arguments;

            public ArgumentsBuilder(Arguments arguments)
            {
                _arguments = arguments;
            }

            public void Add(string key, string value)
            {
                _arguments[key] = new ArgumentValue(key, value, ArgumentCategory.Custom);
            }
        }

        public static Report Build(Action<ArgumentsBuilder> arguments,
            Action<IBuildContext, IUnityPlayerBuilder> configuration)
        {
            var args = new Arguments();
            arguments(new ArgumentsBuilder(args));

            args.Merge(Environment.GetCommandLineArgs()
                .AddReserveArguments());

            if (_buildScope != null) //from editor
            {
                var inputs = _buildScope.InputArgs;
                foreach (var input in inputs)
                {
                    args.Add(input.Key, input.Value.Format(args));
                }
            }

            return Build(args, configuration);
        }

        public static Report Build(Action<IBuildContext, IUnityPlayerBuilder> configuration)
        {
            var args = Environment.GetCommandLineArgs()
                .AddReserveArguments();

            if (_buildScope != null) //from editor
            {
                var inputs = _buildScope.InputArgs;
                foreach (var input in inputs)
                {
                    args.Add(input.Key, input.Value.Format(args));
                }
            }

            return Build(args, configuration);
        }

        private static Report Build(Arguments args, Action<IBuildContext, IUnityPlayerBuilder> configuration)
        {
            Debug.Log(args);
            var context = new UnityBuildContext(args);
            var builder = new UnityPlayerBuilder();
            configuration(context, builder);

            var executor = builder.Build();

            if (args.TryGetValue("mode", out var mode))
            {
                args.Remove("mode");
                if (mode == "preview")
                {
                    return Complete(new Report(context, builder));
                }

                if (mode == "configure")
                {
                    executor.Configure();
                    return Complete(new Report(context, builder));
                }
            }

            try
            {
                executor.Validate();
            }
            catch (Exception e)
            {
                Debug.Log($"[AppBuilder] Build Failed {e.Message}");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(0);
                }
            }

            var unityReport = executor.Execute();
            //todo: BatchMode -> Revert? builder.Revert()
            if (!Application.isBatchMode)
            {
                EditorUtility.RevealInFinder(unityReport.summary.outputPath);
            }

            return Complete(new Report(context, builder, unityReport));

            Report Complete(Report report)
            {
                if (_buildScope != null)
                {
                    _buildScope.Report = report;
                }

                return report;
            }
        }
    }
}