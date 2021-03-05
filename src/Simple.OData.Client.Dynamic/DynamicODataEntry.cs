using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Simple.OData.Client.Extensions;

#pragma warning disable 1591

namespace Simple.OData.Client
{
    public class DynamicODataEntry : ODataEntry, IDynamicMetaObjectProvider
    {
        internal DynamicODataEntry()
        {
        }

        internal DynamicODataEntry(IDictionary<string, object> entry, ITypeCache typeCache) : base(ToDynamicODataEntry(entry, typeCache))
        {
            TypeCache = typeCache;
        }

        internal ITypeCache TypeCache { get; }

        private static IDictionary<string, object> ToDynamicODataEntry(IDictionary<string, object> entry, ITypeCache typeCache)
        {
            return entry == null
                ? null
                : entry.ToDictionary(
                        x => x.Key,
                        y => y.Value is IDictionary<string, object>
                            ? new DynamicODataEntry(y.Value as IDictionary<string, object>, typeCache)
                            : y.Value is IEnumerable<object>
                            ? ToDynamicODataEntry(y.Value as IEnumerable<object>, typeCache)
                            : y.Value);
        }

        private static IEnumerable<object> ToDynamicODataEntry(IEnumerable<object> entry, ITypeCache typeCache)
        {
            return entry == null
                ? null
                : entry.Select(x => x is IDictionary<string, object>
                    ? new DynamicODataEntry(x as IDictionary<string, object>, typeCache)
                    : x).ToList();
        }

        private object GetEntryValue(string propertyName)
        {
            var value = base[propertyName];
            if (value is IDictionary<string, object>)
                value = new DynamicODataEntry(value as IDictionary<string, object>, TypeCache);
            return value;
        }

        public DynamicMetaObject GetMetaObject(Expression parameter)
        {
            return new DynamicEntryMetaObject(parameter, this);
        }

        private class DynamicEntryMetaObject : DynamicMetaObject
        {
            internal DynamicEntryMetaObject(Expression parameter, DynamicODataEntry value)
                : base(parameter, BindingRestrictions.Empty, value)
            {
                TypeCache = value.TypeCache;
            }

            private ITypeCache TypeCache { get; }

            public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
            {
                var methodInfo = typeof(DynamicODataEntry).GetDeclaredMethod(nameof(GetEntryValue));
                var arguments = new Expression[]
                {
                    Expression.Constant(binder.Name)
                };

                return new DynamicMetaObject(
                    Expression.Call(Expression.Convert(Expression, LimitType), methodInfo, arguments),
                    BindingRestrictions.GetTypeRestriction(Expression, LimitType));
            }

            public override DynamicMetaObject BindConvert(ConvertBinder binder)
            {
                Expression<Func<bool, ODataEntry, object>> convertValueExpression = (hv, e) => hv
                    ? e.AsDictionary().ToObject(TypeCache, binder.Type, false)
                    : null;
                var valueExpression = Expression.Convert(Expression.Invoke(convertValueExpression, Expression.Constant(HasValue), Expression.Convert(Expression, LimitType)),
                    binder.Type);
                
                return new DynamicMetaObject(
                    valueExpression,
                    BindingRestrictions.GetTypeRestriction(Expression, LimitType));
            }
        }
    }
}