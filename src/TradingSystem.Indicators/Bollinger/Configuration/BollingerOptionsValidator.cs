using Microsoft.Extensions.Options;

namespace TradingSystem.Indicators.Bollinger.Configuration;

public sealed class BollingerOptionsValidator
    : IValidateOptions<BollingerOptions>
{
    public ValidateOptionsResult Validate(
        string? name,
        BollingerOptions options)
    {
        var errors = new List<string>();

        if (options.Symbols is null ||
            options.Symbols.Length == 0 ||
            options.Symbols.Any(string.IsNullOrWhiteSpace))
        {
            errors.Add(
                "Bollinger symbols are required and cannot contain empty values.");
        }

        if (options.Intervals is null ||
            options.Intervals.Length == 0 ||
            options.Intervals.Any(string.IsNullOrWhiteSpace))
        {
            errors.Add(
                "Bollinger intervals are required and cannot contain empty values.");
        }

        if (options.HistoryLimit <= 0)
            errors.Add("Bollinger HistoryLimit must be positive.");

        if (options.Bands is null || options.Bands.Count == 0)
        {
            errors.Add("At least one Bollinger band is required.");
        }
        else
        {
            foreach (var band in options.Bands)
            {
                if (string.IsNullOrWhiteSpace(band.Name))
                    errors.Add("Bollinger band Name is required.");

                if (band.Length <= 0)
                {
                    errors.Add(
                        $"Bollinger band '{band.Name}' Length must be positive.");
                }

                if (band.Multiplier <= 0)
                {
                    errors.Add(
                        $"Bollinger band '{band.Name}' Multiplier must be positive.");
                }

                if (!string.Equals(
                        band.Source,
                        "open",
                        StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(
                        band.Source,
                        "close",
                        StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add(
                        $"Bollinger band '{band.Name}' Source must be 'open' or 'close'.");
                }
            }

            var duplicateNames = options.Bands
                .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                .GroupBy(
                    x => x.Name,
                    StringComparer.OrdinalIgnoreCase)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToArray();

            if (duplicateNames.Length > 0)
            {
                errors.Add(
                    $"Bollinger band names must be unique. Duplicates: {string.Join(", ", duplicateNames)}.");
            }

            var maximumBandLength = options.Bands
                .Where(x => x.Length > 0)
                .Select(x => x.Length)
                .DefaultIfEmpty(0)
                .Max();

            if (maximumBandLength > 0 &&
                options.HistoryLimit < maximumBandLength)
            {
                errors.Add(
                    "Bollinger HistoryLimit must cover the longest configured band.");
            }
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}