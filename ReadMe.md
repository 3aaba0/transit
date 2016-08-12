##Transit API Consumption Example

Uses http://svc.metrotransit.org

##API example

If you wanted to know when the next 21 bus leaves from Concordia going to the Midtown Global Market, you would use:
https://transitexample.sequentialread.com?route=21&stop=hamline&direction=west
Your response would be a human readable duration, for example, 
`5 Minutes`

The deault response type is `text/plain`.  If you specify a `Accept: application/json` header, then you will recieve a JSON object:

```
{
  "departureTimeText": "3 Min",
  "departureTimeInJsDateMs": 1470994200000
}
```

### URL Parameters

`route` (required) any substring of a bus route name which matches *exactly one* bus route
`stop` (required) any substring of a bus stop name on the specified route which matches *exactly one* stop
`direction` (required) any of "north "east" "west" or "south"

## Pre-requisites for developers

  * .NET Core 1.0.0 preview 2 installed (as of 2016-06-28 this is the default to be installed via visual studio) 
     * `dotnet --version` returns `1.0.0-preview2-003121`
	 * if you have `dnu` and `dnx` on your `PATH`, they should be uninstalled as they are replaced by the `dotnet` command.

#### how to run locally

Open the solution file in visual studo and press run.

#### run tests 

```
PM> cd TransitApiExample.Tests
PM> dotnet test
Project TransitApiExample (.NETCoreApp,Version=v1.0) was previously compiled. Skipping compilation.
Project TransitApiExample.Tests (.NETCoreApp,Version=v1.0) was previously compiled. Skipping compilation.
xUnit.net .NET CLI test runner (64-bit .NET Core win10-x64)
  Discovering: TransitApiExample.Tests
  Discovered:  TransitApiExample.Tests
  Starting:    TransitApiExample.Tests
  Finished:    TransitApiExample.Tests
=== TEST EXECUTION SUMMARY ===
   TransitApiExample.Tests  Total: 8, Errors: 0, Failed: 0, Skipped: 0, Time: 0.549s
SUMMARY: Total: 1 targets, Passed: 1, Failed: 0.
PM> 
```

#### how to build and run using the docker

```
#cd to directory containing Dockerfile

docker build -t 3aaba0/transit-example:<VERSION_HERE> .

docker run -d -p 5000:5000 3aaba0/transit-example
```

