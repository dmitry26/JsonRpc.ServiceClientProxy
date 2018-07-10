// Copyright (c) DMO Consulting LLC. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Community.AspNetCore.JsonRpc;
using ImpromptuInterface;
using Dmo.Reflection;

namespace Community.JsonRpc.ServiceClient
{	
	public static class JsonRpcServiceClientFactory
	{		
		private static Lazy<ConcurrentDictionary<Type,Lazy<ServiceContractInfo>>> _svcCtrInfoDict =
			new Lazy<ConcurrentDictionary<Type,Lazy<ServiceContractInfo>>>(() => new ConcurrentDictionary<Type,Lazy<ServiceContractInfo>>());
				
		public static T AsServiceContract<T>(this JsonRpcClient client)
			where T : class
		{
			_ = client ?? throw new ArgumentNullException(nameof(client));

			var svcCntr = _svcCtrInfoDict.Value.GetOrAdd(typeof(T),svcCntrType => new Lazy<ServiceContractInfo>(() =>
			{
				if (!svcCntrType.IsInterface)
					throw new InvalidOperationException($"{svcCntrType.Name}: is not an interface.");

				var res = new ServiceContractInfo
				{
					OperContracts = svcCntrType.GetMethodsFromInterface().Select(mi => new OperationContractInfo
					{
						MethodInfo = mi,
						MethodName = mi.GetCustomAttribute<AliasAttribute>()?.Name ?? mi.Name,
						RpcName =  mi.GetCustomAttribute<JsonRpcNameAttribute>()?.Value
					}).Where(x => x.RpcName != null).ToArray(),
					Disposable = typeof(IDisposable).IsAssignableFrom(svcCntrType)
				};

				if (res.OperContracts.Count == 0)
					throw new InvalidOperationException($"{svcCntrType.Name}: no operation contracts found.");

				return res;
			})).Value;	

			IDictionary<string,object> expando = new ExpandoObject();
	
			foreach (var oc in svcCntr.OperContracts)
			{
				expando.Add(oc.MethodName,GetOrAddMapFunc(oc.MethodInfo,oc.RpcName,client));
			}

			if (svcCntr.Disposable)
				expando.Add("Dispose",new Action(() => client.Dispose()));

			return expando.ActLike<T>();			
		}

		private struct ServiceContractInfo
		{
			public IReadOnlyList<OperationContractInfo> OperContracts;

			public bool Disposable;
		}

		private struct OperationContractInfo
		{
			public MethodInfo MethodInfo;

			public string MethodName;

			public string RpcName;
		}

		private class TargetInvokeInfo
		{
			public int ParamCount;

			public dynamic InvokeFunc;

			public bool HasCancelToken;
		}

		private static Lazy<ConcurrentDictionary<MethodInfo,Lazy<TargetInvokeInfo>>> _mapFuncDict =
			new Lazy<ConcurrentDictionary<MethodInfo,Lazy<TargetInvokeInfo>>>(() => new ConcurrentDictionary<MethodInfo,Lazy<TargetInvokeInfo>>());

		private static dynamic GetOrAddMapFunc(MethodInfo proxyMethodInfo,string proxyMethodName,object target)
		{
			var tgtInfo = _mapFuncDict.Value.GetOrAdd(proxyMethodInfo,key => new Lazy<TargetInvokeInfo>(() =>
			{
				var proxyReturnType = proxyMethodInfo.ReturnType;

				if (!(proxyReturnType == typeof(Task)) && !(proxyReturnType.IsGenericType && (proxyReturnType.GetGenericTypeDefinition() == typeof(Task<>))))
					throw new InvalidOperationException($"The method '{proxyMethodInfo.Name}' of the service '{proxyMethodInfo.DeclaringType.Name}' must return type a Task or Task<TResult>");

				var proxyParams = proxyMethodInfo.GetParameters();
				int proxyParamCount = proxyParams.Length;
				
				bool hasCancelToken;

				if (proxyParamCount > 0 && proxyParams[proxyParamCount - 1].ParameterType == typeof(CancellationToken))
				{
					--proxyParamCount;
					hasCancelToken = true;
				}
				else
					hasCancelToken = false;

				var tgtParamTypes = (proxyParamCount == 0)
					? new Type[] { typeof(string),typeof(CancellationToken) }
					: new Type[] { typeof(string),typeof(object[]),typeof(CancellationToken) };

				var genArgTypes = proxyReturnType.IsGenericType ? new Type[] { proxyReturnType.GetGenericArguments().Single() } : null;
				var lambdaExp = CreateLambdaExp(target.GetType(),"InvokeAsync",genArgTypes,tgtParamTypes);

				return new TargetInvokeInfo
				{
					HasCancelToken = hasCancelToken,
					ParamCount = proxyParamCount,
					InvokeFunc = (proxyParamCount > 0)
						? (dynamic)((Func<dynamic,dynamic,dynamic,dynamic,dynamic>)lambdaExp.Compile())
						: (dynamic)((Func<dynamic,dynamic,dynamic,dynamic>)lambdaExp.Compile())
				};
			})).Value;

			return tgtInfo.HasCancelToken
			  ? CreateMapFuncWithCancelToken(target,proxyMethodName,tgtInfo.InvokeFunc,tgtInfo.ParamCount)
			  : CreateMapFunc(target,proxyMethodName,tgtInfo.InvokeFunc,tgtInfo.ParamCount);
		}

		private static dynamic CreateMapFunc(object target,string proxyMethodName,dynamic func,int paramCount)
		{
			switch (paramCount)
			{
				case 0:
					return new Func<dynamic>(() =>
						func(target,proxyMethodName,default(CancellationToken)));
				case 1:
					return new Func<dynamic,dynamic>(p1 =>
						func(target,proxyMethodName,new object[] { p1 },default(CancellationToken)));
				case 2:
					return new Func<dynamic,dynamic,dynamic>((p1,p2) =>
						func(target,proxyMethodName,new object[] { p1,p2 },default(CancellationToken)));
				case 3:
					return new Func<dynamic,dynamic,dynamic,dynamic>((p1,p2,p3) =>
						func(target,proxyMethodName,new object[] { p1,p2,p3 },default(CancellationToken)));
				case 4:
					return new Func<dynamic,dynamic,dynamic,dynamic,dynamic>((p1,p2,p3,p4) =>
						func(target,proxyMethodName,new object[] { p1,p2,p3,p4 },default(CancellationToken)));
				case 5:
					return new Func<dynamic,dynamic,dynamic,dynamic,dynamic,dynamic>((p1,p2,p3,p4,p5) =>
						func(target,proxyMethodName,new object[] { p1,p2,p3,p4,p5 },default(CancellationToken)));
				case 6:
					return new Func<dynamic,dynamic,dynamic,dynamic,dynamic,dynamic,dynamic>((p1,p2,p3,p4,p5,p6) =>
						func(target,proxyMethodName,new object[] { p1,p2,p3,p4,p5,p6 },default(CancellationToken)));
				case 7:
					return new Func<dynamic,dynamic,dynamic,dynamic,dynamic,dynamic,dynamic,dynamic>((p1,p2,p3,p4,p5,p6,p7) =>
						func(target,proxyMethodName,new object[] { p1,p2,p3,p4,p5,p6,p7 },default(CancellationToken)));
				case 8:
					return new Func<dynamic,dynamic,dynamic,dynamic,dynamic,dynamic,dynamic,dynamic,dynamic>((p1,p2,p3,p4,p5,p6,p7,p8) =>
						func(target,proxyMethodName,new object[] { p1,p2,p3,p4,p5,p6,p7,p8 },default(CancellationToken)));
				default:
					throw new NotImplementedException("Only supports 8 parameters.");
			}
		}

		private static dynamic CreateMapFuncWithCancelToken(object target,string proxyMethodName,dynamic func,int paramCount)
		{
			switch (paramCount)
			{
				case 0:
					return new Func<dynamic,dynamic>((ct) =>
						func(target,proxyMethodName,ct));
				case 1:
					return new Func<dynamic,dynamic,dynamic>((p1,ct) =>
						func(target,proxyMethodName,new object[] { p1 },ct));
				case 2:
					return new Func<dynamic,dynamic,dynamic,dynamic>((p1,p2,ct) =>
						func(target,proxyMethodName,new object[] { p1,p2 },ct));
				case 3:
					return new Func<dynamic,dynamic,dynamic,dynamic,dynamic>((p1,p2,p3,ct) =>
						func(target,proxyMethodName,new object[] { p1,p2,p3 },ct));
				case 4:
					return new Func<dynamic,dynamic,dynamic,dynamic,dynamic,dynamic>((p1,p2,p3,p4,ct) =>
						func(target,proxyMethodName,new object[] { p1,p2,p3,p4 },ct));
				case 5:
					return new Func<dynamic,dynamic,dynamic,dynamic,dynamic,dynamic,dynamic>((p1,p2,p3,p4,p5,ct) =>
						func(target,proxyMethodName,new object[] { p1,p2,p3,p4,p5 },ct));
				case 6:
					return new Func<dynamic,dynamic,dynamic,dynamic,dynamic,dynamic,dynamic,dynamic>((p1,p2,p3,p4,p5,p6,ct) =>
						func(target,proxyMethodName,new object[] { p1,p2,p3,p4,p5,p6 },ct));
				case 7:
					return new Func<dynamic,dynamic,dynamic,dynamic,dynamic,dynamic,dynamic,dynamic,dynamic>((p1,p2,p3,p4,p5,p6,p7,ct) =>
						func(target,proxyMethodName,new object[] { p1,p2,p3,p4,p5,p6,p7 },ct));
				case 8:
					return new Func<dynamic,dynamic,dynamic,dynamic,dynamic,dynamic,dynamic,dynamic,dynamic,dynamic>((p1,p2,p3,p4,p5,p6,p7,p8,ct) =>
						func(target,proxyMethodName,new object[] { p1,p2,p3,p4,p5,p6,p7,p8 },ct));
				default:
					throw new NotImplementedException("Only supports 8 parameters.");
			}
		}

		private static LambdaExpression CreateLambdaExp(Type srcType,string methodName,Type[] genArgTypes,params Type[] paramTypes)
		{
			var srcAsObj = Expression.Parameter(typeof(object),"src");
			var src = Expression.Convert(srcAsObj,srcType);
			var paramsAsObjs = paramTypes.Select((t,i) => Expression.Parameter(typeof(object),"p" + (i + 1).ToString())).ToArray();
			var @params = paramsAsObjs.Zip(paramTypes,(a,t) => Expression.Convert(a,t)).ToArray();
			var methodCall = Expression.Call(src,methodName,genArgTypes,@params);
			return Expression.Lambda(methodCall,(new ParameterExpression[] { srcAsObj }).Concat(paramsAsObjs).ToArray());
		}
	}
}
