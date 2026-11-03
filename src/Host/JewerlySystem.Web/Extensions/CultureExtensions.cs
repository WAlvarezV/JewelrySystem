using System.Globalization;
using Microsoft.AspNetCore.Localization;

namespace JewerlySystem.Web.Extensions;

public static class CultureExtensions
{
    public static readonly CultureInfo ColombiaCulture = CreateColombiaCulture();

    private static CultureInfo CreateColombiaCulture()
    {
        CultureInfo culture;
        try
        {
            culture = (CultureInfo)CultureInfo.GetCultureInfo("es-CO").Clone();
        }
        catch
        {
            culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        }

        culture.NumberFormat.CurrencySymbol = "$";
        culture.NumberFormat.CurrencyPositivePattern = 0; // "$1.500.000"
        culture.NumberFormat.CurrencyNegativePattern = 1; // "-$1.500.000"
        culture.NumberFormat.CurrencyGroupSeparator = ".";
        culture.NumberFormat.CurrencyDecimalSeparator = ",";
        culture.NumberFormat.NumberGroupSeparator = ".";
        culture.NumberFormat.NumberDecimalSeparator = ",";

        return culture;
    }

    public static IServiceCollection AddColombiaLocalization(this IServiceCollection services)
    {
        CultureInfo.DefaultThreadCurrentCulture = ColombiaCulture;
        CultureInfo.DefaultThreadCurrentUICulture = ColombiaCulture;
        Thread.CurrentThread.CurrentCulture = ColombiaCulture;
        Thread.CurrentThread.CurrentUICulture = ColombiaCulture;

        services.Configure<RequestLocalizationOptions>(options =>
        {
            var supported = new[] { ColombiaCulture };
            options.DefaultRequestCulture = new RequestCulture(ColombiaCulture);
            options.SupportedCultures = supported;
            options.SupportedUICultures = supported;
        });

        return services;
    }

    public static IApplicationBuilder UseColombiaLocalization(this IApplicationBuilder app)
    {
        return app.UseRequestLocalization(new RequestLocalizationOptions
        {
            DefaultRequestCulture = new RequestCulture(ColombiaCulture),
            SupportedCultures = new[] { ColombiaCulture },
            SupportedUICultures = new[] { ColombiaCulture }
        });
    }
}
