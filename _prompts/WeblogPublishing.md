# Create Custom Weblog Publishing API

### Name of the API Ideas
BlazePost API  (*)
SimplyPost API

A very simple quick format for publishing and accessing blog posts that focuses just on the basics for a simple blog engine. The goal is to have a very simple API that can be quickly and easily be implemented by any REST API client and Server without complicated dependencies.

At minimum we need to match the feature set of this without `getUsersBlogs` and `getAuthors` methods in
`D:\projects\West Wind Web Log\WebLogFramework\Services\MetaWeblog.cs` which is a specific implementation. We only need the shell with the implementation being handled by each application.

Basic Feature support:

* Simple Userid/pwd authentication
* Optional -  Bearer Token authentication

* Publish a post
* Publish media files (images, other media)
* Retrieve a post
* Delete a post (should delete any dependencies)

* Get Recent Posts
* Optional - Search Posts (search by date and/or title)


### Client and Server Implementations
Using .NET C# implementation.

Client is started to be implemented in `Westwind.WeblogServices.Client` project with the actual client interface living in `WeblogPostServiceClient.cs`. For the client use `HttpClientUtils` for the Http calls. Use the existing methods as a reference.

For the server implement as an interface that can be used with an ASP.NET Controller. But the interface should be generic so it could be implemented in a different manner.

The idea is that URLs are based on a base Url and all other Urls are applied below that Url base path.






