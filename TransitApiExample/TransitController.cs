using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using iSynaptic.Commons;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Http;

namespace TransitAPIExample
{
    [Route("")]
    public class TransitController : Controller
    {
        private readonly Config _config;
        private readonly INexTripService _nexTripService;

        public TransitController(Config config, INexTripService nexTripService) {
            _config = config;
            _nexTripService = nexTripService;
        }
        
        [HttpGet("")]
        public async Task<IActionResult> Get(
               [FromHeader(Name ="Accept")] string accept,
               string route,
               string stop,
               string direction
           )
        {
            var parametersNotNullOutcome = GetNotNullParameterOutcome(
                new List<Parameter>() {
                    new Parameter(nameof(route), route),
                    new Parameter(nameof(stop), stop),
                    new Parameter(nameof(direction), direction)
                }
            );

            if (!parametersNotNullOutcome.WasSuccessful)
            {
                return OutcomeResult(parametersNotNullOutcome, accept, BadRequest);
            }

            var routeIdResult = await GetRouteId(route);
            if(!routeIdResult.WasSuccessful)
            {
                return OutcomeResult(Outcome.Failure(routeIdResult.Observations), accept, BadRequest);
            }

            var directionIdResult = await GetDirectionId(direction, routeIdResult.Value);
            if(!directionIdResult.WasSuccessful)
            {
                return OutcomeResult(Outcome.Failure(directionIdResult.Observations), accept, BadRequest);
            }

            var stopIdResult = await GetStopId(
                new RouteDirection() {
                    DirectionId = directionIdResult.Value,
                    RouteId = routeIdResult.Value,
                },
                stop
            );
            if (!stopIdResult.WasSuccessful)
            {
                return OutcomeResult(Outcome.Failure(stopIdResult.Observations), accept, BadRequest);
            }

            var departuresResult = await _nexTripService.GetDeparturesForRouteDirectionStop(
                new RouteDirectionStop()
                {
                    DirectionId = directionIdResult.Value,
                    RouteId = routeIdResult.Value,
                    StopId = stopIdResult.Value
                }
            );
            if(!departuresResult.WasSuccessful)
            {
                return OutcomeResult(Outcome.Failure(departuresResult.Observations), accept, ServiceUnavailable);
            }
            if (!departuresResult.Value.Any())
            {
                return OutcomeResult(Outcome.Failure("Process completed successfully, but no departures were found."), accept, NotFound);
            }

            return DepartureResult(departuresResult.Value.First(), accept);
        }
        
        private IActionResult OutcomeResult(Outcome<string> outcome, string accept, Func<object, IActionResult> httpStatus)
        {
            if(IsJsonMode(accept))
            {
                return httpStatus(outcome);
            }
            else
            {
                return httpStatus($"Bad Request:\n\n{string.Join("\n", outcome.Observations)}");
            }
        }

        private IActionResult DepartureResult(Departure departure, string accept)
        {
            if (IsJsonMode(accept))
            {
                return Ok(departure);
            }
            else
            {
                return Ok(departure.DepartureTimeText);
            }
        }

        private ObjectResult ServiceUnavailable(object value)
        {
            var toReturn = new ObjectResult(value);
            toReturn.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return toReturn;
        }

        private bool IsJsonMode (string acceptHeader)
        {
            return acceptHeader != null && acceptHeader.Contains("json", StringComparison.OrdinalIgnoreCase);
        }

        public Outcome<string> GetNotNullParameterOutcome(IEnumerable<Parameter> parameters) {
            var missing = parameters.Where(x => string.IsNullOrWhiteSpace(x.Value));
            if(missing.Any()) {
                var missingWithAnd = missing.Select((x, i) => i > 0 && i == missing.Count() - 1 ? $"and '{x.Key}'" : $"'{x.Key}'");
                var missingString = string.Join(missing.Count() > 2 ? ", " : " ", missingWithAnd);
                var missingStringWithGrammar = missing.Count() > 1 ? $"the {missingString} parameters" : $"a '{missingString}' parameter";
                return Outcome.Failure($"Please provide {missingStringWithGrammar}. For example: ?route=21&stop=hamline&direction=west");
            } else {
                return Outcome.Success();
            }
        }
        
        public async Task<Result<int, string>> GetDirectionId(string directionQuery, int routeId) {
            var maybeDirectionId = _config.Directions
               .Where(x => x.Value.StartsWith(directionQuery, StringComparison.OrdinalIgnoreCase))
               .Select(x => x.Key)
               .MaybeFirst();

            if(maybeDirectionId.HasValue) {
                var getDirectionsResult = await _nexTripService.GetDirectionsForRoute(routeId);
                if(!getDirectionsResult.WasSuccessful)
                {
                    return Result.NoValue.Fail().ObserveMany(getDirectionsResult.Observations);
                }
                if(!getDirectionsResult.Value.Any(x => x.Id == maybeDirectionId.Value)) {
                    return Result.NoValue.Fail().Observe(
                        string.Format(
                            "Invalid direction '{0}'. Valid directions for this route are {1}.",
                            directionQuery,
                            string.Join(" and ", getDirectionsResult.Value.Select(x => x.Description.ToLower()))
                        )
                    );
                }
                return maybeDirectionId.Value.ToResult();
            } else {
                return Result.NoValue.Fail().Observe(
                    string.Format(
                        "Invalid direction '{0}'. Valid directions strings are similar to: {1}", 
                        directionQuery, 
                        string.Join(", ", _config.Directions.Select(x => x.Value))
                    )
                );
            }
        }

        public async Task<Result<int, string>> GetRouteId(string routeQuery)
        {
            var routesResult = await _nexTripService.GetRoutes();
            if (!routesResult.WasSuccessful)
            {
                return Result.NoValue.Fail().ObserveMany(routesResult.Observations);
            }
            else
            {
                var containsWholeWordRegex = new Regex($@"\b{routeQuery}\b", RegexOptions.IgnoreCase);

                var matches = routesResult.Value
                    .Where(x => containsWholeWordRegex.Match(x.Description).Success);

                if (matches.Count() == 0)
                {
                    matches = routesResult.Value
                        .Where(x => x.Description.Contains(routeQuery, StringComparison.OrdinalIgnoreCase));
                }

                if (matches.Count() == 0)
                {
                    return Result.NoValue.Fail().Observe($"No route found matching {routeQuery}");
                }
                if (matches.Count() > 1)
                {
                    var matchesString = "\n    "+string.Join(",\n    ", matches.Select(x => x.Description));
                    return Result.NoValue.Fail().Observe(
                        $"Multiple routes found matching {routeQuery}: {matchesString}.\n\nPlease narrow your search."
                    );
                }

                return matches.First().Id.ToResult<int, string>();
            }
        }

        public async Task<Result<string, string>> GetStopId(RouteDirection routeDirection, string stopQuery)
        {
            var stopsResult = await _nexTripService.GetStopsForRouteDirection(routeDirection);
            if (!stopsResult.WasSuccessful)
            {
                return Result.NoValue.Fail().ObserveMany(stopsResult.Observations);
            }
            else
            {
                var containsWholeWordRegex = new Regex($@"\b{stopQuery}\b", RegexOptions.IgnoreCase);

                var matches = stopsResult.Value
                    .Where(x => containsWholeWordRegex.Match(x.Description).Success);

                if (matches.Count() == 0)
                {
                    matches = stopsResult.Value
                        .Where(x => x.Description.Contains(stopQuery, StringComparison.OrdinalIgnoreCase));
                }

                if (matches.Count() == 0)
                {
                    return Result.NoValue.Fail().Observe($"No stop found matching {stopQuery}");
                }
                if (matches.Count() > 1)
                {
                    var matchesString = "\n    " + string.Join(",\n    ", matches.Select(x => x.Description));
                    return Result.NoValue.Fail().Observe(
                        $"Multiple stops found matching {stopQuery}: {matchesString}.\n\nPlease narrow your search."
                    );
                }

                return matches.First().Id.ToResult<string, string>();
            }
        }

    }

}

