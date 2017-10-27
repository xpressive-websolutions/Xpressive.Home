
var configuration = Argument("Configuration", "Build");

Information(configuration);

var build = (int)(DateTime.Now - new DateTime(2015, 1, 1)).TotalHours;
var versionPrefix = "1.0.0";
var version = string.Format("{0}.{1}", versionPrefix, build);
var informationalVersion = versionPrefix + "-beta.11";

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
    XmlPoke(File("./Build/Xpressive.Home.ConsoleHost.exe.config"), "/configuration/appSettings/add/@value", "");
    XmlPoke(File("./Build/Xpressive.Home.Service.exe.config"), "/configuration/appSettings/add/@value", "");

    DeleteFiles("./Build/*.xml");
    DeleteFiles("./Build/Plugins/*.xml");
	DeleteDirectory("./Build/config", recursive:true);
});

Task("Copy Web").IsDependentOn("Copy").Does(() =>
{
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

Task("Package").IsDependentOn("Sign").Does(() =>
{
	var files = GetFiles("./build.z*");
	Zip("./", informationalVersion + ".zip", files);
});

Task("Clean Up").IsDependentOn("Package").Does(() =>
{
    //CleanDirectory("Build");
    //DeleteDirectory("Build", true);
});

RunTarget("Clean Up");
