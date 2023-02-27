using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace AsyncLocalRegion
{
    public sealed class AsyncLocalParameter<T>
    {
        private readonly AsyncLocal<Stack<T>> _currentValue = new AsyncLocal<Stack<T>>();

        /// <summary>
        ///     Current value
        /// </summary>
        /// <exception cref="AsyncLocalRegionException">No current parameter defined</exception>
        public T CurrentValue
        {
            get
            {
                try
                {
                    return _currentValue.Value.Peek();
                }
                catch (NullReferenceException nullReferenceException)
                {
                    throw new AsyncLocalRegionException(
                        "Can not retrieve a value because there is no current parameter",
                        nullReferenceException);
                }
                catch (InvalidOperationException invalidOperationException)
                {
                    throw new AsyncLocalRegionException(
                        "Can not retrieve a value because there is no current parameter",
                        invalidOperationException);
                }
            }
        }

        /// <summary>
        /// Has any region started
        /// </summary>
        public bool HasCurrentValue => !(_currentValue.Value == null || _currentValue.Value.Count == 0);

        /// <summary>
        ///     Get the current value
        /// </summary>
        /// <param name="parameter">Region for which to get the value</param>
        /// <exception cref="AsyncLocalRegionException">There is no a parameter currently</exception>
        /// <returns>Value set for current thread/async flow</returns>
        public static explicit operator T(AsyncLocalParameter<T> parameter)
        {
            return ToT(parameter);
        }


        // ReSharper disable once MemberCanBePrivate.Global Need to be public to prevent CA2225
        public static T ToT(AsyncLocalParameter<T> parameter)
        {
            return parameter.CurrentValue;
        }

        /// <summary>
        ///     Start a parameter with provided value
        /// </summary>
        /// <param name="value">Value to be kept for this parameter</param>
        /// <returns>Returns the parameter. Dispose it to revert to previous value</returns>
        public IDisposable StartRegion(T value)
        {
            if (_currentValue.Value == null)
                _currentValue.Value = new Stack<T>(1);
            _currentValue.Value.Push(value);
            return new Region(this);
        }

        private class Region : IDisposable
        {
            private bool _disposed;
            private readonly AsyncLocalParameter<T> _parent;

            public Region(AsyncLocalParameter<T> parent)
            {
                _parent = parent;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            public void Dispose()
            {
                if (_disposed)
                    throw new ObjectDisposedException("Object has been already disposed");
                _disposed = true;
                ReleaseResources();
                GC.SuppressFinalize(this);
            }

            private void ReleaseResources()
            {
                _parent._currentValue.Value.Pop();//todo: check poped region is the one
            }

            ~Region()
            {
                ReleaseResources();
            }
        }
    }
}