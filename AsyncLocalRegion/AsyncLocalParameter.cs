using System;
using System.Threading;

namespace AsyncLocalRegion
{
    public sealed class AsyncLocalParameter<T>
    {
        private readonly AsyncLocal<Region> _currentValue = new AsyncLocal<Region>();

        /// <summary>
        ///     Current value
        /// </summary>
        /// <exception cref="AsyncLocalRegionException">No current parameter defined</exception>
        public T CurrentValue
        {
            get
            {
                var region = _currentValue.Value;
                if (region == null)
                    throw new AsyncLocalRegionException(
                        "Can not retrieve a value because there is no current parameter");

                return region.Value;
            }
        }

        /// <summary>
        ///     Has any region started
        /// </summary>
        public bool HasCurrentValue => _currentValue.Value != null;

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
            var region = new Region(_currentValue, value);
            // Assign a new head for this flow instead of mutating a shared stack.
            _currentValue.Value = region;
            return region;
        }

        private class Region : IDisposable
        {
            private readonly AsyncLocal<Region> _currentValue;
            private readonly Region _previous;
            private bool _disposed;

            public Region(AsyncLocal<Region> currentValue, T value)
            {
                _currentValue = currentValue;
                _previous = currentValue.Value;
                Value = value;
            }

            public T Value { get; }

            public void Dispose()
            {
                if (_disposed)
                    throw new ObjectDisposedException("Object has been already disposed");
                if (!ReferenceEquals(_currentValue.Value, this))
                    throw new InvalidOperationException("Regions must be disposed in reverse order of creation");

                _currentValue.Value = _previous;
                _disposed = true;
            }
        }
    }
}
