using IdGen;
using IdGen.DependencyInjection;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization.Metadata;
using WebUI.Configuration;
using WebUI.Domain;
using WebUI.Domain.ObjectStore;
using WebUI.Domain.ObjectStore.Internal;
using WebUI.Endpoints;
using WebUI.Endpoints.Resources.Interfaces;
using WebUI.Features;
using WebUI.Filters;
using WebUI.JsonConverters;
using WebUI.Model.Hypermedia;
using WebUI.Types;
using EventId = WebUI.Types.EventId;

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
            services.AddObjectStore()
                .UseInMemoryDb()
                .ConfigureCollections(ConfigureCollection.ConfigureAll);

            services.ConfigureOptions<ConfigureHttpJsonOptions>();

            services.ConfigureFeatures()
                .Register<FlatDriverSkillFeature>();
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

            app.Map("/api",
                api => api.UseRouting().UseEndpoints(endpoints => endpoints.MapFormulaApi(Environment)));
        }
    }
    private class ConfigureHttpJsonOptions(FeatureRegistry featureRegistry) : IConfigureOptions<JsonOptions>, IConfigureOptions<ObjectStoreJsonOptions>
    {
        readonly FeatureRegistry _featureRegistry = featureRegistry;

        public void Configure(JsonOptions opts)
        {
            static void IgnoreVersionedFields(JsonTypeInfo typeInfo)
            {
                if (typeInfo.Kind is JsonTypeInfoKind.Object && typeInfo.Type.GetInterface(nameof(IVersioned)) is not null)
                {
                    var versionProperty = typeInfo.Properties.FirstOrDefault(p => p.Name.Equals(nameof(IVersioned.Version), StringComparison.OrdinalIgnoreCase));
                    typeInfo.Properties.Remove(versionProperty!); // This actually works with a null value despite the annotation.
                }
            }

            static void IgnoreValidationPropertyNameFields(JsonTypeInfo typeInfo)
            {
                if (typeInfo.Kind is JsonTypeInfoKind.Object && typeInfo.Type == typeof(ValidationMessage))
                {
                    var propertyNameProperty = typeInfo.Properties.FirstOrDefault(p => p.Name.Equals(nameof(ValidationMessage.PropertyName), StringComparison.OrdinalIgnoreCase));
                    typeInfo.Properties.Remove(propertyNameProperty!); // This actually works with a null value despite the annotation.
                }
            }

            opts.SerializerOptions.Converters.Add(new ParseAndFormatJsonConverter<ChampionshipId>("BASE36"));
            opts.SerializerOptions.Converters.Add(new ParseAndFormatJsonConverter<TrackId>("BASE36"));
            opts.SerializerOptions.Converters.Add(new ParseAndFormatJsonConverter<TeamId>("BASE36"));
            opts.SerializerOptions.Converters.Add(new ParseAndFormatJsonConverter<DriverId>("BASE36"));
            opts.SerializerOptions.Converters.Add(new ParseAndFormatJsonConverter<EventId>("BASE36"));
            opts.SerializerOptions.Converters.Add(new ParseAndFormatJsonConverter<SessionId>("BASE36"));

            opts.SerializerOptions.Converters.Add(new DistanceJsonConverter());
            opts.SerializerOptions.Converters.Add(new ColorJsonConverter());

            opts.SerializerOptions.Converters.Add(new FeatureCollectionJsonConverter(_featureRegistry));
            opts.SerializerOptions.Converters.Add(new FeatureDataCollectionJsonConverter<IFeatureDriverData>(_featureRegistry, reg => reg.DriverData));
            opts.SerializerOptions.Converters.Add(new FeatureDataCollectionJsonConverter<IFeatureTrackData>(_featureRegistry, reg => reg.TrackData));
            opts.SerializerOptions.Converters.Add(new FeatureDataCollectionJsonConverter<IFeatureTeamData>(_featureRegistry, reg => reg.TeamData));

            opts.SerializerOptions.Converters.Add(new HypermediaJsonConverterFactory());
            opts.SerializerOptions.Converters.Add(new ValidationMessagesJsonConverter());
            opts.SerializerOptions.Converters.Add(new InputJsonConverterFactory());

            opts.SerializerOptions.TypeInfoResolver = new DefaultJsonTypeInfoResolver
            {
                Modifiers = { IgnoreVersionedFields, IgnoreValidationPropertyNameFields }
            };
        }

        public void Configure(ObjectStoreJsonOptions opts)
        {
            opts.SerializerOptions.Converters.Add(new DistanceJsonConverter());
            opts.SerializerOptions.Converters.Add(new FeatureCollectionJsonConverter(_featureRegistry));
            opts.SerializerOptions.Converters.Add(new FeatureDataCollectionJsonConverter<IFeatureDriverData>(_featureRegistry, reg => reg.DriverData));
            opts.SerializerOptions.Converters.Add(new FeatureDataCollectionJsonConverter<IFeatureTrackData>(_featureRegistry, reg => reg.TrackData));
            opts.SerializerOptions.Converters.Add(new FeatureDataCollectionJsonConverter<IFeatureTeamData>(_featureRegistry, reg => reg.TeamData));
        }
    }

    static class ConfigureCollection
    {
        public static void ConfigureAll(ObjectStoreCollectionOptions options)
        {
            options.ConfigureCollection<Championship>(Championships);
            options.ConfigureCollection<Track>(Tracks);
            options.ConfigureCollection<Team>(Teams);
            options.ConfigureCollection<Driver>(Drivers);
            options.ConfigureCollection<Event>(Events);
            options.ConfigureCollection<Session>(Sessions);
        }

        public static void Championships(ObjectCollectionOptions<Championship> options)
        {
            options.AddKey(nameof(Championship.ChampionshipId), c => c.ChampionshipId.Value);
        }

        public static void Tracks(ObjectCollectionOptions<Track> options)
        {
            options.AddKey(nameof(Track.ChampionshipId), t => t.ChampionshipId.Value);
            options.AddKey(nameof(Track.TrackId), t => t.TrackId.Value);
        }

        public static void Teams(ObjectCollectionOptions<Team> options)
        {
            options.AddKey(nameof(Team.ChampionshipId), t => t.ChampionshipId.Value);
            options.AddKey(nameof(Team.TeamId), t => t.TeamId.Value);
        }

        public static void Drivers(ObjectCollectionOptions<Driver> options)
        {
            options.AddKey(nameof(Driver.ChampionshipId), t => t.ChampionshipId.Value);
            options.AddKey(nameof(Driver.DriverId), t => t.DriverId.Value);
        }

        public static void Events(ObjectCollectionOptions<Event> options)
        {
            options.AddKey(nameof(Event.ChampionshipId), t => t.ChampionshipId.Value);
            options.AddKey(nameof(Event.EventId), t => t.EventId.Value);
        }

        public static void Sessions(ObjectCollectionOptions<Session> options)
        {
            options.AddKey(nameof(Session.ChampionshipId), t => t.ChampionshipId.Value);
            options.AddKey(nameof(Session.EventId), t => t.EventId.Value);
            options.AddKey(nameof(Session.SessionId), t => t.SessionId.Value);
        }
    }
}
