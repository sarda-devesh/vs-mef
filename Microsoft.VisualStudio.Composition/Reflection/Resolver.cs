﻿namespace Microsoft.VisualStudio.Composition.Reflection
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;

    public static class Resolver
    {
        private static readonly Dictionary<AssemblyName, Module> assemblyManifests = new Dictionary<AssemblyName, Module>(ByValueEquality.AssemblyName);

        public static Type Resolve(this TypeRef typeRef)
        {
            return typeRef == null ? null : typeRef.ResolvedType;
        }

        public static ConstructorInfo Resolve(this ConstructorRef constructorRef)
        {
            if (constructorRef.IsEmpty)
            {
                return null;
            }

            var manifest = GetManifest(constructorRef.DeclaringType.AssemblyName);
            return (ConstructorInfo)manifest.ResolveMethod(constructorRef.MetadataToken);
        }

        public static MethodInfo Resolve(this MethodRef methodRef)
        {
            if (methodRef.IsEmpty)
            {
                return null;
            }

            var manifest = GetManifest(methodRef.DeclaringType.AssemblyName);
            var method = (MethodInfo)manifest.ResolveMethod(methodRef.MetadataToken);
            if (methodRef.GenericMethodArguments.Length > 0)
            {
                var constructedMethod = method.MakeGenericMethod(methodRef.GenericMethodArguments.Select(Resolve).ToArray());
                return constructedMethod;
            }

            return method;
        }

        public static PropertyInfo Resolve(this PropertyRef propertyRef)
        {
            if (propertyRef.IsEmpty)
            {
                return null;
            }

            Type type = Resolve(propertyRef.DeclaringType);
            return type.GetRuntimeProperties().First(p => p.MetadataToken == propertyRef.MetadataToken);
        }

        public static MethodInfo ResolveGetter(this PropertyRef propertyRef)
        {
            if (propertyRef.GetMethodMetadataToken.HasValue)
            {
                Module manifest = GetManifest(propertyRef.DeclaringType.AssemblyName);
                return (MethodInfo)manifest.ResolveMethod(propertyRef.GetMethodMetadataToken.Value);
            }

            return null;
        }

        public static MethodInfo ResolveSetter(this PropertyRef propertyRef)
        {
            if (propertyRef.SetMethodMetadataToken.HasValue)
            {
                Module manifest = GetManifest(propertyRef.DeclaringType.AssemblyName);
                return (MethodInfo)manifest.ResolveMethod(propertyRef.SetMethodMetadataToken.Value);
            }

            return null;
        }

        public static FieldInfo Resolve(this FieldRef fieldRef)
        {
            if (fieldRef.IsEmpty)
            {
                return null;
            }

            var manifest = GetManifest(fieldRef.AssemblyName);
            return manifest.ResolveField(fieldRef.MetadataToken);
        }

        public static ParameterInfo Resolve(this ParameterRef parameterRef)
        {
            if (parameterRef.IsEmpty)
            {
                return null;
            }

            Module manifest = GetManifest(parameterRef.AssemblyName);
            MethodBase method = manifest.ResolveMethod(parameterRef.MethodMetadataToken);
            return method.GetParameters()[parameterRef.ParameterIndex];
        }

        public static MemberInfo Resolve(this MemberRef memberRef)
        {
            if (memberRef.IsEmpty)
            {
                return null;
            }

            if (memberRef.IsField)
            {
                return memberRef.Field.Resolve();
            }

            if (memberRef.IsProperty)
            {
                return memberRef.Property.Resolve();
            }

            if (memberRef.IsMethod)
            {
                return memberRef.Method.Resolve();
            }

            if (memberRef.IsConstructor)
            {
                return memberRef.Constructor.Resolve();
            }

            throw new NotSupportedException();
        }

        public static MemberInfo Resolve(this MemberDesc memberDesc)
        {
            var fieldDesc = memberDesc as FieldDesc;
            if (fieldDesc != null)
            {
                return fieldDesc.Field.Resolve();
            }

            var propertyDesc = memberDesc as PropertyDesc;
            if (propertyDesc != null)
            {
                return propertyDesc.Property.Resolve();
            }

            var methodDesc = memberDesc as MethodDesc;
            if (methodDesc != null)
            {
                return methodDesc.Method.Resolve();
            }

            var constructorDesc = memberDesc as ConstructorDesc;
            if (constructorDesc != null)
            {
                return constructorDesc.Constructor.Resolve();
            }

            throw new NotSupportedException();
        }

        internal static Module GetManifest(AssemblyName assemblyName)
        {
            Module module;
            lock (assemblyManifests)
            {
                assemblyManifests.TryGetValue(assemblyName, out module);
            }

            if (module == null)
            {
                var assembly = Assembly.Load(assemblyName);
                module = assembly.ManifestModule;

                lock (assemblyManifests)
                {
                    assemblyManifests[assemblyName] = module;
                }
            }

            return module;
        }
    }
}
