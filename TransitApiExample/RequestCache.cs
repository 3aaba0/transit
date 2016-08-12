using iSynaptic.Commons;
using Microsoft.Extensions.PlatformAbstractions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TransitAPIExample
{
    public class RequestCache<TRequest, TResult>
    {
        private readonly Func<TRequest, Task<TResult>> _makeRequest;
        private readonly TimeSpan _timeToLive;
        private readonly Dictionary<TRequest, TResult> _cache;

        public RequestCache(Func<TRequest, Task<TResult>> makeRequest, TimeSpan timeToLive)
            : this(makeRequest, timeToLive, null) { }

        public RequestCache(
                Func<TRequest, Task<TResult>> makeRequest,
                TimeSpan timeToLive,
                IEqualityComparer<TRequest> equalityComparer
            )
        {
            _makeRequest = makeRequest;
            _timeToLive = timeToLive;
            if (equalityComparer != null)
            {
                _cache = new Dictionary<TRequest, TResult>(equalityComparer);
            }
            else
            {
                _cache = new Dictionary<TRequest, TResult>();
            }
        }

        public async Task<TResult> Get(TRequest request)
        {
            Maybe<TResult> maybeCached = Maybe.NoValue;
            lock (_cache)
            {
                if (_cache.ContainsKey(request))
                {
                    maybeCached = _cache[request].ToMaybe();
                }
            }
            if (maybeCached.HasValue)
            {
                return maybeCached.Value;
            }
            else
            {
                var toReturn = await _makeRequest(request);
                lock (_cache)
                {
                    _cache[request] = toReturn;
                }

                new Task(async () => {
                    await Task.Delay(_timeToLive);
                    lock (_cache)
                    {
                        _cache.Remove(request);
                    }
                }).Start();

                return toReturn;
            }
        }
    }
}
