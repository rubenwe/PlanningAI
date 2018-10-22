using System.Diagnostics.CodeAnalysis;

namespace PlanningAi.Utils.Logging
{
    [ExcludeFromCodeCoverage]
    internal sealed class DummyLogger : ILogger
    {
        public void Debug(string message, params object[] args)
        {
        }

        public void Trace(string message, params object[] args)
        {
        }

        public void Info(string message, params object[] args)
        {
        }

        public void Warn(string message, params object[] args)
        {
        }

        public void Error(string message, params object[] args)
        {
        }
    }
}