# Dockerized Testing

This colleaction of packages allows you to quickly spin up Docker resources to test your projects with.

Simply implement IClassFixture and then start the resource like this:

    public class RedisFixtureTests : IClassFixture<RedisFixture>
    {
        public RedisFixtureTests(RedisFixture redisFixture)
        {
            redisFixture.Start().Wait();
            var client = await ConnectionMultiplexer.ConnectAsync("localhost:" + fixture.Ports.Single());
        }
    }

You can also build and run instances of your own VS projects like this:

    public class ExampleProjectTests : IClassFixture<ExampleTestFixture>
    {
        private readonly ExampleTestFixture fixture;

        public ExampleProjectTests(ExampleTestFixture fixture)
        {
            fixture.Start(new ExampleTestFixtureOptions()).Wait();
        }
    }

    public class ExampleTestFixtureOptions : FixtureOptions
    {
        public override IDockerImageProvider ImageProvider { get; } = new DockerProjectImageProvider(nameof(ExampleProjectNs));
    }

    public class ExampleTestFixture : BaseFixture<ExampleTestFixtureOptions>
    {
        // etc
    }

Or you can build and run other dockerized resources like this:

    public class ReadDataFromDb : IClassFixture<MsSqlFixture>
    {
        public ReadDataFromDb(MsSqlFixture fixture)
        {
            fixture.Start(new CustomMsSqlFixtureOptions()).Wait();
        }
    }

    public class CustomMsSqlFixtureOptions : MsSqlFixtureOptions
    {
        public CustomMsSqlFixtureOptions()
        {
            ImageProvider = new DockerfileImageProvider("example_database", ".",
                new ImageBuildParameters
                {
                    BuildArgs = new Dictionary<string, string>()
                    {
                        {"PASSWORD", this.SaPassword},
                        {"DATABASE","test" }
                    }
                });
        }
        public override IDockerImageProvider ImageProvider { get; }
    }

The corresponding Dockerfile contains:

    LABEL project=example_database

Take a look at the Examples folder or the integration tests for each project for more examples.

Nuget packages are available here https://www.nuget.org/profiles/devjoes