using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Text.RegularExpressions;

namespace TransitAPIExample
{
    public class Parameter
    {
        public Parameter(string key, string value)
        {
            Key = key;
            Value = value;
        }
        public string Key;
        public string Value;
    }

    public struct Route
    {
        public string Description;
        public string ProviderID;
        [JsonProperty(PropertyName = "Route")]
        public int Id;
    }

    public struct Direction
    {
        [JsonProperty(PropertyName = "Text")]
        public string Description;
        [JsonProperty(PropertyName= "Value")]
        public int Id;
    }

    public struct Stop
    {
        [JsonProperty(PropertyName = "Text")]
        public string Description;
        [JsonProperty(PropertyName = "Value")]
        public string Id;
    }

    public class Departure
    {
        private static Regex DepartureTimeRegex = new Regex(@"/Date\((\d+)-\d+\)/");

        public readonly string DepartureTimeText;
        public readonly long DepartureTimeInJsDateMs;

        [JsonConstructor]
        public Departure(string DepartureText, string DepartureTime, string Terminal)
        {
            this.DepartureTimeText = DepartureText;
            this.DepartureTimeInJsDateMs = Convert.ToInt64(DepartureTimeRegex.Match(DepartureTime).Groups[1].Value);
        }
    }

    public struct RouteDirection
    {
        public int RouteId;
        public int DirectionId;
    }

    public class RouteDirectionEqualityComparer : IEqualityComparer<RouteDirection>
    {
        public bool Equals(RouteDirection a, RouteDirection b)
        {
            return a.RouteId == b.RouteId && a.DirectionId == b.DirectionId;
        }
        public int GetHashCode(RouteDirection a)
        {
            return (a.RouteId ^ a.DirectionId).GetHashCode();
        }
    }

    public struct RouteDirectionStop
    {
        public int RouteId;
        public int DirectionId;
        public string StopId;
    }

    public class RouteDirectionStopEqualityComparer : IEqualityComparer<RouteDirectionStop>
    {
        public bool Equals(RouteDirectionStop a, RouteDirectionStop b)
        {
            return a.RouteId == b.RouteId && a.DirectionId == b.DirectionId && a.StopId.Equals(b.StopId);
        }
        public int GetHashCode(RouteDirectionStop a)
        {
            return (a.RouteId ^ a.DirectionId).GetHashCode() ^ a.StopId.GetHashCode();
        }
    }
}
