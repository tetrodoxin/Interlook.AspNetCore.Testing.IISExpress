#region license

//MIT License

//Copyright(c) 2013-2020 Andreas Hübner

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

#endregion 
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Interlook.AspNetCore.Testing.IISExpress
{
    /// <summary>
    /// A wrapper for starting local IIS-Express instance for ASP .NET  Core projects.
    /// </summary>
    public class IISExpress
    {
        /// <summary>
        /// The default ASP environment
        /// </summary>
        public const string DefaultAspEnvironment = "Development";

        /// <summary>
        /// The default IIS express executable path
        /// </summary>
        public const string DefaultIISExpressExecutablePath = @"C:\Program Files\IIS Express\iisexpress.exe";

        private const string ArgumentNameAppPool = "apppool";
        private const string ArgumentNameConfig = "config";
        private const string ArgumentNameSite = "site";

        private const int WM_Quit = 0x12;
        private string _appPool;
        private string _config;
        private System.Diagnostics.Process _process;
        private string _site;

        private IISExpress(string appHostConfigFilePath, string siteName, string appPool, string launcherRelativePath, string aspEnvironmentName, string executableFilePath)
        {
            _config = appHostConfigFilePath;
            _site = siteName;
            _appPool = appPool;

            var argumentsBuilder = new StringBuilder();
            if (!string.IsNullOrEmpty(_config))
                argumentsBuilder.Append($"/{ArgumentNameConfig}:{_config} ");

            if (!string.IsNullOrEmpty(_site))
                argumentsBuilder.Append($"/{ArgumentNameSite}:{_site} ");

            if (!string.IsNullOrEmpty(_appPool))
                argumentsBuilder.Append($"\"/{ArgumentNameAppPool}:{_appPool}\"");

            var procStartInfo = new ProcessStartInfo()
            {
                FileName = executableFilePath,
                Arguments = argumentsBuilder.ToString().TrimEnd(),
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            procStartInfo.EnvironmentVariables.Add("ASPNETCORE_ENVIRONMENT", aspEnvironmentName);
            procStartInfo.EnvironmentVariables.Add("LAUNCHER_PATH", launcherRelativePath);

            _process = new System.Diagnostics.Process { StartInfo = procStartInfo };
        }

        /// <summary>
        /// Starts a new IIS-Express process for a given ASP .NET Core project.
        /// </summary>
        /// <param name="appHostConfigFilePath">Full path of an 'applicationhost.config' file, belonging to the ASP .NET Core Project under Test.</param>
        /// <param name="siteName">Name of the site, which must be contained in the config file provided in <paramref name="appHostConfigFilePath"/>.</param>
        /// <param name="appPool">Name of the application pool (also in file <paramref name="appHostConfigFilePath"/>).</param>
        /// <param name="launcherRelativePath">File path of the executable file of the 'Project under Test' (including extension),
        /// relative to the projects base path, this is usually where the <c>.csproj</c> file resides.</param>
        /// <param name="iisExpressExecutableFilePath">Full path to the executable file of IIS-Express server.</param>
        /// <param name="aspEnvironmentName">Name of the ASP environment (defaults to <c>Development</c>), set via the <c>ASPNETCORE_ENVIRONMENT</c> environment variable.</param>
        /// <returns>An instance of <see cref="Process.Started"/>, if the server process has been successfully launched, that can be used to stop that server process;
        /// otherwise a <see cref="Process.Failed"/> instance containing the error/exception that occured.</returns>
        /// <exception cref="ArgumentNullException">
        /// appHostConfigFilePath
        /// or
        /// siteName
        /// or
        /// appPool
        /// or
        /// launcherRelativePath
        /// </exception>
        public static Process Start(string appHostConfigFilePath, string siteName, string appPool, string launcherRelativePath, string iisExpressExecutableFilePath = DefaultIISExpressExecutablePath, string aspEnvironmentName = DefaultAspEnvironment)
        {
            try
            {
                var iis = new IISExpress(appHostConfigFilePath ?? throw new ArgumentNullException(nameof(appHostConfigFilePath)),
                    siteName ?? throw new ArgumentNullException(nameof(siteName)),
                    appPool ?? throw new ArgumentNullException(nameof(appPool)),
                    launcherRelativePath ?? throw new ArgumentNullException(nameof(launcherRelativePath)),
                    aspEnvironmentName ?? string.Empty,
                    iisExpressExecutableFilePath ?? DefaultIISExpressExecutablePath);

                if (iis._process.Start() == false)
                    return new Process.Failed(new InvalidOperationException("IIS process launch failed unexpectedly."));

                if (iis._process.HasExited)
                    return new Process.Failed(new InvalidOperationException("IIS process has exited unexpectedly."));

                return new Process.Started(iis);
            }
            catch (Exception ex)
            {
                return new Process.Failed(ex);
            }
        }

        private static void sendStopMessageToProcess(int processID)
        {
            try
            {
                for (var ptr = User32.GetTopWindow(IntPtr.Zero); ptr != IntPtr.Zero; ptr = User32.GetWindow(ptr, 2))
                {
                    User32.GetWindowThreadProcessId(ptr, out uint windowProcID);
                    if (processID == windowProcID)
                    {
                        var hWnd = new HandleRef(null, ptr);
                        User32.PostMessage(hWnd, WM_Quit, IntPtr.Zero, IntPtr.Zero);
                        return;
                    }
                }
            }
            catch
            { /* just to be safer. Dealing with native methods is... non-deterministic */ }
        }

        /// <summary>
        /// Abstract base class for the status of a process start of an IIS-Express server instance. Follows the functional style non-null approach.
        /// </summary>
        public abstract class Process
        {
            /// <summary>
            /// Indicates a successful launch
            /// </summary>
            public abstract bool IsSuccess { get; }

            internal Process()
            { }

            /// <summary>
            /// Performs an implicit conversion from <see cref="Process"/> to <see cref="bool"/>.
            /// </summary>
            /// <param name="state">The state object.</param>
            /// <returns>
            /// <c>true</c>, if the concrete instance of <see cref="Process"/> provided in <paramref name="state"/>
            /// is of type <see cref="Process.Started"/>
            /// </returns>
            public static implicit operator bool(Process state)
                => state != null ? state.IsSuccess : false;

            /// <summary>
            /// Type indicating an error of the process launch.
            /// </summary>
            /// <seealso cref="Process" />
            public sealed class Failed : Process
            {
                /// <summary>
                /// Actual occured error/exception during process launch.
                /// </summary>
                public Exception Exception { get; }

                /// <summary>
                /// Indicates a successful launch
                /// </summary>
                public override bool IsSuccess => false;

                internal Failed(Exception ex)
                {
                    Exception = ex;
                }
            }

            /// <summary>
            /// The <c>NULL</c>-equivalent of the <see cref="Process"/> class
            /// </summary>
            /// <seealso cref="Interlook.AspNetCore.Testing.IISExpress.IISExpress.Process" />
            public sealed class None : Process
            {
                private static Lazy<None> _instance = new Lazy<None>(() => new None());

                /// <summary>
                /// The only instance of the class.
                /// </summary>
                public static None Default => _instance.Value;

                /// <summary>
                /// Indicates a successful launch
                /// </summary>
                public override bool IsSuccess => false;

                private None()
                { }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

                public override bool Equals(object? obj) => obj is None;

                public override int GetHashCode() => 0;

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
            }

            /// <summary>
            /// Type referring to a successful server process launch.
            /// </summary>
            /// <seealso cref="Process" />
            public sealed class Started : Process
            {
                private IISExpress _iisObject;

                /// <summary>
                /// Indicates a successful launch
                /// </summary>
                public override bool IsSuccess => true;

                internal Started(IISExpress iisObject)
                {
                    _iisObject = iisObject;
                }

                /// <summary>
                /// Method to stop the launched server process.
                /// </summary>
                public void Stop()
                {
                    sendStopMessageToProcess(_iisObject._process.Id);
                    _iisObject._process.Close();
                }
            }
        }

        private class User32
        {
            [DllImport("user32.dll", SetLastError = true)]
            internal static extern IntPtr GetTopWindow(IntPtr hWnd);

            [DllImport("user32.dll", SetLastError = true)]
            internal static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

            [DllImport("user32.dll", SetLastError = true)]
            internal static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint lpdwProcessId);

            [DllImport("user32.dll", SetLastError = true)]
            internal static extern bool PostMessage(HandleRef hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        }
    }
}