// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using NuGet.Services.Hosting.Azure;

namespace NuGet.Services.Search
{
    public class SearchWorkerRole 
        : SingleServiceWorkerRole<SearchService>
    {
    }
}
