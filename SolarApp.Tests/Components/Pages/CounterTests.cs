using Bunit;
using SolarApp.Components.Pages;

namespace SolarApp.Tests.Components.Pages;

[TestClass]
public sealed class CounterTests
{
    [TestMethod]
    public void Counter_ClickSurLeBouton_IncrementeLaValeur()
    {
        using var testContext = new BunitContext();

        var component = testContext.Render<Counter>();

        Assert.AreEqual("Current count: 0", component.Find("p[role='status']").TextContent.Trim());

        component.Find("button").Click();

        Assert.AreEqual("Current count: 1", component.Find("p[role='status']").TextContent.Trim());
    }
}
