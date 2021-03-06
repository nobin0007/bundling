﻿namespace Karambolo.AspNetCore.Bundling.Internal
{
    public interface IBundleModelFactory
    {
        IBundleModel Create(Bundle bundle);
        IBundleSourceModel CreateSource(BundleSource bundleSource);
    }
}
