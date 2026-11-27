/// <summary>
/// CLI entry: (re)creates Assets/God/Scenes/Main.unity.
/// Run: unity command script run --file AgentScripts/MakeMainScene.cs --entry MakeMainScene.Run
/// </summary>
public static class MakeMainScene
{
    public static string Run() => God.Editor.MakeMainScene.Run();
}
