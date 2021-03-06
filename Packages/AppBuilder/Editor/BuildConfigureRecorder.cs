using System;
using System.Collections.Generic;
using System.Text;

namespace AppBuilder
{
    public enum BuildPropertyOptions
    {
        None,
        SectionBegin,
        SectionEnd,
    }

    public readonly struct BuildProperty
    {
        public BuildPropertyOptions Options { get; }

        public BuildProperty(string name, string value)
        {
            Name = name;
            Value = value;
            Options = BuildPropertyOptions.None;
        }

        public BuildProperty(string name, BuildPropertyOptions options)
        {
            Name = name;
            Value = null;
            Options = options;
        }

        public string Name { get; }
        public string Value { get; }
    }

    public readonly struct ConfigureSection : IDisposable
    {
        private readonly BuildConfigureRecorder _recorder;

        public ConfigureSection(BuildConfigureRecorder recorder, string name)
        {
            _recorder = recorder;
            _recorder.Write(new BuildProperty(name, BuildPropertyOptions.SectionBegin));
        }

        public void Dispose()
        {
            _recorder.Write(new BuildProperty(string.Empty, BuildPropertyOptions.SectionEnd));
        }
    }

    public static class BuildConfigureRecorderExtensions
    {
        public static ConfigureSection Section(this BuildConfigureRecorder recorder, string name)
        {
            return new ConfigureSection(recorder, name);
        }
    }

    public enum ConfigureTiming
    {
        ConfigureOnly,
        ExecuteBuild
    }

    public class BuildConfigureRecorder
    {
        private readonly StringBuilder _builder = new();

        // private readonly List<Action> _configureActions = new();

        private readonly Dictionary<ConfigureTiming, List<Action>> _configureActions = new();
        private readonly List<BuildProperty> _configureMessages = new();

        public BuildProperty[] GetProperties() => _configureMessages.ToArray();

        private void AddAction(ConfigureTiming timing, Action execute)
        {
            if (!_configureActions.TryGetValue(timing, out var list))
            {
                list = new List<Action>();
                _configureActions.Add(timing, list);
            }

            list.Add(execute);
        }

        public void Enqueue(Action execute, BuildProperty property,
            ConfigureTiming timing = ConfigureTiming.ConfigureOnly)
        {
            AddAction(timing, execute);
            Write(property);
        }

        public void Enqueue(Action execute, BuildProperty[] properties,
            ConfigureTiming timing = ConfigureTiming.ConfigureOnly)
        {
            AddAction(timing, execute);
            foreach (var property in properties)
            {
                Write(property);
            }
        }

        public void Enqueue(Action execute, string section, ConfigureTiming timing = ConfigureTiming.ConfigureOnly,
            params BuildProperty[] properties)
        {
            AddAction(timing, execute);
            Write(new BuildProperty(section, BuildPropertyOptions.SectionBegin));
            foreach (var property in properties)
            {
                Write(property);
            }

            Write(new BuildProperty(section, BuildPropertyOptions.SectionEnd));
        }

        public void Write(BuildProperty property)
        {
            _configureMessages.Add(property);
        }

        public void Write(string name, string value)
        {
            _configureMessages.Add(new BuildProperty(name, value));
        }

        public Action[] Export(ConfigureTiming timing = ConfigureTiming.ConfigureOnly)
        {
            if (_configureActions.TryGetValue(timing, out var list))
            {
                return list.ToArray();
            }

            return Array.Empty<Action>();
        }

        public override string ToString()
        {
            _builder.Clear();
            foreach (var property in _configureMessages)
            {
                _builder.AppendLine($"{property.Name,-50}{property.Value,-50}");
            }

            return _builder.ToString();
        }
    }
}