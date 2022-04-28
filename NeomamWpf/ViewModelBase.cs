using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace NeomamWpf
{
    class MemberVisitor : ExpressionVisitor
    {
        private readonly List<MemberExpression> _memberExpressions = new();

        [return: NotNullIfNotNull("node")]
        public override Expression? Visit(Expression? node)
        {
            if (node is MemberExpression me)
            {
                this._memberExpressions.Add(me);
            }

            return base.Visit(node);
        }

        public static List<string> GetMembers(Expression exp)
        {
            var vis = new MemberVisitor();
            vis.Visit(exp);
            return vis._memberExpressions.Select(m => m.Member.Name).ToList();
        }
    }

    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly Dictionary<string, List<string>> _depProps = new();

        protected void RaisePropChanged(string propName)
        {
            this.RaisePropChangedInternal(propName);
            if (this._depProps.TryGetValue(propName, out var deps))
            {
                foreach (var dep in deps)
                {
                    this.RaisePropChangedInternal(dep);
                }
            }
        }

        protected void RaisePropChangedInternal(string propName)
            => this.OnPropertyChanged(new PropertyChangedEventArgs(propName));

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs args)
        {
            this.PropertyChanged?.Invoke(this, args);
        }

        public T DepProp<T>(Expression<Func<T>> getter, [CallerMemberName]string? propName = null)
        {
            if (!this._depProps.Values.SelectMany(x => x).Any(v => v == propName))
            {
                foreach (var mem in MemberVisitor.GetMembers(getter))
                {
                    if (!this._depProps.TryGetValue(mem, out var deps))
                    {
                        deps = this._depProps[mem] = new List<string>();
                    }

                    deps.Add(propName ?? throw new ArgumentNullException());
                }
            }

            return getter.Compile()();
        }

        protected void Set<T>(
                Func<T> getter,
                Action setter,
                [CallerMemberName] string? propname = null
            )
        {
            var oldValue = getter();
            setter();
            if (!EqualityComparer<T>.Default.Equals(oldValue, getter()))
            {
                RaisePropChanged(propname ?? throw new ArgumentOutOfRangeException(nameof(propname)));
            }
        }

        protected void Set<T>(
            ref T field,
            T value,
            [CallerMemberName] string? propname = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                RaisePropChanged(propname ?? throw new ArgumentOutOfRangeException(nameof(propname)));
            }
        }
    }

    public class NeomamViewModel : ViewModelBase
    {
        protected override void OnPropertyChanged(PropertyChangedEventArgs args)
        {
            base.OnPropertyChanged(args);
            MainViewModel.NotifyChange();
        }
    }
}
