using PlateformeLocationDisques.Tests.Helpers;
using Xunit;

namespace PlateformeLocationDisques.Tests.Modules.DiscogsImportation;

[CollectionDefinition("Discogs Read-Only Collection")]
public class DiscogsReadOnlyCollection : ICollectionFixture<DiscogsReadOnlyFixture>
{
}
