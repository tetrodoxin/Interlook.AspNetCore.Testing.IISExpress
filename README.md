Interlook.AspNetCore.Testing.IISExpress
=======================================

Adds support for instances of *IIS-Express server* in integration tests of Asp .Net Core projects. This may be useful, when testing Windows/AD-authentication, for example, which is not supported by test servers like `Microsoft.AspNetCore.Mvc.Testing.TestServer`.


## Description

Testing ASP.NET Core projects (Web-API or MVC) is not sufficiently covered with unit tests, which is why integration tests exist. In *Visual Studio* you normally utilize testing frameworks like [xUnit.net](https://github.com/xunit/xunit), [NUnit](https://github.com/nunit/nunit) or the builtin *MS-Test*. Because launching tests does not regularly start an instance of IIS-Express server with the project to be tested, those tests of use in-memory test servers, like the one in the [Microsoft.AspNetCore.Mvc.Testing](https://www.nuget.org/packages/Microsoft.AspNetCore.Mvc.Testing) NuGet package.

But as these test server are designed to be lightweight, they lack special features/functionalities (e.g. windows authentication). Sometimes, however, one finds oneself in a situation where you have to test even those special functionalities. You could work around this by starting your project to be tests in debug mode with VS and this IIS-Express and launching your special integration tests separately, thus blindly depending on a running test instance of your server.

With this library it's possible to launch instances of IIS-Express inside your test classes and thereby control the settings of your test server.

## Usage

Essentially, there are two ways of creating an IIS-Express instance: directly or via test fixture class.

### Direct Launch

The relevant class is `IISExpress`.

You can it like this:

```csharp
using Interlook.AspNetCore.Testing.IISExpress;

// ... //

// use the actual location of your responsible applicationhost.config file
var appHostConfigFile = $"{solutionDirectory}\\.vs\\{projectName}\\config\\applicationhost.config"

// file path of the executable of your ASP.NET Core system under test (with extension)
var launcherRelativePath = "bin\\Debug\\netcoreapp3.1\\{assemblyName}.exe";

// this site must be in your applicationhost.config file
var siteName = "MySite";

// name of the application pool used by the site above (again, look in applicationhost.config)
var applicationPool = "MyAppPool"

var result =  = IISExpress.Start(appHostConfigFilePath, siteName, applicationPool, launcherRelativePath);

if(result is IISExpress.Process.Started proc)
{
    // IIS-Express has been started, you can communicate with it now

    // ... //

    proc.Stop();    // shuts down your started IIS-Express instance
}
else if(result is IISExpress.Process.Failed error)
{
    System.Diagnostics.Debug.WriteLine(error.Exception.Message);
}
```

Note, that `IISExpress.Start()` also takes further optional parameters, for the environment name and the full path of the IIS-Express executable file.
  

### Use `IISExpressTestServerFixture`

Testing frameworks may support fixture classes for test collections, which are objects being created once before executing a sequence of tests and disposed afterwards. For example, in *xUnit* you use `IClassFixture<MyFixtureClass>` for that purpose and the constructor of your test class receives an instance of `MyFixtureClass`.

To use the fixture for your IIS-Express instance, create fixture class deriving from 'IISExpressTestServerFixture'

```csharp
public class MyProjectTestServerFixture : IISExpressTestServerFixture
{
    public override string AppHostConfigFilePath => $"{SuTSolutionDirectory}\\.vs\\{SuTProjectName}\\config\\applicationhost.config";

    public override string SiteName => "MySite";

    public override string AppPool => "MyAppPool";

    public override string LauncherRelativePath => "bin\\Debug\\netcoreapp3.1\\{SuTAssemblyName}.exe";
}
```

Then, you can use it in any of your test classes, using that special endpoint of IIS-Express.

```csharp
public class MyTestClassOne : IClassFixture<MyProjectTestServerFixture> // example for xUnit
{
    public MyTestClassOne(MyProjectTestServerFixture fixture)
    {
        fixture.StartServer();            
    }
}
```

Those testclasses are expected to call `IDisposable.Dispose()` on fixture classes, when all contained test cases are finished. In this way the test server is automatically finished.

However, if, for whatever reason, the testing framework does not utilize this `IDisposable` scheme, you can implement it yourself, similar to this:

```csharp
public class MyTestClassOne : IClassFixture<MyProjectTestServerFixture> // example for xUnit
{
    private MyProjectTestServerFixture _fixture

    public MyTestClassOne(MyProjectTestServerFixture fixture)
    {
        _fixture = fixture;_
        _fixture.StartServer();            
    }

    ~MyTestClassOne()
    {
        _fixture.Dispose();
    }
}
```


That's basically it, guys.