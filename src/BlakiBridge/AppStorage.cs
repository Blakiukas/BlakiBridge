namespace BlakiBridge;
internal static class AppStorage
{
    internal static bool IsPortable => !PackageMode.IsPackaged && File.Exists(Path.Combine(AppContext.BaseDirectory,"portable.flag"));
    internal static string DirectoryPath => IsPortable
        ? Path.Combine(AppContext.BaseDirectory,"Data")
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"BlakiBridge");
}
