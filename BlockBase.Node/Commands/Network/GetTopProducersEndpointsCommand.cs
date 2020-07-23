using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BlockBase.Domain.Endpoints;
using BlockBase.Domain.Results;
using BlockBase.Network.Mainchain;
using BlockBase.Node.Commands.Utils;
using BlockBase.Utils;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace BlockBase.Node.Commands.Network
{
    public class GetTopProducersEndpointsCommand : AbstractCommand
    {
        private ILogger _logger;


        public override string CommandName => "Get top producers endpoints";

        public override string CommandInfo => "Retrieves top producers endpoints";

        public override string CommandUsage => "get top producers";

        public GetTopProducersEndpointsCommand(ILogger logger)
        {
            _logger = logger;
        }

        public override async Task<CommandExecutionResponse> Execute()
        {
           try
            {
                var request = HttpHelper.ComposeWebRequestGet(BlockBaseNetworkEndpoints.GET_TOP_21_PRODUCERS_ENDPOINTS);
                var json = await HttpHelper.CallWebRequest(request);
                var topProducers = JsonConvert.DeserializeObject<List<TopProducerEndpoint>>(json);

                //TODO rpinto - Nice implementation - should be done periodically though, and not on request
                var topProducersEndpointResponse = await ConvertToAndMeasureTopProducerEndpointResponse(topProducers.Take(10).ToList());

                return new CommandExecutionResponse(HttpStatusCode.OK, new OperationResponse<List<TopProducerEndpointResponse>>(topProducersEndpointResponse));
            }
            catch (Newtonsoft.Json.JsonReaderException)
            {
                return new CommandExecutionResponse(HttpStatusCode.NotFound, new OperationResponse(false, "Unable to retrieve the list of producers"));
            }
            catch (Exception e)
            {
                return new CommandExecutionResponse(HttpStatusCode.InternalServerError, new OperationResponse(e));
            }
        }

        protected override bool IsCommandAppropratelyStructured(string[] commandData)
        {
            return commandData.Length == 3;
        }

        protected override bool IsCommandRecognizable(string commandStr)
        {
            return commandStr.StartsWith(CommandUsage);
        }

        protected override CommandParseResult ParseCommand(string[] commandData)
        {
            if (commandData.Length == 3) return new CommandParseResult(true, true);

            return new CommandParseResult(true, CommandUsage);
        }

         private async Task<List<TopProducerEndpointResponse>> ConvertToAndMeasureTopProducerEndpointResponse(List<TopProducerEndpoint> topProducers)
        {
            var topProducersEndpointResponse = new List<TopProducerEndpointResponse>();

            foreach (var producer in topProducers)
            {
                if (!producer.Endpoints.Any()) continue;
                var producerEndpointResponse = new TopProducerEndpointResponse();
                producerEndpointResponse.ProducerInfo = producer.ProducerInfo;
                producerEndpointResponse.Endpoints = new List<EndpointResponse>();
                var requests = new List<HttpWebRequest>();

                foreach (var endpoint in producer.Endpoints)
                {
                    if (!endpoint.Contains("http")) continue;

                    var infoRequest = HttpHelper.ComposeWebRequestGet($"{endpoint}/v1/chain/get_info");
                    requests.Add(infoRequest);
                }

                var requestResults = requests.Select(r => HttpHelper.MeasureWebRequest(r.RequestUri.GetLeftPart(System.UriPartial.Authority), r)).ToList();
                await Task.WhenAll(requestResults);
                var results = requestResults.Select(r => r.Result);

                foreach (var result in results)
                {
                    var endpointResponse = new EndpointResponse();
                    endpointResponse.Endpoint = result.Item1;
                    endpointResponse.ResponseTimeInMs = result.Item2;
                    producerEndpointResponse.Endpoints.Add(endpointResponse);
                }

                producerEndpointResponse.Endpoints = producerEndpointResponse.Endpoints.OrderBy(e => e.ResponseTimeInMs).ToList();
                topProducersEndpointResponse.Add(producerEndpointResponse);
            }

            return topProducersEndpointResponse.ToList();
        }
    }
}