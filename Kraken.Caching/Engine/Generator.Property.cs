using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Kraken.Caching.Engine
{
	internal partial class Generator
	{
		private void GenerateProperty(GeneratorContext ctx, PropertyInfo property)
		{
			PropertyInfo propertyImpl = GetImplementation(ctx.ServiceType, ctx.ImplementationType, property);

			PropertyBuilder propertyBuilder = ctx.TypeBuilder.DefineProperty(
				property.Name,
				property.Attributes,
				property.PropertyType,
				property.GetIndexParameters().Select(o => o.ParameterType).ToArray());

			// cache handler discovery
			Type cacheHandler = FindAttribute<CachedAttribute>(ctx.ServiceType, ctx.ImplementationType, property)?.Manager;

			if (property.CanRead)
			{
				MethodBuilder getter = GenerateMethod(ctx, property.GetMethod, property);
				propertyBuilder.SetGetMethod(getter);
			}
			if (property.CanWrite)
			{
				MethodBuilder setter = GenerateMethod(ctx, property.SetMethod, property);
				propertyBuilder.SetSetMethod(setter);
			}
		}
	}
}
