using NUnit.Framework;

namespace God.Game.Tests
{
    /// <summary>The start card holds the game until a choice is made; the play-mode tests drive the scene themselves, so it stays away.</summary>
    [SetUpFixture]
    public class TestSetup
    {
        [OneTimeSetUp]
        public void SkipTheStartCard() { GodBootstrap.SkipStartCard = true; }
    }
}
