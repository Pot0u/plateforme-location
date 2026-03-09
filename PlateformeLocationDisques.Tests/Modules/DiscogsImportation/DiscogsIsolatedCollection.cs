using PlateformeLocationDisques.Tests.Helpers;
using Xunit;

namespace PlateformeLocationDisques.Tests.Modules.DiscogsImportation;

[CollectionDefinition("Discogs Isolated Collection")]
public class DiscogsIsolatedCollection : ICollectionFixture<DiscogsIsolatedFixture>
{
}
