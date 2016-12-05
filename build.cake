
var configuration = Argument("Configuration", "Release");

Information(configuration);

var build = (int)(DateTime.Now - new DateTime(2015, 1, 1)).TotalHours;
var versionPrefix = "1.0.0";
var version = string.Format("{0}.{1}", versionPrefix, build);
var informationalVersion = versionPrefix + "-beta.6";

Setup(context =>
{
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
        InformationalVersion = informationalVersion
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

        if (DirectoryExists("Build/Web"))
        {
            if (DirectoryExists("Build/Web/app")) CleanDirectory("Build/Web/app");
            if (DirectoryExists("Build/Web/Scripts")) CleanDirectory("Build/Web/Scripts");
            if (DirectoryExists("Build/Web/Styles")) CleanDirectory("Build/Web/Styles");

            CleanDirectory("Build/Web");
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
    CopyFiles(GetFiles("Xpressive.Home.Service/bin/" + configuration + "/*.*"), "./Build");
    CopyFiles(GetFiles("Xpressive.Home.Services/bin/" + configuration + "/*.*"), "./Build");
    CopyFiles(GetFiles("Xpressive.Home.WebApi/bin/" + configuration + "/*.*"), "./Build");
    CopyFiles(GetFiles("Xpressive.Home.Deployment.Updater/bin/" + configuration + "/*.*"), "./Build");

    DeleteFiles("./Build/**/*.xml");
});

Task("Copy Web").IsDependentOn("Copy").Does(() =>
{
    CreateDirectory("Build/Web");
    CreateDirectory("Build/Web/app");
    CreateDirectory("Build/Web/app/admin");
    CreateDirectory("Build/Web/Scripts");
    CreateDirectory("Build/Web/Styles");
    CreateDirectory("Build/Web/Icons");

    CopyFiles(GetFiles("Xpressive.Home.WebApi/*.html"), "./Build/Web");
    CopyFiles(GetFiles("Xpressive.Home.WebApi/app/admin/*.min.html"), "./Build/Web/app/admin");
    CopyFiles(GetFiles("Xpressive.Home.WebApi/app/*.min.html"), "./Build/Web/app");
    CopyFiles(GetFiles("Xpressive.Home.WebApi/app/*.min.js"), "./Build/Web/app");
    CopyFiles(GetFiles("Xpressive.Home.WebApi/Scripts/*.js"), "./Build/Web/Scripts");
    CopyFiles(GetFiles("Xpressive.Home.WebApi/Styles/*.min.css"), "./Build/Web/Styles");
    CopyFiles(GetFiles("Xpressive.Home.WebApi/Styles/*.jpg"), "./Build/Web/Styles");
    CopyDirectory("Xpressive.Home.WebApi/Styles/Icons", "Build/Web/Styles/Icons");

    var adminHtml = System.IO.File.ReadAllText(File("./Build/Web/admin.html"));
    var indexHtml = System.IO.File.ReadAllText(File("./Build/Web/index.html"));
    adminHtml = adminHtml.Replace("?v=app_ver", "?v=" + informationalVersion);
    indexHtml = indexHtml.Replace("?v=app_ver", "?v=" + informationalVersion);
    System.IO.File.WriteAllText(File("./Build/Web/admin.html"), adminHtml);
    System.IO.File.WriteAllText(File("./Build/Web/index.html"), indexHtml);
});

Task("Zip").IsDependentOn("Copy Web").Does(() =>
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
    //CleanDirectory("Build");
    //DeleteDirectory("Build", true);
});

RunTarget("Clean Up");
