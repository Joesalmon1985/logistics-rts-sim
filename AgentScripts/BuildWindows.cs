/// <summary>
/// CLI entry: builds Builds/Windows/GodOfTheIsland.exe (see Assets/God/Editor/BuildWindows.cs).
/// Run: unity command script run --file AgentScripts/BuildWindows.cs --entry BuildWindows.Run --timeout 900
/// </summary>
public static class BuildWindows
{
    public static string Run() => God.Editor.BuildWindows.Run();
}
