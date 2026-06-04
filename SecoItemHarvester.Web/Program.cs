using System.Net;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Extensions.Http;
using SecoItemHarvester.Web.Data;
using SecoItemHarvester.Web.Models;
using SecoItemHarvester.Web.Dtos;
using SecoItemHarvester.Web.Options;
using SecoItemHarvester.Web.Repositories;
using SecoItemHarvester.Web.Services;
using SecoItemHarvester.Web.Validators;
using SecoItemHarvester.Web.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<DeepSeekOptions>(builder.Configuration.GetSection(DeepSeekOptions.SectionName));
builder.Services.Configure<ProcessingOptions>(builder.Configuration.GetSection(ProcessingOptions.SectionName));

builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddValidatorsFromAssemblyContaining<UploadItemsRequestValidator>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=seco-harvester.db";

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddScoped<IItemLookupRepository, ItemLookupRepository>();
builder.Services.AddScoped<IItemLookupService, ItemLookupService>();
builder.Services.AddSingleton<IItemInputParser, ItemInputParser>();
builder.Services.AddSingleton<ICsvExportService, CsvExportService>();
builder.Services.AddSingleton<IProcessingTrigger, ProcessingTrigger>();
builder.Services.AddHostedService<ItemProcessingWorker>();

var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(500 * attempt));

builder.Services.AddHttpClient<ISecoProductScraper, SecoProductScraper>()
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        AutomaticDecompression = DecompressionMethods.All,
        AllowAutoRedirect = true,
        PooledConnectionLifetime = TimeSpan.FromMinutes(5)
    })
    .ConfigureHttpClient(client =>
    {
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
        client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
    })
    .AddPolicyHandler(retryPolicy);

builder.Services.AddHttpClient<IDeepSeekClient, DeepSeekClient>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<DeepSeekOptions>>().Value;
    var baseUrl = string.IsNullOrWhiteSpace(options.BaseUrl)
        ? "http://localhost:11434"
        : options.BaseUrl.TrimEnd('/') + "/";
    client.BaseAddress = new Uri(baseUrl);
})
.AddPolicyHandler(retryPolicy);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    await db.ItemLookups
        .Where(x => x.Status == ItemLookupStatus.Processing)
        .ExecuteUpdateAsync(setters => setters
            .SetProperty(x => x.Status, ItemLookupStatus.Pending)
            .SetProperty(x => x.UpdatedUtc, DateTime.UtcNow));
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapRazorPages();
app.MapControllers();

app.Run();

public partial class Program;
