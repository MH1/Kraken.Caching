# Kraken.Caching
[![Nuget](https://img.shields.io/nuget/v/Kraken.Caching.svg)](https://www.nuget.org/packages/Kraken.Caching/)
[![License: Apache 2.0](https://img.shields.io/badge/License-apachev2-brightgreen.svg)](LICENSE)
[![Author](https://img.shields.io/badge/author-Martin%20Hloušek-blue.svg)](http://www.hlousek.org)

Kraken.Caching is a library that generates proxy implementation to the services to the dependency injection and adds customizable caching components

### Supported platforms
- **.NET Standard 1.6** or later
- **.NET Framework 4.5** or later

### Download and install
Nuget package [Kraken.Caching](https://www.nuget.org/packages/Kraken.Caching/)

```
Install-Package Kraken.Caching
```

### Features
- Simple methods
- Cache handler interface
- Basic memory cache and scoped memory cache handlers
- Attribute to ignore the method parameters in the case if it doesn't affect the result

### Not supported yet - will be available soon
The following functionality is generated as a direct non-cached call of the original service.
- Methods with reference or output parameters
- Generic methods
- Properties - cached getters and setters with the cache invalidation
- Async methods
- void-returning methods to force invalidate cache
- IEnumerable, IAsyncEnumerable caching - optional with attribute annotation
- Invalidating of the cached data by tag groups
- Indirect call of local method throught the cached service
- Configuration - eg. maximum memory usage

### Usage
You have to register caching in the Startup.cs
```csharp
services.AddCaching();
```
And then you can register the service proxy
```csharp
services.AddCached<IService, Service>();
```

#### Cache configuration attributes

You have to use the Cached attribute to specify the cache handler to use and cache duration:
```csharp
[Cached(typeof(MemoryCache), 5)]
```
You can specify Cached at the following levels:
- assembly
- interface
- service
- method
The lower level configuration rewrites the general configuration. Eg.

You have the following line in the AssemblyInfo.cs:
```csharp
[Cached(typeof(MemoryCache), 5)]
```
And the following attribute in the interface:
```csharp
[Cached(typeof(ScopedMemoryCache), 2)]
```
Then the ScopedMemoryCache cache handler is used and cached data expires after 2 minutes.

If you specify global cache attribute settings, you can suppress the caching behaviour by [NonCached] attribute used at the level of methods or properties.

#### Ignore parameter
TBD

#### Cache key
TBD

#### Cache tags
TBD
