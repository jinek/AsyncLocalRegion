using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace AsyncLocalRegion
{
    public sealed class AsyncLocalRegion<T>
    {
        /// <summary>
        /// Get current value
        /// </summary>
        /// <param name="region"></param>
        /// <exception cref="AsyncLocalRegionException">There is no a region currently</exception>
        /// <returns>Value set for current thread/async flow</returns>
        public static explicit operator T(AsyncLocalRegion<T> region)
        {
            return region.CurrentValue;
        }  
        
        private readonly AsyncLocal<Stack<T>> _currentValue = new AsyncLocal<Stack<T>>();

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
                    throw new AsyncLocalRegionException("Can not retrieve a value because there is no current region",
                        nullReferenceException);
                }
                catch (InvalidOperationException invalidOperationException)
                {
                    throw new AsyncLocalRegionException("Can not retrieve a value because there is no current region",
                        invalidOperationException);
                }
            }
        }
        
        /// <summary>
        /// Has any region started
        /// </summary>
        public bool HasCurrentValue => !(_currentValue.Value == null || _currentValue.Value.Count == 0);

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
            private readonly AsyncLocalRegion<T> _parent;

            public Region(AsyncLocalRegion<T> parent)
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