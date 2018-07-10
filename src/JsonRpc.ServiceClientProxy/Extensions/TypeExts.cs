// Copyright (c) DMO Consulting LLC. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Dmo.Reflection
{
	public static class TypeExts
	{
		private const BindingFlags DefaultBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;

		/// <summary>
		/// Searches for the public instance method whose parameters match the specified criteria.
		/// </summary>
		/// <param name="source">The type of the source.</param>
		/// <param name="name">The string containing the name of the public instance method to get.</param>
		/// <param name="paramTypes">An array of Type objects representing the number, order, and type of the parameters for the public method to get.</param>
		/// <param name="genMethod">Indicates whether the method is generic.</param>
		/// <returns>An object representing the method, if found; otherwise, null.</returns>
		public static MethodInfo GetMethod(this Type source,string name,Type[] paramTypes,bool genMethod) =>
			GetMethod(source,name,DefaultBindingFlags,paramTypes,genMethod);

		/// <summary>
		/// Searches for the method whose parameters match the specified criteria.
		/// </summary>
		/// <param name="source">The type of the source.</param>
		/// <param name="name">The string containing the name of the method to get.</param>
		/// <param name="flags">A bitmask comprised of one or more BindingFlags that specify how to search.</param>
		/// <param name="paramTypes">An array of Type objects representing the number, order, and type of the parameters for the method to get.</param>
		/// <param name="genMethod">Indicates whether the method is generic.</param>
		/// <returns>An object representing the method, if found; otherwise, null.</returns>
		public static MethodInfo GetMethod(this Type source,string name,BindingFlags flags,Type[] paramTypes,bool genMethod) =>
			(source ??  throw new ArgumentNullException(nameof(source)))
			.GetMethods(flags)
			.Where(mi => mi.Name == name
				&& !(mi.IsGenericMethod ^ genMethod)
				&& mi.GetParameters()
				.Select(x => x.ParameterType)
				.SequenceEqual(paramTypes ?? new Type[0],new EqualityComparerWrapper<Type>((x,y) => x?.IsAssignableFrom(y) ?? x == y)))
			.SingleOrDefault();

		/// <summary>
		/// Enumerates all the methods for the given interface type and inherited ones.
		/// </summary>
		/// <param name="ifcType">The type of the interface.<</param>
		/// <param name="ifcAttr">Interface custom attribute.</param>
		/// <returns>An enumerable object that contains the methods.</returns>
		public static IEnumerable<MethodInfo> GetMethodsFromInterface(this Type ifcType,Type ifcAttr = null) =>		
			(ifcType ?? throw new ArgumentNullException(nameof(ifcAttr)))
			.IsInterface
				? GetMethodsFromInterfaceIntern(ifcType,ifcAttr)
				: throw new ArgumentException($"'{nameof(ifcType)}' is not an interface type");

		private static IEnumerable<MethodInfo> GetMethodsFromInterfaceIntern(Type ifcType,Type ifcAttr = null)
		{
			foreach (var mi in ifcType.GetMethods())
			{
				yield return mi;
			}

			foreach (var ifc in ifcType.GetInterfaces())
			{
				if (ifcAttr != null && ifcType.GetCustomAttribute(ifcAttr) == null)
					continue;

				foreach (var mi in GetMethodsFromInterface(ifc,ifcAttr))
				{
					yield return mi;
				}
			}
		}
	}

	/// <summary>
	/// Wraps a delegate in an IEqualityComparer
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class EqualityComparerWrapper<T> : IEqualityComparer<T>
	{
		public EqualityComparerWrapper(Func<T,T,bool> equals,Func<T,int> hashCodeGen = null)
		{
			_equals = equals ?? throw new ArgumentNullException(nameof(equals));
			_hashCodeGen = hashCodeGen ?? new Func<T,int>(x => x?.GetHashCode() ?? throw new ArgumentNullException(nameof(x)));
		}

		private readonly Func<T,T,bool> _equals;

		private readonly Func<T,int> _hashCodeGen;

		#region IEqualityComparer<T> Members

		public bool Equals(T x,T y) => _equals(x,y);

		public int GetHashCode(T obj) => _hashCodeGen(obj);

		#endregion
	}
}
