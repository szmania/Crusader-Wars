using System;
using System.IO;
using CrusaderWars.client;
using CrusaderWars.games;
using Xunit;

namespace CrusaderWars.Tests;

public class GameServiceFactoryTests
{
    [Fact]
    public void GetGameService_Attila_ReturnsAttilaGameService()
    {
        var service = GameServiceFactory.GetGameService("Attila");
        Assert.NotNull(service);
        Assert.IsType<AttilaGameService>(service);
    }

    [Fact]
    public void GetGameService_Rome2_ReturnsRome2GameService()
    {
        var service = GameServiceFactory.GetGameService("Rome2");
        Assert.NotNull(service);
        Assert.IsType<Rome2GameService>(service);
    }

    [Fact]
    public void GetGameService_Unknown_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => GameServiceFactory.GetGameService("UnknownGame"));
    }

    [Theory]
    [InlineData("DefaultCK3", "Attila")]
    [InlineData("TheFallenEagle", "Attila")]
    [InlineData("RealmsInExile", "Attila")]
    [InlineData("AGOT", "Attila")]
    [InlineData("BronzeAgeReborn", "Rome2")]
    public void ResolveGameTypeFromPlaythrough_KnownPlaythroughs_ReturnsCorrectGameType(string playthrough, string expectedGameType)
    {
        var result = GameServiceFactory.ResolveGameTypeFromPlaythrough(playthrough);
        Assert.Equal(expectedGameType, result);
    }

    [Fact]
    public void ResolveGameTypeFromPlaythrough_EmptyString_ReturnsAttila()
    {
        var result = GameServiceFactory.ResolveGameTypeFromPlaythrough(string.Empty);
        Assert.Equal("Attila", result);
    }

    [Fact]
    public void ResolveGameTypeFromPlaythrough_Null_ReturnsAttila()
    {
        var result = GameServiceFactory.ResolveGameTypeFromPlaythrough(null);
        Assert.Equal("Attila", result);
    }

    [Fact]
    public void ResolveGameTypeFromPlaythrough_UnknownPlaythrough_ReturnsAttila()
    {
        var result = GameServiceFactory.ResolveGameTypeFromPlaythrough("UnknownPlaythrough");
        Assert.Equal("Attila", result);
    }

    [Fact]
    public void GetCurrentGameService_Default_ReturnsAttilaService()
    {
        // Reset any cached service
        typeof(GameServiceFactory)
            .GetField("_currentService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            ?.SetValue(null, null);

        var service = GameServiceFactory.GetCurrentGameService();
        Assert.NotNull(service);
        Assert.IsType<AttilaGameService>(service);
    }

    [Fact]
    public void GetCurrentGameService_AfterSet_ReturnsSetService()
    {
        var rome2Service = GameServiceFactory.GetGameService("Rome2");
        GameServiceFactory.SetCurrentGameService(rome2Service);

        var service = GameServiceFactory.GetCurrentGameService();
        Assert.Same(rome2Service, service);
    }

    [Fact]
    public void GetGameService_ReturnsSingletonInstances()
    {
        var attila1 = GameServiceFactory.GetGameService("Attila");
        var attila2 = GameServiceFactory.GetGameService("Attila");
        Assert.Same(attila1, attila2);

        var rome21 = GameServiceFactory.GetGameService("Rome2");
        var rome22 = GameServiceFactory.GetGameService("Rome2");
        Assert.Same(rome21, rome22);
    }
}