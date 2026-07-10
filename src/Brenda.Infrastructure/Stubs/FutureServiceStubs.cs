using Brenda.Core.Abstractions.Future;

namespace Brenda.Infrastructure.Stubs;

// Placeholder implementations for roadmap v1.1/v2.0 modules. Registered in DI so the
// application shell can already surface "coming soon" pages; replace with real
// implementations as the roadmap progresses.

public sealed class RenderServiceStub : IRenderService
{
    public bool IsAvailable => false;
}

public sealed class GitServiceStub : IGitService
{
    public bool IsAvailable => false;
}

public sealed class AssetLibraryServiceStub : IAssetLibraryService
{
    public bool IsAvailable => false;
}
