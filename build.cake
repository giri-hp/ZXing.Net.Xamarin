#tool nuget:?package=XamarinComponent
#tool nuget:?package=vswhere

#addin nuget:?package=Cake.Android.SdkManager
#addin nuget:?package=Cake.XCode
#addin nuget:?package=Cake.Xamarin
#addin nuget:?package=Cake.Xamarin.Build
#addin nuget:?package=Cake.SemVer
#addin nuget:?package=Cake.FileHelpers
#addin nuget:?package=Cake.MonoApiTools

var PREVIEW = "";
var VERSION = EnvironmentVariable ("APPVEYOR_BUILD_VERSION") ?? Argument("version", "0.0.0");
var NUGET_VERSION_SUFFIX = "";
var NUGET_VERSION = VERSION + NUGET_VERSION_SUFFIX;

var ANDROID_HOME = EnvironmentVariable ("ANDROID_HOME") ?? Argument ("android_home", "");

var TARGET = Argument ("t", Argument ("target", "Default"));

Task("externals").Does (() => {
	Information ("ANDROID_HOME: {0}", ANDROID_HOME);

	var androidSdkSettings = new AndroidSdkManagerToolSettings { 
		SdkRoot = ANDROID_HOME,
		SkipVersionCheck = true
	};

	try { AcceptLicenses (androidSdkSettings); } catch { }

	AndroidSdkManagerInstall (new [] { 
			"platforms;android-15",
			"platforms;android-23",
			"platforms;android-25",
			"platforms;android-26"
		}, androidSdkSettings);
});

Task("libs")
	.Does(() =>
{
	NuGetRestore("./ZXing.Net.Mobile.sln");
	NuGetRestore("./ZXing.Net.Mobile.Forms.sln");

	DirectoryPath vsLatest = VSWhereLatest();
	string msbuildExeRelativePath = "./MSBuild/Current/Bin/MSBuild.exe";
	FilePath msbuildPathFromVS = (vsLatest == null) ? null : vsLatest.CombineWithFilePath(msbuildExeRelativePath); 

	var config = IsRunningOnWindows() ? "ReleaseWin" : "ReleaseMac";

	var msbuildSettings = new MSBuildSettings {
		Configuration = config,
		MSBuildPlatform = MSBuildPlatform.x86,
		ToolPath = msbuildPathFromVS,
	};

	MSBuild ("./ZXing.Net.Mobile.sln", msbuildSettings);
	MSBuild ("./ZXing.Net.Mobile.Forms.sln", msbuildSettings);
});

Task ("samples")
	.IsDependentOn("libs")
	.Does (() =>
{
	NuGetRestore ("./Samples/Android/Sample.Android.sln");
	NuGetRestore ("./Samples/iOS/Sample.iOS.sln");
	NuGetRestore ("./Samples/Forms/Sample.Forms.sln");
	NuGetRestore ("./Samples/WindowsUniversal/Sample.WindowsUniversal.sln");

	var config = "Release";
	MSBuild ("./Samples/Android/Sample.Android.sln", c => c.SetConfiguration(config).SetMSBuildPlatform(MSBuildPlatform.x86));
	MSBuild ("./Samples/iOS/Sample.iOS.sln", c => c.SetConfiguration(config).SetMSBuildPlatform(MSBuildPlatform.x86));

	if (IsRunningOnWindows()) {
		MSBuild ("./Samples/Forms/Sample.Forms.sln", c => c.SetConfiguration(config).SetMSBuildPlatform(MSBuildPlatform.x86));
		MSBuild ("./Samples/WindowsUniversal/Sample.WindowsUniversal.sln", c => c.SetConfiguration(config).SetMSBuildPlatform(MSBuildPlatform.x86));
	}
});

Task ("nuget")
	.IsDependentOn("libs")
	.Does (() =>
{
	NuGetPack ("./ZXing.Net.Mobile.nuspec", new NuGetPackSettings { Version = NUGET_VERSION });
	NuGetPack ("./ZXing.Net.Mobile.Forms.nuspec", new NuGetPackSettings { Version = NUGET_VERSION });
});

Task ("clean")
	.Does (() =>
{
	CleanDirectories ("./**/bin");
	CleanDirectories ("./**/obj");
});

Task("Default")
    .IsDependentOn("libs");

RunTarget (TARGET);

