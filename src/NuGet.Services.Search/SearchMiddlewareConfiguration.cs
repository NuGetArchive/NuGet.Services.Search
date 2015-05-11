// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using Microsoft.WindowsAzure.Storage;
using NuGet.Services.Configuration;
using System;

namespace NuGet.Services.Search
{
    public class SearchMiddlewareConfiguration
    {
        private readonly ConfigurationHub _config;

        public bool UseStorage { get; private set; }
        public string LocalIndexPath { get; private set; }
        public CloudStorageAccount StorageAccount { get; private set; }
        public string StorageContainer { get; private set; }

        public SearchMiddlewareConfiguration(ConfigurationHub config)
        {
            _config = config;
            Reload();
        }

        public void Reload()
        {
            var section = _config.GetSection<SearchConfiguration>();
            if (String.IsNullOrEmpty(section.IndexPath))
            {
                UseStorage = true;
                StorageAccount = _config.Storage.Primary;
                StorageContainer = "ng-search";
                LocalIndexPath = null;
            }
            else
            {
                UseStorage = false;
                StorageAccount = null;
                StorageContainer = null;
                LocalIndexPath = section.IndexPath;
            }
        }
    }
}
