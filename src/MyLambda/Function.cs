using Amazon.CodeDeploy.Model;
using Amazon.Lambda;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Model;
using System.Text;
using Environment = System.Environment;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace MyLambda;

public class Function
{
    private readonly AmazonLambdaClient _lambdaClient;

    public Function()
    {
        _lambdaClient = new AmazonLambdaClient();
    }

    public APIGatewayHttpApiV2ProxyResponse FunctionHandler(APIGatewayHttpApiV2ProxyRequest input, ILambdaContext context)
    {
        return new APIGatewayHttpApiV2ProxyResponse
        {
            Body = $"{{'version':'{context.FunctionVersion}'}}",
            StatusCode = 200,
            Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
        };
    }

    public async Task PreFunctionHandler(PutLifecycleEventHookExecutionStatusRequest request, ILambdaContext context)
    {
        context.Logger.Log("Processing lifecycle pre-traffic hook");
        var status = Amazon.CodeDeploy.LifecycleEventStatus.Failed;
        var function = Environment.GetEnvironmentVariable("TARGET");
        try
        {
            context.Logger.Log($"Invoking {function}");
            var invokeRequest = new InvokeRequest
            {
                FunctionName = function,
                Payload = "{}",               
                InvocationType = InvocationType.RequestResponse
            };
            var invokeResponse = await _lambdaClient.InvokeAsync(invokeRequest);
            var payload = Encoding.UTF8.GetString(invokeResponse.Payload.ToArray());
            context.Logger.Log($"Response {payload}");
            status = Amazon.CodeDeploy.LifecycleEventStatus.Succeeded;
        }
        catch (Exception ex)
        {
            status = Amazon.CodeDeploy.LifecycleEventStatus.Failed;
            context.Logger.LogError($"Error {ex.Message}");
        }
        finally
        {
            context.Logger.Log("Calling CodeDeploy");
            using var codedeploy = new Amazon.CodeDeploy.AmazonCodeDeployClient();
            var statusRequest = new PutLifecycleEventHookExecutionStatusRequest()
            {
                DeploymentId = request.DeploymentId,
                LifecycleEventHookExecutionId = request.LifecycleEventHookExecutionId,
                Status = status,
            };
            await codedeploy.PutLifecycleEventHookExecutionStatusAsync(statusRequest).ConfigureAwait(false);
        }
    }
    public async Task PostFunctionHandler(PutLifecycleEventHookExecutionStatusRequest request, ILambdaContext context)
    {
        context.Logger.Log("Processing lifecycle post-traffic hook.");
        var status = Amazon.CodeDeploy.LifecycleEventStatus.Failed;
        var function = Environment.GetEnvironmentVariable("TARGET");
        try
        {
            context.Logger.Log($"Invoking {function}");
            var invokeRequest = new InvokeRequest
            {
                FunctionName = function,
                Payload = "{}",
                InvocationType = InvocationType.RequestResponse
            };
            var invokeResponse = await _lambdaClient.InvokeAsync(invokeRequest);
            var payload = Encoding.UTF8.GetString(invokeResponse.Payload.ToArray());
            context.Logger.Log($"Response {payload}");
            status = Amazon.CodeDeploy.LifecycleEventStatus.Succeeded;
        }
        catch (Exception ex)
        {
            status = Amazon.CodeDeploy.LifecycleEventStatus.Failed;
            context.Logger.LogError($"Error {ex.Message}");
        }
        finally
        {
            context.Logger.Log("Calling CodeDeploy");
            using var codedeploy = new Amazon.CodeDeploy.AmazonCodeDeployClient();
            var statusRequest = new PutLifecycleEventHookExecutionStatusRequest()
            {
                DeploymentId = request.DeploymentId,
                LifecycleEventHookExecutionId = request.LifecycleEventHookExecutionId,
                Status = status,
            };
            await codedeploy.PutLifecycleEventHookExecutionStatusAsync(statusRequest).ConfigureAwait(false);
        }
    }
}