using IdGen;
using IdGen.DependencyInjection;
using System.Text.Json.Serialization.Metadata;
using WebUI.Domain;
using WebUI.Domain.ObjectStore;
using WebUI.Domain.ObjectStore.Internal;
using WebUI.Endpoints;
using WebUI.Endpoints.Resources.Interfaces;
using WebUI.JsonConverters;
using WebUI.Model.Hypermedia;
using WebUI.Types;

namespace WebUI;

public delegate long GenerateId();

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var startup = new Startup(builder.Configuration, builder.Environment);
        startup.ConfigureServices(builder.Services);
        var app = builder.Build();
        startup.Configure(app, app.Environment);
        app.Run();
    }

    class Startup(IConfiguration configuration, IWebHostEnvironment environment)
    {
        public IConfiguration Configuration { get; } = configuration;
        public IWebHostEnvironment Environment { get; } = environment;

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddSingleton(sp => TimeProvider.System);
            services.AddIdGen(1);
            services.AddHypermediaUriGenerator();
            services.AddSingleton<GenerateId>(sp => sp.GetRequiredService<IIdGenerator<long>>().CreateId);
            services.AddObjectStore(opts => opts.UseInMemoryDb()
                .Configure<Championship>(ConfigureCollection.Championships)
                .Configure<Track>(ConfigureCollection.Tracks)
                .ConfigureJson(opts => opts.Converters.Add(new DistanceJsonConverter())));

            services.ConfigureHttpJsonOptions(opts =>
            {
                static void IgnoreVersionedFields(JsonTypeInfo typeInfo)
                {
                    if (typeInfo.Kind is JsonTypeInfoKind.Object && typeInfo.Type.GetInterface(nameof(IVersioned)) is not null)
                    {
                        var versionProperty = typeInfo.Properties.FirstOrDefault(p => p.Name.Equals(nameof(IVersioned.Version), StringComparison.OrdinalIgnoreCase));
                        typeInfo.Properties.Remove(versionProperty!); // This actually works with a null value despite the annotation.
                    }
                }

                opts.SerializerOptions.Converters.Add(new ParseAndFormatJsonConverter<ChampionshipId>("BASE36"));
                opts.SerializerOptions.Converters.Add(new ParseAndFormatJsonConverter<TrackId>("BASE36"));
                opts.SerializerOptions.Converters.Add(new HypermediaJsonConverterFactory());
                opts.SerializerOptions.Converters.Add(new DistanceJsonConverter());
                opts.SerializerOptions.TypeInfoResolver = new DefaultJsonTypeInfoResolver
                {
                    Modifiers = { IgnoreVersionedFields }
                };
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            app.Map("/api", api =>
            {

                api.UseRouting();
                api.UseEndpoints(endpoints => endpoints.MapFormulaApi(Environment));
            });
        }
    }

    static class ConfigureCollection
    {
        public static void Championships(ObjectCollectionOptions<Championship> options)
        {
            options.AddKey(nameof(Championship.ChampionshipId), c => c.ChampionshipId.Value);
        }

        public static void Tracks(ObjectCollectionOptions<Track> options)
        {
            options.AddKey(nameof(Track.ChampionshipId), t => t.ChampionshipId.Value);
            options.AddKey(nameof(Track.TrackId), t => t.TrackId.Value);
        }
    }
}
