////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Load diagnostics: runs as soon as Inventor touches the ThreadModeler assembly (module initializer),
// i.e. before the add-in object is created. Records where and how the assembly was loaded and logs
// dependencies that cannot be resolved - the usual reasons for an add-in that stays "Unloaded".
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace ThreadModeler.Addin
{
    internal static class LoadDiagnostics
    {
        private static readonly HashSet<string> Reported = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static string _addinDirectory;

#pragma warning disable CA2255 // Intentional: must run before Inventor creates the add-in object.
        [ModuleInitializer]
        internal static void OnModuleLoaded()
#pragma warning restore CA2255
        {
            try
            {
                Assembly self = typeof(LoadDiagnostics).Assembly;
                AssemblyLoadContext alc = AssemblyLoadContext.GetLoadContext(self);
                _addinDirectory = System.IO.Path.GetDirectoryName(self.Location);

                AddinLog.Info("----------------------------------------------------------------");
                AddinLog.Info("ThreadModeler " + self.GetName().Version + " loaded from " + self.Location);
                AddinLog.Info("Runtime: " + RuntimeInformation.FrameworkDescription +
                              ", load context: " + Describe(alc) +
                              ", process: " + Environment.ProcessPath);

                foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    string name = a.GetName().Name;
                    if (name == "Autodesk.Inventor.Interop" || name == "stdole")
                        AddinLog.Info("Loaded: " + a.GetName().FullName + " in " + Describe(AssemblyLoadContext.GetLoadContext(a)));
                }

                foreach (AssemblyName reference in self.GetReferencedAssemblies())
                {
                    if (!reference.Name.StartsWith("System", StringComparison.Ordinal) &&
                        !reference.Name.StartsWith("Microsoft", StringComparison.Ordinal) &&
                        reference.Name != "netstandard" && reference.Name != "mscorlib")
                        AddinLog.Info("References: " + reference.FullName);
                }

                if (alc != null)
                    alc.Resolving += OnResolving;
            }
            catch (Exception ex)
            {
                AddinLog.Error("Load diagnostics failed.", ex);
            }
        }

        /// <summary>
        /// Called when the load context cannot find an assembly. Tries the add-in folder, and logs the failure
        /// (once per name) otherwise. Returns null to let other handlers try.
        /// </summary>
        private static Assembly OnResolving(AssemblyLoadContext context, AssemblyName name)
        {
            try
            {
                string candidate = _addinDirectory != null ? System.IO.Path.Combine(_addinDirectory, name.Name + ".dll") : null;
                if (candidate != null && File.Exists(candidate))
                {
                    AddinLog.Info("Resolving " + name.FullName + " from the add-in folder.");
                    return context.LoadFromAssemblyPath(candidate);
                }

                bool first;
                lock (Reported)
                    first = Reported.Add(name.Name) && Reported.Count <= 50;
                if (first)
                    AddinLog.Warn("Could not resolve " + name.FullName + " in " + Describe(context) + ".");
            }
            catch (Exception ex)
            {
                AddinLog.Warn("Resolving " + name.FullName + " failed.", ex);
            }
            return null;
        }

        private static string Describe(AssemblyLoadContext alc)
        {
            if (alc == null)
                return "(none)";
            if (alc == AssemblyLoadContext.Default)
                return "Default";
            return "'" + (alc.Name ?? alc.GetType().FullName) + "'";
        }
    }
}
