using Amazon.Lambda.AspNetCoreServer;
using Amazon.S3;
using Amazon.S3.Model;

var builder = WebApplication.CreateBuilder(args);

// Load AWS settings from configuration
var awsOptions = builder.Configuration.GetSection("AWS").Get<AwsOptions>();

// Register the AmazonS3Client with dependency injection
builder.Services.AddSingleton<IAmazonS3>(sp =>
{
	return new AmazonS3Client(
		awsAccessKeyId: awsOptions.AccessKeyId,
		awsSecretAccessKey: awsOptions.SecretAccessKey,
		region: Amazon.RegionEndpoint.GetBySystemName(awsOptions.Region)
	);
});

// Configure middleware and endpoints
var app = builder.Build();

app.MapPost("/upload", async (HttpContext context, IAmazonS3 s3Client) =>
{
	var keyName = "your-file-key";
	using var stream = context.Request.Body;
	var putRequest = new PutObjectRequest
	{
		BucketName = awsOptions.BucketName,
		Key = keyName,
		InputStream = stream
	};

	var response = await s3Client.PutObjectAsync(putRequest);
	return Results.Ok(response.HttpStatusCode);
});

app.MapGet("/download", async (HttpContext context, IAmazonS3 s3Client) =>
{
	var keyName = "your-file-key";
	var getRequest = new GetObjectRequest
	{
		BucketName = awsOptions.BucketName,
		Key = keyName
	};

	using var response = await s3Client.GetObjectAsync(getRequest);
	context.Response.ContentType = response.Headers["Content-Type"];
	await response.ResponseStream.CopyToAsync(context.Response.Body);
});

app.Run();

public class AwsOptions
{
	public string AccessKeyId { get; set; }
	public string SecretAccessKey { get; set; }
	public string Region { get; set; }
	public string BucketName { get; set; }
}

// Lambda entry point (for AWS Lambda without Startup.cs)
public class LambdaEntryPoint : APIGatewayHttpApiV2ProxyFunction
{
	// No need to override Init if you're using Program.cs directly
}
