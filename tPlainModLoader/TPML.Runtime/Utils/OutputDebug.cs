using System;
using System.Diagnostics;
using System.Reflection;
using TPML.Core.Logging;

namespace tContentPatch.Utils
{
    internal class OutputDebug
    {
        private static readonly ILogger Logger = LogManager.GetLogger("ModLoader");

        public static void OutputException(Exception ex, int stackTrace = 1)
        {
            StackTrace st = new StackTrace();
            StackFrame sf = st.GetFrame(stackTrace) ?? st.GetFrame(1);
            MethodBase method = sf?.GetMethod();
            string typeName = method?.ReflectedType?.Name ?? "?";
            string methodName = method?.Name ?? "?";
            Logger.Error($"{typeName}.{methodName}异常:{ex.Message}", ex);
        }
    }
}
