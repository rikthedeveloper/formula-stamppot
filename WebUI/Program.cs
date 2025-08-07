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
using WebUI.Model.PointsSystems;
using WebUI.Model.StartingOrderStrategies;
using WebUI.Types;
using WebUI.Types.Internal;
using EventId = WebUI.Types.EventId;

namespace WebUI;

public delegate long GenerateId();

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton(sp => TimeProvider.System);
        builder.Services.AddIdGen(1);
        builder.Services.AddHypermediaUriGenerator();
        builder.Services.AddSingleton<GenerateId>(sp => sp.GetRequiredService<IIdGenerator<long>>().CreateId);
        builder.Services.AddObjectStore()
            .UseInMemoryDb()
            .ConfigureCollections(ConfigureCollection.ConfigureAll);

        builder.Services.ConfigureOptions<ConfigureJsonOptions>();

        builder.Services.ConfigureFeatures()
            .Register<FlatDriverSkillFeature>();

        builder.Services.AddEndpointsApiExplorer().AddSwaggerGen();

        var app = builder.Build(); 
        
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        //app.UseStaticFiles();

        app.Map("/api", api =>
        {
            api.UseSwagger();
            api.UseSwaggerUI(configure => configure.RoutePrefix = "swagger");
            api.UseReDoc(configure => configure.RoutePrefix = "redoc");

            api.UseRouting().UseEndpoints(endpoints => endpoints.MapFormulaApi(app.Environment.IsDevelopment()));
        });

        app.Run();
    }

    class ConfigureJsonOptions(FeatureRegistry featureRegistry) : IConfigureOptions<JsonOptions>, IConfigureOptions<ObjectStoreJsonOptions>
    {
        readonly FeatureRegistry _featureRegistry = featureRegistry;
        readonly Action<JsonTypeInfo> _configurePointsSystemInheritance = ConfigurePolymorphicSerialization<IPointsSystem>(derivedTypes: [
            new(typeof(PositionPointsSystem), nameof(PositionPointsSystem))
        ]);

        readonly Action<JsonTypeInfo> _configureStartingOrderInheritance = ConfigurePolymorphicSerialization<IStartingOrderStrategy>(derivedTypes: [
            new(typeof(NoopStartingOrderStrategy), nameof(NoopStartingOrderStrategy)),
            new(typeof(OtherSessionResultsStartingOrderStrategy), nameof(OtherSessionResultsStartingOrderStrategy))
        ]);

        /// <summary>
        /// Configures the JSON serialization options for the HTTP API.
        /// </summary>
        public void Configure(JsonOptions opts)
        {
            // Serialize IDs to Base36 strings because JavaScript cannot handle 64-bit integers properly.
            opts.SerializerOptions.Converters.Add(new Base36IdConverter<ChampionshipId>());
            opts.SerializerOptions.Converters.Add(new Base36IdConverter<TrackId>());
            opts.SerializerOptions.Converters.Add(new Base36IdConverter<TeamId>());
            opts.SerializerOptions.Converters.Add(new Base36IdConverter<DriverId>());
            opts.SerializerOptions.Converters.Add(new Base36IdConverter<EventId>());
            opts.SerializerOptions.Converters.Add(new Base36IdConverter<SessionId>());

            opts.SerializerOptions.Converters.Add(new DistanceJsonConverter());
            opts.SerializerOptions.Converters.Add(new ColorJsonConverter());

            opts.SerializerOptions.Converters.Add(new FeatureCollectionJsonConverter(_featureRegistry));
            opts.SerializerOptions.Converters.Add(new FeatureDataCollectionJsonConverter<IFeatureDriverData>(_featureRegistry, reg => reg.DriverData));
            opts.SerializerOptions.Converters.Add(new FeatureDataCollectionJsonConverter<IFeatureTrackData>(_featureRegistry, reg => reg.TrackData));
            opts.SerializerOptions.Converters.Add(new FeatureDataCollectionJsonConverter<IFeatureTeamData>(_featureRegistry, reg => reg.TeamData));

            opts.SerializerOptions.Converters.Add(new HypermediaJsonConverterFactory());
            opts.SerializerOptions.Converters.Add(new ValidationMessagesJsonConverter());

            opts.SerializerOptions.TypeInfoResolver = new DefaultJsonTypeInfoResolver
            {
                Modifiers = {
                    IgnoreVersionedFields,
                    IgnoreValidationPropertyNameFields,
                    _configurePointsSystemInheritance,
                    _configureStartingOrderInheritance
                }
            };
        }

        /// <summary>
        /// Configures the JSON serialization options for the ObjectStore.
        /// </summary>
        public void Configure(ObjectStoreJsonOptions opts)
        {
            opts.SerializerOptions.Converters.Add(new DistanceJsonConverter());
            opts.SerializerOptions.Converters.Add(new FeatureCollectionJsonConverter(_featureRegistry));
            opts.SerializerOptions.Converters.Add(new FeatureDataCollectionJsonConverter<IFeatureDriverData>(_featureRegistry, reg => reg.DriverData));
            opts.SerializerOptions.Converters.Add(new FeatureDataCollectionJsonConverter<IFeatureTrackData>(_featureRegistry, reg => reg.TrackData));
            opts.SerializerOptions.Converters.Add(new FeatureDataCollectionJsonConverter<IFeatureTeamData>(_featureRegistry, reg => reg.TeamData));
            opts.SerializerOptions.TypeInfoResolver = new DefaultJsonTypeInfoResolver
            {
                Modifiers = {
                    _configurePointsSystemInheritance,
                    _configureStartingOrderInheritance
                }
            };
        }

        static void IgnoreVersionedFields(JsonTypeInfo typeInfo)
        {
            if (typeInfo.Kind is JsonTypeInfoKind.Object && typeInfo.Type.GetInterface(nameof(IVersioned)) is not null)
            {
                var versionProperty = typeInfo.Properties.FirstOrDefault(p => p.Name.Equals(nameof(IVersioned.Version), StringComparison.OrdinalIgnoreCase));
                if (versionProperty is not null)
                    typeInfo.Properties.Remove(versionProperty);
            }
        }

        static void IgnoreValidationPropertyNameFields(JsonTypeInfo typeInfo)
        {
            if (typeInfo.Kind is JsonTypeInfoKind.Object && typeInfo.Type == typeof(ValidationMessage))
            {
                var propertyNameProperty = typeInfo.Properties.FirstOrDefault(p => p.Name.Equals(nameof(ValidationMessage.PropertyName), StringComparison.OrdinalIgnoreCase));
                if (propertyNameProperty is not null)
                    typeInfo.Properties.Remove(propertyNameProperty);
            }
        }

        static Action<JsonTypeInfo> ConfigurePolymorphicSerialization<T>(
            string typeDiscriminatorPropertyName = "name",
            IEnumerable<JsonDerivedType>? derivedTypes = null) => typeInfo =>
        {
            if (typeInfo.Kind is JsonTypeInfoKind.Object && typeInfo.Type != typeof(T))
                return;

            typeInfo.PolymorphismOptions = new JsonPolymorphismOptions
            {
                TypeDiscriminatorPropertyName = typeDiscriminatorPropertyName,
                IgnoreUnrecognizedTypeDiscriminators = false,
                UnknownDerivedTypeHandling = System.Text.Json.Serialization.JsonUnknownDerivedTypeHandling.FailSerialization
            };

            if (derivedTypes is not null)
                foreach (var derivedType in derivedTypes)
                    typeInfo.PolymorphismOptions.DerivedTypes.Add(derivedType);
        };

        class Base36IdConverter<TId>()
            : ParseAndFormatJsonConverter<TId>("BASE36") where TId : IFormattable, ITryParseable<string, TId>
        { }
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
