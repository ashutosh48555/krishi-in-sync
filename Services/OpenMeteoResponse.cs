using System.Text.Json.Serialization;

namespace AgriSyncIndia.Services;

public class OpenMeteoResponse
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("timezone")]
    public string Timezone { get; set; } = string.Empty;

    [JsonPropertyName("current")]
    public OpenMeteoCurrent? Current { get; set; }

    [JsonPropertyName("hourly")]
    public OpenMeteoHourly? Hourly { get; set; }
}

public class OpenMeteoCurrent
{
    [JsonPropertyName("time")]
    public string Time { get; set; } = string.Empty;

    [JsonPropertyName("temperature_2m")]
    public double Temperature2m { get; set; }

    [JsonPropertyName("relative_humidity_2m")]
    public double RelativeHumidity2m { get; set; }

    [JsonPropertyName("surface_pressure")]
    public double SurfacePressure { get; set; }

    [JsonPropertyName("wind_speed_10m")]
    public double WindSpeed10m { get; set; }

    [JsonPropertyName("soil_temperature_0cm")]
    public double SoilTemperature0cm { get; set; }

    [JsonPropertyName("soil_moisture_0_to_1cm")]
    public double SoilMoisture0To1cm { get; set; }
}

public class OpenMeteoHourly
{
    [JsonPropertyName("time")]
    public List<string> Time { get; set; } = new();

    [JsonPropertyName("temperature_2m")]
    public List<double> Temperature2m { get; set; } = new();

    [JsonPropertyName("relative_humidity_2m")]
    public List<double> RelativeHumidity2m { get; set; } = new();
}
