using System;
using System.IO;
using System.Linq;
using CrusaderWars.unit_mapper;
using Xunit;

namespace CrusaderWars.Tests;

public class WeatherDataTests
{
    private const string WeatherXml = """
        <?xml version="1.0" encoding="UTF-8"?>
        <Weather>
          <Land>
            <Day>
              <WeatherType ck3_type="summer">
                <Environment key="weather\summer\sunny_fields_day.environment"/>
                <Environment key="weather\summer\sunny_forest_day.environment"/>
              </WeatherType>
              <WeatherType ck3_type="harsh_winter">
                <Environment key="weather\winter\snow_heavy_day.environment"/>
              </WeatherType>
            </Day>
            <Night/>
          </Land>
          <Sea/>
        </Weather>
        """;

    private static string CreateTempMapperDir(bool withWeatherFolder)
    {
        string dir = Path.Combine(Path.GetTempPath(), "WeatherDataTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        if (withWeatherFolder)
        {
            string weatherDir = Path.Combine(dir, "Weather");
            Directory.CreateDirectory(weatherDir);
            File.WriteAllText(Path.Combine(weatherDir, "test.xml"), WeatherXml);
        }
        return dir;
    }

    [Fact]
    public void LoadFromMapper_ParsesWeatherFolder_ReturnsEnvironments()
    {
        string dir = CreateTempMapperDir(withWeatherFolder: true);
        try
        {
            WeatherData.Instance.LoadFromMapper(dir);

            var summer = WeatherData.Instance.GetEnvironments("summer");
            Assert.Equal(2, summer.Count);
            Assert.Equal(
                new[] { "weather\\summer\\sunny_fields_day.environment", "weather\\summer\\sunny_forest_day.environment" },
                summer.Select(e => e.Key).ToArray());
            Assert.All(summer, e => Assert.Equal("summer", e.Ck3Type));

            var harshWinter = WeatherData.Instance.GetEnvironments("harsh_winter");
            Assert.Single(harshWinter);
            Assert.Equal("weather\\winter\\snow_heavy_day.environment", harshWinter[0].Key);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetEnvironments_UnknownType_ReturnsEmptyList()
    {
        string dir = CreateTempMapperDir(withWeatherFolder: true);
        try
        {
            WeatherData.Instance.LoadFromMapper(dir);

            var result = WeatherData.Instance.GetEnvironments("nonexistent_type");
            Assert.NotNull(result);
            Assert.Empty(result);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void LoadFromMapper_WithoutWeatherFolder_ClearsPriorData()
    {
        string populatedDir = CreateTempMapperDir(withWeatherFolder: true);
        string emptyDir = CreateTempMapperDir(withWeatherFolder: false);
        try
        {
            WeatherData.Instance.LoadFromMapper(populatedDir);
            Assert.NotEmpty(WeatherData.Instance.GetEnvironments("summer"));

            // Loading a mapper with no Weather subfolder must clear previously loaded data.
            WeatherData.Instance.LoadFromMapper(emptyDir);
            Assert.Empty(WeatherData.Instance.GetEnvironments("summer"));
            Assert.Empty(WeatherData.Instance.GetEnvironments("harsh_winter"));
        }
        finally
        {
            Directory.Delete(populatedDir, recursive: true);
            Directory.Delete(emptyDir, recursive: true);
        }
    }
}
