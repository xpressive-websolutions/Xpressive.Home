
var configuration = Argument("Configuration", "Release");

Information(configuration);

Task("Default").Does(() =>
{
    Information("Hello World!");
});

Setup(context =>
{
    var build = (int)(DateTime.Now - new DateTime(2015, 1, 1)).TotalHours;
    var version = string.Format("1.0.0.{0}", build);

    var assemblyInfo = new AssemblyInfoSettings
    {
        Configuration = "",
        Company = "Xpressive Websolutions",
        Product = "Xpressive.Home",
        Copyright = "Copyright \x00A9 " + DateTime.Now.Year,
        Trademark = "",
        ComVisible = false,
        Version = version,
        FileVersion = version,
        InformationalVersion = version + "-beta1"
    };

    CreateAssemblyInfo("./Xpressive.Home/Properties/AssemblyInfo.shared.cs", assemblyInfo);
});

Task("Pre-Build Clean Up").Does(() =>
{
    var directories = GetDirectories("./Xpressive.*/bin");
    
    foreach(var directory in directories)
    {
        CleanDirectory(directory.FullPath + "/" + configuration);
    }

    if (DirectoryExists("Build"))
    {
        if (DirectoryExists("Build/Plugins"))
        {
            CleanDirectory("Build/Plugins");
        }

        CleanDirectory("Build");
    }
});

Task("Build").IsDependentOn("Pre-Build Clean Up").Does(() =>
{
    MSBuild("./Xpressive.Home.sln", new MSBuildSettings {
        Verbosity = Verbosity.Minimal,
        ToolVersion = MSBuildToolVersion.VS2015,
        Configuration = configuration,
        PlatformTarget = PlatformTarget.MSIL
    });
});

Task("Copy").IsDependentOn("Build").Does(() =>
{
    CreateDirectory("Build");
    CreateDirectory("Build/Plugins");

    Func<IFileSystemInfo, bool> onlyPluginDirectories = fileSystemInfo =>
        fileSystemInfo.Path.FullPath.Contains(".Plugins.") &&
        !fileSystemInfo.Path.FullPath.EndsWith(".Tests", StringComparison.OrdinalIgnoreCase);
            
    var directories = GetDirectories("./*", onlyPluginDirectories);

    foreach(var directory in directories)
    {
        var binaries = GetFiles(directory + "/bin/" + configuration + "/*.*");
        CopyFiles(binaries, "./Build/Plugins");
    }

    CopyFiles(GetFiles("Xpressive.Home.ConsoleHost/bin/" + configuration + "/*.*"), "./Build");
    CopyFiles(GetFiles("Xpressive.Home.WebApi/bin/" + configuration + "/*.*"), "./Build");

    DeleteFiles("./Build/**/*.xml");
});

Task("Zip").IsDependentOn("Copy").Does(() =>
{
    Zip("./Build/", "Build.zip", "./Build/**/*.*");
});

Task("Sign").IsDependentOn("Zip").Does(() =>
{
    var file = MakeAbsolute(File("./build.zip")).FullPath;
    StartProcess("./Xpressive.Home.Deployment.Sign/bin/" + configuration + "/Xpressive.Home.Deployment.Sign.exe", "\"" + file + "\"");
});

Task("Clean Up").IsDependentOn("Sign").Does(() =>
{
    CleanDirectory("Build");
    DeleteDirectory("Build", true);
});

RunTarget("Clean Up");
