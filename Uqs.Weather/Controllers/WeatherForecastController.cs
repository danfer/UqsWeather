using AdamTibi.OpenWeather;
using Microsoft.AspNetCore.Mvc;
using Uqs.Weather.Wrappers;

namespace Uqs.Weather.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private const int FORECAST_DAYS = 5;
    private readonly IClient _client;
    private readonly ILogger<WeatherForecastController> _logger;
    private readonly IConfiguration _config;
    private readonly INowWrapper _nowWrapper;
    private readonly IRandomWrapper _randomWrapper;
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild",
        "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    public WeatherForecastController(IClient client,
            ILogger<WeatherForecastController> logger, IConfiguration config,
            INowWrapper nowWrapper, IRandomWrapper randomWrapper)
    {
        _client = client;
        _logger = logger;
        _config = config;
        _nowWrapper = nowWrapper;
        _randomWrapper = randomWrapper;
    }

    [HttpGet("ConvertCToF")]
    public double ConvertCToF(double c)
    {
        double f = c * (9d / 5d) + 32;
        _logger.LogInformation("conversion requested");
        return f;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public IEnumerable<WeatherForecast> Get()
    {
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateTime.Now.AddDays(index),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        })
        .ToArray();
    }

    [HttpGet("GetRealWeatherForecast")]
    public async Task<IEnumerable<WeatherForecast>> GetReal()
    {
        try
        {
            const decimal GREENWICH_LAT = 12.1508m;
            const decimal GREENWICH_LON = -86.2683m;
            string apiKey = _config["OpenWeather:Key"];
            HttpClient httpClient = new HttpClient();
            
            OneCallResponse res = await _client.OneCallAsync
                (GREENWICH_LAT, GREENWICH_LON, new[] {
                Excludes.Current, Excludes.Minutely,
                Excludes.Hourly, Excludes.Alerts }, Units.Metric);

            WeatherForecast[] wfs = new WeatherForecast[FORECAST_DAYS];
            for (int i = 0; i < wfs.Length; i++)
            {
                var wf = wfs[i] = new WeatherForecast();
                wf.Date = res.Daily[i + 1].Dt;
                double forecastedTemp = res.Daily[i + 1].Temp.Day;
                wf.TemperatureC = (int)Math.Round(forecastedTemp);
                wf.Summary = MapFeelToTemp(wf.TemperatureC);
            }
            return wfs;
        }
        catch (Exception ex)
        {
            return null;
        }       
    }

    [HttpGet("GetRandomWeatherForecast")]
    public IEnumerable<WeatherForecast> GetRandom()
    {
        WeatherForecast[] wfs = new WeatherForecast[FORECAST_DAYS];
        for (int i = 0; i < wfs.Length; i++)
        {
            var wf = wfs[i] = new WeatherForecast();
            wf.Date = _nowWrapper.Now.AddDays(i + 1);
            wf.TemperatureC = _randomWrapper.Next(-20, 55);
            wf.Summary = MapFeelToTemp(wf.TemperatureC);
        }
        return wfs;
    }

    private static string MapFeelToTemp(int temperatureC)
    {
        // Anything <= 0 is "Freezing"
        if (temperatureC <= 0)
        {
            return Summaries.First();
        }
        // Dividing the temperature into 5 intervals
        int summariesIndex = (temperatureC / 5) + 1;
        // Anything >= 45 is "Scorching"
        if (summariesIndex >= Summaries.Length)
        {
            return Summaries.Last();
        }
        return Summaries[summariesIndex];
    }
}
