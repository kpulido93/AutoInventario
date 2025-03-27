using Amazon.Lambda;
using Amazon.Lambda.Model;
using System.Threading.Tasks;
using System;
using System.Text.Json;

namespace Services
{
        public class LambdaInvoker
    {
        private readonly AmazonLambdaClient _lambdaClient;
        private readonly string _functionName = "TuNombreDeLambda";

        public LambdaInvoker()
        {
            var config = new AmazonLambdaConfig
            {
                RegionEndpoint = Amazon.RegionEndpoint.USEast1 // o la región que uses
            };

            _lambdaClient = new AmazonLambdaClient("ACCESS_KEY", "SECRET_KEY", config);
        }

        public async Task InvokeLambdaAsync(object data)
        {
            var payload = JsonSerializer.Serialize(data);

            var request = new InvokeRequest
            {
                FunctionName = _functionName,
                Payload = payload
            };

            var response = await _lambdaClient.InvokeAsync(request);

            if (response.StatusCode != 200)
            {
                throw new Exception("Error al invocar la Lambda: " + response.FunctionError);
            }
        }
    }

}