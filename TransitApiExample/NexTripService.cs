using iSynaptic.Commons;
using Microsoft.Extensions.PlatformAbstractions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Headers;

namespace TransitAPIExample
{
    public interface INexTripService
    {
        Task<Result<IEnumerable<Route>, string>> GetRoutes();
        Task<Result<IEnumerable<Direction>, string>> GetDirectionsForRoute(int routeId);
        Task<Result<IEnumerable<Stop>, string>> GetStopsForRouteDirection(RouteDirection query);
        Task<Result<IEnumerable<Departure>, string>> GetDeparturesForRouteDirectionStop(RouteDirectionStop query);
    }

    public class NexTripService : INexTripService
    {
        private readonly string _url;
        private readonly HttpClient _httpClient;

        private readonly RequestCache<bool, Result<IEnumerable<Route>, string>> _routes;
        private readonly RequestCache<int, Result<IEnumerable<Direction>, string>> _directionsByRoute;
        private readonly RequestCache<RouteDirection, Result<IEnumerable<Stop>, string>> _stopsByRouteDirection;
        private readonly RequestCache<RouteDirectionStop, Result<IEnumerable<Departure>, string>> _departuresByRouteDirectionStop;

        public NexTripService (Config config)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _url = config.ApiUrl;

            _routes = new RequestCache<bool, Result<IEnumerable<Route>, string>>(
                x => Get<IEnumerable<Route>>($"{_url}/routes"),
                new TimeSpan(0, 0, config.RoutesTTLSeconds)
            );
            _directionsByRoute = new RequestCache<int, Result<IEnumerable<Direction>, string>>(
                routeId => Get<IEnumerable<Direction>>($"{_url}/directions/{routeId}"),
                new TimeSpan(0, 0, config.DirectionsTTLSeconds)
            );
            _stopsByRouteDirection = new RequestCache<RouteDirection, Result<IEnumerable<Stop>, string>>(
                query => Get<IEnumerable<Stop>>($"{_url}/stops/{query.RouteId}/{query.DirectionId}"),
                new TimeSpan(0, 0, config.StopsTTLSeconds)
            );
            _departuresByRouteDirectionStop = new RequestCache<RouteDirectionStop, Result<IEnumerable<Departure>, string>>(
                query => Get<IEnumerable<Departure>>($"{_url}/{query.RouteId}/{query.DirectionId}/{query.StopId}"),
                new TimeSpan(0, 0, config.DeparturesTTLSeconds)
            );
        }

        public Task<Result<IEnumerable<Route>, string>> GetRoutes()
        {
            return _routes.Get(true);
        }

        public Task<Result<IEnumerable<Direction>, string>> GetDirectionsForRoute(int routeId)
        {
            return _directionsByRoute.Get(routeId);
        }

        public Task<Result<IEnumerable<Stop>, string>> GetStopsForRouteDirection(RouteDirection query)
        {
            return _stopsByRouteDirection.Get(query);
        }

        public Task<Result<IEnumerable<Departure>, string>> GetDeparturesForRouteDirectionStop(RouteDirectionStop query)
        {
            return _departuresByRouteDirectionStop.Get(query);
        }

        private async Task<Result<T, string>> Get<T>(string url)
        {
            var http = await _httpClient.GetAsync(url);
            if(!http.IsSuccessStatusCode)
            {
                return Result.NoValue.Fail().Observe($"HTTP GET: {url}: {(int)http.StatusCode} {http.ReasonPhrase}");
            }
            var contentString = await http.Content.ReadAsStringAsync();
            try
            {
                return JsonConvert.DeserializeObject<T>(contentString).ToResult<T, string>();
            }
            catch (Exception ex)
            {
                return Result.NoValue.Fail().Observe($"HTTP GET: {url}: Malformed data. {ex.GetType().Name}: {ex.Message}.");
            }
        }
    }
}
