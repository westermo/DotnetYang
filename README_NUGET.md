dotnetYang is a [Roslyn](https://github.com/dotnet/roslyn) source generator for using the .yang language to generate C# code, providing access to data models, ease-of-use asynchronous RPC, Action & Notification calls directly from code and generated server interfaces.

## Features

- **Drop-and-go:** Add your .yang files to a C# project as additional files that references this generator, that is it, your .yang defined RPC's and more are now available directly in  that C# projects code
- **Server-interface:** Want to implement a server that responds to NETCONF calls? Look no further than the generated interface `IYangServer` and it's extension method `async Task Recieve(this IYangServer server, Stream input, Stream output);` which provides a framework for implementing your own server without having to worry about serializing and parsing NETCONF directly, but instead work with well defined C# Datatypes.
he risk of becoming rather big. In such a case, it is recommended to split it's implementation into several `partial` server classes in order to maintain readability.  