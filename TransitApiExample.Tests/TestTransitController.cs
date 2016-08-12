using System;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using FluentAssertions;
using Xunit;
using TransitAPIExample;
using iSynaptic.Commons;
using Newtonsoft.Json;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace TransitApiExample.Tests
{
    public class TestTransitController
    {
        private readonly string _testDataFolder = "TestData";
        private readonly TransitController _controller;

        public TestTransitController()
        {
            var mockNexTripService = new Mock<INexTripService>();
            mockNexTripService.Setup(x => x.GetRoutes())
                .Returns(ResultFromFile<IEnumerable<Route>>("routes.json"));

            mockNexTripService.Setup(x => x.GetDirectionsForRoute(It.IsAny<int>()))
                .Returns<int>(x => ResultFromFile<IEnumerable<Direction>>($"directions-{x}.json"));

            mockNexTripService.Setup(x => x.GetStopsForRouteDirection(It.IsAny<RouteDirection>()))
                .Returns<RouteDirection>(
                    x => ResultFromFile<IEnumerable<Stop>>($"stops-{x.RouteId}-{x.DirectionId}.json")
                );
            mockNexTripService.Setup(x => x.GetDeparturesForRouteDirectionStop(It.IsAny<RouteDirectionStop>()))
                .Returns<RouteDirectionStop>(
                    x => ResultFromFile<IEnumerable<Departure>>($"departures-{x.RouteId}-{x.DirectionId}-{x.StopId}.json")
                );

            Config config = JsonConvert.DeserializeObject<Config>(LocalFileLoader.Load($"{_testDataFolder}/config.json").Value);

            _controller = new TransitController(config, mockNexTripService.Object);
        }


        [Fact]
        public async Task ControllerShouldExplainParametersToUser()
        {
            var result = await _controller.Get(null, null, null, null);
            VerifyStringsInResult<BadRequestObjectResult>(result, "route", "stop", "direction");
        }

        [Fact]
        public async Task ControllerShouldReturnJSONGivenProperHeaders()
        {
            var result = await _controller.Get("application/json", null, null, null);
            ((ObjectResult)result).Value.GetType().ShouldBeEquivalentTo(typeof(Outcome<string>));
        }

        [Fact]
        public async Task ControllerShouldDemandSpecificRoute()
        {
            var result = await _controller.Get(null, "METRO", "Target", "East");
            VerifyStringsInResult<BadRequestObjectResult>(result, "Multiple routes", "Green Line", "Blue Line");
        }

        [Fact]
        public async Task ControllerShouldDemandSpecificStop()
        {
            var result = await _controller.Get(null, "Northstar Rail", "5th", "South");
            VerifyStringsInResult<BadRequestObjectResult>(result, "Multiple stops", "5th st", "5th ave");
        }

        [Fact]
        public async Task ControllerShouldShowFirstDeparture()
        {
            var result = await _controller.Get(null, "Northstar Rail", "5th st", "South");
            VerifyStringsInResult<OkObjectResult>(result, "6:00");
        }

        [Fact]
        public async Task ControllerShouldReturnJSONDeparture()
        {
            var result = await _controller.Get("application/json", "Northstar Rail", "5th st", "South");
            ((ObjectResult)result).Value.GetType().ShouldBeEquivalentTo(typeof(Departure));
        }

        private void VerifyStringsInResult<T>(IActionResult result, params string[] strings)
        {
            result.Should().BeOfType<T>();
            var resultString = (string)((ObjectResult)result).Value;
            foreach (var match in strings)
            {
                resultString.Contains(match, StringComparison.OrdinalIgnoreCase).Should().BeTrue();
            }
        }

        private Task<Result<T, string>> ResultFromFile<T>(string fileName) {
            var contentResult = LocalFileLoader.Load($"{_testDataFolder}/{fileName}");
            if(!contentResult.WasSuccessful)
            {
                return Task.FromResult(Result<T, string>.NoValue.Fail().ObserveMany(contentResult.Observations));
            }
            return Task.FromResult(JsonConvert.DeserializeObject<T>(contentResult.Value).ToResult<T, string>());
        }
    }
}
