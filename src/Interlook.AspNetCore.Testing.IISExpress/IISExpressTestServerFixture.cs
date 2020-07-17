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
using System.Text;

namespace Interlook.AspNetCore.Testing.IISExpress
{
    /// <summary>
    /// Fixture class for using IIS-Express as test server in integration tests.
    /// <para>By implementing <see cref="IDisposable"/> the server started by <see cref="StartServer(bool)"/>
    /// is automatically shut down. This mechanism is used by testing frameworks like <c>XUnit</c>.
    /// </para>
    /// </summary>
    public abstract class IISExpressTestServerFixture : IDisposable
    {
        private bool _disposedValue;
        private IISExpress.Process _instance;

        /// <summary>
        /// Gets or sets the IIS express executable file path.
        /// <para>Default value is <see cref="IISExpress.DefaultIISExpressExecutablePath"/></para>
        /// </summary>
        public string IISExpressExecutableFilePath { get; set; } = IISExpress.DefaultIISExpressExecutablePath;

        /// <summary>
        /// Gets or sets the value of the <c>ASPNETCORE_ENVIRONMENT</c> environment variable,
        /// thus the name of the environment (Production, Development etc.)
        /// <para>Default value is <see cref="IISExpress.DefaultAspEnvironment"/> (usually 'Development')</para>
        /// </summary>
        public string AspEnvironment { get; set; } = IISExpress.DefaultAspEnvironment;

        /// <summary>
        /// Full path of an 'applicationhost.config' file, belonging to the ASP .NET Core Project under Test.
        /// </summary>
        public abstract string AppHostConfigFilePath { get; }

        /// <summary>
        /// Name of the site, which must be contained in the config file provided in <see cref="AppHostConfigFilePath"/>.
        /// </summary>
        public abstract string SiteName { get; }

        /// <summary>
        /// Name of the application pool (also in file <see cref="AppHostConfigFilePath"/>)
        /// </summary>
        public abstract string AppPool { get; }

        /// <summary>
        /// File path of the executable file of the 'Project under Test' (including extension),
        /// relative to the projects base path, this is usually where the <c>.csproj</c> file resides.
        /// </summary>
        public abstract string LauncherRelativePath { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="IISExpressTestServerFixture"/> class.
        /// </summary>
        public IISExpressTestServerFixture()
        {
            _instance = IISExpress.Process.None.Default;
        }

        /// <summary>
        /// Starts the IIS Express server.
        /// </summary>
        /// <param name="throwExceptionOnError">if set to <c>true</c> an exception, that occurs during launching the server process
        /// is thrown right after logging it to <see cref="System.Diagnostics.Debug"/> and <see cref="Console.Error"/>;
        /// otherwise the exception is catched and the method will return <c>false</c>.</param>
        /// <returns>
        /// A value indicating, whether the server start has succeeded.
        /// </returns>
        /// <exception cref="InvalidOperationException">Error launching server. Only thrown if <paramref name="throwExceptionOnError"/> was <c>true</c>.</exception>
        public bool StartServer(bool throwExceptionOnError = false)
        {
            _instance = IISExpress.Start(AppHostConfigFilePath, SiteName, AppPool, LauncherRelativePath);
            if (_instance is IISExpress.Process.Failed fail)
            {
                var errorText = new StringBuilder("Error starting IISExpress.")
                    .AppendLine()
                    .AppendLine($"Error: {fail.Exception.Message}")
                    .ToString();
                System.Diagnostics.Debug.WriteLine(errorText);
                Console.Error.WriteLine(errorText);
                return !throwExceptionOnError ? false : throw new InvalidOperationException("Error launching server.", fail.Exception);
            }
            else if (_instance is IISExpress.Process.Started)
            {
                return true;
            }
            else
            {
                if (throwExceptionOnError) throw new InvalidOperationException("Unknown error launching server. StartServer() returned unexpected object.");
                return false;
            }
        }

        /// <summary>
        /// Disposes this instance and terminates a possibly started server.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    if (_instance is IISExpress.Process.Started start)
                    {
                        start.Stop();
                    }
                }

                _disposedValue = true;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}